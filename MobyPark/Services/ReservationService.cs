using MobyPark.DTOs.ParkingLot.Request;
using MobyPark.DTOs.Reservation.Request;
using MobyPark.Models;
using MobyPark.Models.Repositories.Interfaces;
using MobyPark.Services.Interfaces;
using MobyPark.Services.Results;
using MobyPark.Services.Results.Price;
using MobyPark.Services.Results.Reservation;
using MobyPark.Validation;
using MobyPark.Services.Results.User;
using MobyPark.Services.Results.UserPlate;
using System.Collections.Concurrent;

namespace MobyPark.Services;

public class ReservationService : IReservationService
{
    private static readonly ConcurrentDictionary<long, SemaphoreSlim> LotLocks = new();

    private readonly IReservationRepository _reservations;
    private readonly IParkingLotService _parkingLots;
    private readonly ILicensePlateService _licensePlates;
    private readonly IUserService _users;
    private readonly IUserPlateService _userPlates;
    private readonly IPricingService _pricing;

    public ReservationService(
        IReservationRepository reservations,
        IParkingLotService parkingLots,
        ILicensePlateService licensePlates,
        IUserService users,
        IUserPlateService userPlates,
        IPricingService pricing)
    {
        _reservations = reservations;
        _parkingLots = parkingLots;
        _licensePlates = licensePlates;
        _users = users;
        _userPlates = userPlates;
        _pricing = pricing;
    }

    public async Task<GetReservationCostEstimateResult> GetReservationCostEstimate(ReservationCostEstimateRequest dto, long userId)
    {
        var timeLotValidation = await ValidateTimeWindowAndGetLot(dto.ParkingLotId, dto.StartDate, dto.EndDate);
        if (timeLotValidation is ValidateResult.InvalidTimeWindow tw)
            return new GetReservationCostEstimateResult.InvalidTimeWindow(tw.Message);
        if (timeLotValidation is ValidateResult.LotNotFound)
            return new GetReservationCostEstimateResult.LotNotFound();
        var lotDto = ((ValidateResult.Success)timeLotValidation).Lot;

        var plate = dto.LicensePlate.Upper();
        var userPlatesResult = await _userPlates.GetUserPlatesByUserId(userId);
        if (userPlatesResult is not GetUserPlateListResult.Success platesSuccess ||
            !platesSuccess.Plates.Any(p => p.LicensePlateNumber == plate))
            return new GetReservationCostEstimateResult.Error("License plate not owned by user.");

        var availability = await CheckLotAvailability(dto.ParkingLotId, dto.StartDate, dto.EndDate, lotDto.Capacity);
        if (availability is AvailabilityResult.LotClosed)
            return new GetReservationCostEstimateResult.LotClosed();

        // Pricing based on lot and time window
        var price = _pricing.CalculateParkingCost(new ParkingLotModel
        {
            Id = lotDto.Id,
            Name = lotDto.Name,
            Location = lotDto.Location,
            Address = lotDto.Address,
            Capacity = lotDto.Capacity,
            Reserved = lotDto.Reserved,
            Tariff = lotDto.Tariff,
            DayTariff = lotDto.DayTariff,
        }, dto.StartDate, dto.EndDate);
        if (price is not CalculatePriceResult.Success priceSuccess)
            return new GetReservationCostEstimateResult.Error("Failed to calculate price.");
        return new GetReservationCostEstimateResult.Success(priceSuccess.Price);
    }

    public async Task<CreateReservationResult> CreateReservation(CreateReservationDto dto, long requesterUserId, bool isAdminRequest = false)
    {
        var timeLotValidation = await ValidateTimeWindowAndGetLot(dto.ParkingLotId, dto.StartDate, dto.EndDate);
        if (timeLotValidation is ValidateResult.InvalidTimeWindow tw)
            return new CreateReservationResult.InvalidInput(tw.Message);
        if (timeLotValidation is ValidateResult.LotNotFound)
            return new CreateReservationResult.LotNotFound();
        var lotDto2 = ((ValidateResult.Success)timeLotValidation).Lot;

        long targetUserId = requesterUserId;

        var plate = dto.LicensePlate.Upper();

        // Check that the license plate exists
        var plateLookup = await _licensePlates.GetByLicensePlate(plate);
        if (plateLookup is Services.Results.LicensePlate.GetLicensePlateResult.NotFound)
            return new CreateReservationResult.PlateNotFound();

        // Determine target user (admin create for others, normal users cannot)
        if (!string.IsNullOrWhiteSpace(dto.Username))
        {
            if (!isAdminRequest)
                return new CreateReservationResult.Forbidden();

            var userLookup = await _users.GetUserByUsername(dto.Username);
            if (userLookup is GetUserResult.NotFound)
                return new CreateReservationResult.UserNotFound(dto.Username!);
            targetUserId = ((GetUserResult.Success)userLookup).User.Id;
        }

        // Verify plate ownership
        var ownershipResult = await _userPlates.GetUserPlateByUserIdAndPlate(targetUserId, plate);
        if (ownershipResult is GetUserPlateResult.NotFound)
            return new CreateReservationResult.PlateNotOwned("License plate is not registered to the specified user.");

        var lotLock = LotLocks.GetOrAdd(dto.ParkingLotId, _ => new SemaphoreSlim(1, 1));
        await lotLock.WaitAsync();
        try
        {
            // 1) Plate overlap check at same lot and time window
            var plateReservations = await _reservations.GetByLicensePlate(plate) ?? new List<ReservationModel>();
            bool plateOverlap = plateReservations.Any(r =>
                r.ParkingLotId == dto.ParkingLotId &&
                r.Status != ReservationStatus.Cancelled && r.Status != ReservationStatus.NoShow &&
                r.StartTime < dto.EndDate && dto.StartDate < r.EndTime);
            if (plateOverlap)
                return new CreateReservationResult.AlreadyExists("A reservation already exists for this plate in the selected window.");

            // 2) Capacity check for the lot/time window
            var availability = await CheckLotAvailability(dto.ParkingLotId, dto.StartDate, dto.EndDate, lotDto2.Capacity);
            if (availability is AvailabilityResult.LotClosed)
                return new CreateReservationResult.LotFull();

            // 3) Price and create
            var price = _pricing.CalculateParkingCost(new ParkingLotModel
            {
                Id = lotDto2.Id,
                Name = lotDto2.Name,
                Location = lotDto2.Location,
                Address = lotDto2.Address,
                Capacity = lotDto2.Capacity,
                Reserved = lotDto2.Reserved,
                Tariff = lotDto2.Tariff,
                DayTariff = lotDto2.DayTariff,
            }, dto.StartDate, dto.EndDate);
            if (price is not CalculatePriceResult.Success priceSuccess)
                return new CreateReservationResult.Error("Failed to calculate reservation cost.");

            var reservation = new ReservationModel
            {
                ParkingLotId = dto.ParkingLotId,
                LicensePlateNumber = plate,
                StartTime = dto.StartDate,
                EndTime = dto.EndDate,
                Status = ReservationStatus.Pending,
                Cost = priceSuccess.Price,
                CreatedAt = DateTimeOffset.UtcNow
            };

            (bool ok, long id) = await _reservations.CreateWithId(reservation);
            if (!ok) return new CreateReservationResult.Error("Failed to save reservation.");
            reservation.Id = id;
            return new CreateReservationResult.Success(reservation, priceSuccess.Price);
        }
        finally
        {
            lotLock.Release();
        }
    }

    private enum AvailabilityResult
    {
        Ok,
        LotClosed
    }

    private abstract class ValidateResult
    {
        public sealed class Success : ValidateResult { public ReadParkingLotDto Lot { get; set; } = null!; }
        public sealed class LotNotFound : ValidateResult { }
        public sealed class InvalidTimeWindow : ValidateResult { public string Message { get; set; } = "Invalid time window"; }
    }

    private async Task<ValidateResult> ValidateTimeWindowAndGetLot(long lotId, DateTimeOffset start, DateTimeOffset end)
    {
        if (start >= end)
            return new ValidateResult.InvalidTimeWindow { Message = "Start must be before End." };
        if (start < DateTimeOffset.UtcNow)
            return new ValidateResult.InvalidTimeWindow { Message = "Start cannot be in the past." };

        var lotResult = await _parkingLots.GetParkingLotByIdAsync(lotId);
        if (lotResult.Status != ServiceStatus.Success || lotResult.Data is null)
            return new ValidateResult.LotNotFound();
        return new ValidateResult.Success { Lot = lotResult.Data };
    }

    private async Task<AvailabilityResult> CheckLotAvailability(long lotId, DateTimeOffset start, DateTimeOffset end, int capacity)
    {
        var lotReservations = await _reservations.GetByParkingLotId(lotId) ?? new List<ReservationModel>();
        int overlapCount = lotReservations
            .Where(r => r.Status != ReservationStatus.Cancelled && r.Status != ReservationStatus.NoShow)
            .Count(r => r.StartTime < end && start < r.EndTime);
        if (capacity > 0 && overlapCount >= capacity)
            return AvailabilityResult.LotClosed;
        return AvailabilityResult.Ok;
    }

    public async Task<GetReservationResult> GetReservationById(long id, long requestingUserId)
    {
        var reservation = await _reservations.GetById<ReservationModel>(id);
        if (reservation is null)
            return new GetReservationResult.NotFound();

        var ownershipResult = await _userPlates.GetUserPlateByUserIdAndPlate(requestingUserId, reservation.LicensePlateNumber);
        if (ownershipResult is GetUserPlateResult.NotFound)
            return new GetReservationResult.NotFound();

        return new GetReservationResult.Success(reservation);
    }

    public async Task<GetReservationListResult> GetReservationsByParkingLotId(long parkingLotId, long requestingUserId)
    {
        var lotReservations = await _reservations.GetByParkingLotId(parkingLotId);
        if (lotReservations.Count == 0)
            return new GetReservationListResult.NotFound();

        var userPlatesResult = await _userPlates.GetUserPlatesByUserId(requestingUserId);
        if (userPlatesResult is not GetUserPlateListResult.Success userPlatesSuccess)
            return new GetReservationListResult.NotFound();

        var userPlateNumbers = userPlatesSuccess.Plates.Select(uPlate => uPlate.LicensePlateNumber).ToList();
        var filteredReservations = lotReservations
            .Where(reservation => userPlateNumbers.Contains(reservation.LicensePlateNumber))
            .ToList();

        if (filteredReservations.Count == 0)
            return new GetReservationListResult.NotFound();
        return new GetReservationListResult.Success(filteredReservations);
    }

    public async Task<GetReservationListResult> GetReservationsByLicensePlate(string licensePlate, long requestingUserId)
    {
        string normalizedPlate = licensePlate.Upper();
        var ownershipResult = await _userPlates.GetUserPlateByUserIdAndPlate(requestingUserId, normalizedPlate);
        if (ownershipResult is GetUserPlateResult.NotFound)
            return new GetReservationListResult.NotFound();

        var reservations = await _reservations.GetByLicensePlate(normalizedPlate);
        if (reservations.Count == 0)
            return new GetReservationListResult.NotFound();
        return new GetReservationListResult.Success(reservations);
    }

    public async Task<GetReservationListResult> GetReservationsByStatus(string status, long requestingUserId)
    {
        if (!Enum.TryParse<ReservationStatus>(status, true, out var parsedStatus))
            return new GetReservationListResult.InvalidInput($"'{status}' is not a valid reservation status.");

        var statusReservations = await _reservations.GetByStatus(parsedStatus);
        if (statusReservations.Count == 0)
            return new GetReservationListResult.NotFound();

        var userPlatesResult = await _userPlates.GetUserPlatesByUserId(requestingUserId);
        if (userPlatesResult is not GetUserPlateListResult.Success userPlatesSuccess)
            return new GetReservationListResult.NotFound();

        var userPlateNumbers = userPlatesSuccess.Plates.Select(uPlate => uPlate.LicensePlateNumber).ToHashSet();
        var filteredReservations = statusReservations
            .Where(reservation => userPlateNumbers.Contains(reservation.LicensePlateNumber))
            .ToList();

        if (filteredReservations.Count == 0)
            return new GetReservationListResult.NotFound();
        return new GetReservationListResult.Success(filteredReservations);
    }

    public async Task<GetReservationListResult> GetAllReservations()
    {
        var reservations = await _reservations.GetAll();
        if (reservations.Count == 0)
            return new GetReservationListResult.NotFound();
        return new GetReservationListResult.Success(reservations);
    }

    public async Task<int> CountReservations() => await _reservations.Count();

    public async Task<UpdateReservationResult> UpdateReservation(long reservationId, long requestingUserId, UpdateReservationDto dto)
    {
        var getResult = await GetReservationById(reservationId, requestingUserId);
        if (getResult is not GetReservationResult.Success success)
            return new UpdateReservationResult.NotFound();

        var existingReservation = success.Reservation;

        if (dto.Status.HasValue && dto.Status.Value != existingReservation.Status)
        {
            if (existingReservation.Status == ReservationStatus.Completed)
                return new UpdateReservationResult.Error("Cannot change the status of a completed reservation.");
        }

        var applyResult = ApplyReservationUpdates(existingReservation, dto);

        if (applyResult is not ApplyUpdateResult.Success applyUpdateSuccess)
        {
            return applyResult switch
            {
                ApplyUpdateResult.CannotChangeStartedReservation => new UpdateReservationResult.Error(
                    "Cannot change dates of a reservation that has already started."),
                ApplyUpdateResult.EndTimeBeforeStartTime => new UpdateReservationResult.Error(
                    "End time must be after the start time."),
                ApplyUpdateResult.CannotChangeCampletedStatus => new UpdateReservationResult.Error(
                    "Cannot change the status of a completed reservation."),
                _ => new UpdateReservationResult.Error("An unknown error occurred while applying updates.")
            };
        }

        var updatedReservation = applyUpdateSuccess.UpdatedReservation;
        bool datesChanged = applyUpdateSuccess.DatesChanged;
        bool modelChanged = applyUpdateSuccess.ModelChanged;

        if (!modelChanged)
            return new UpdateReservationResult.NoChangesMade();

        if (datesChanged)
        {
            var lot = await _parkingLots.GetParkingLotByIdAsync(updatedReservation.ParkingLotId);
            if (lot.Status == ServiceStatus.Success)
            {
                var costResult = _pricing.CalculateParkingCost(new ParkingLotModel{Id = lot.Data!.Id}, updatedReservation.StartTime, updatedReservation.EndTime);
                if (costResult is CalculatePriceResult.Success successPrice)
                    updatedReservation.Cost = successPrice.Price;
                else if (costResult is CalculatePriceResult.Error err)
                    return new UpdateReservationResult.Error($"Failed to recalculate cost: {err.Message}");
            }
            else
            {
                return new UpdateReservationResult.Error("Failed to retrieve parking lot for cost recalculation.");
            }
        }

        try
        {
            if (modelChanged)
            {
                bool saved = await _reservations.Update(existingReservation, dto);
                if (!saved)
                    return new UpdateReservationResult.Error("Database update failed.");
            }

            return new UpdateReservationResult.Success(updatedReservation);
        }
        catch (Exception ex)
        { return new UpdateReservationResult.Error(ex.Message); }
    }

    private ApplyUpdateResult ApplyReservationUpdates(ReservationModel existingReservation, UpdateReservationDto dto)
    {
        bool datesChanged = false;
        bool modelChanged = false;

        if (dto.StartTime.HasValue && dto.StartTime.Value != existingReservation.StartTime)
        {
            if (existingReservation.StartTime < DateTimeOffset.UtcNow)
                return new ApplyUpdateResult.CannotChangeStartedReservation();

            existingReservation.StartTime = dto.StartTime.Value;
            datesChanged = true;
            modelChanged = true;
        }

        if (dto.EndTime.HasValue && dto.EndTime.Value != existingReservation.EndTime)
        {
            if (dto.EndTime.Value <= existingReservation.StartTime)
                return new ApplyUpdateResult.EndTimeBeforeStartTime();

            existingReservation.EndTime = dto.EndTime.Value;
            datesChanged = true;
            modelChanged = true;
        }

        if (dto.Status.HasValue && dto.Status.Value != existingReservation.Status)
        {
            if (existingReservation.Status == ReservationStatus.Completed)
                return new ApplyUpdateResult.CannotChangeCampletedStatus();

            existingReservation.Status = dto.Status.Value;
            modelChanged = true;
        }

        return new ApplyUpdateResult.Success(existingReservation, datesChanged, modelChanged);
    }

    public async Task<DeleteReservationResult> DeleteReservation(long id, long requestingUserId)
    {
        var getResult = await GetReservationById(id, requestingUserId);
        if (getResult is GetReservationResult.NotFound)
            return new DeleteReservationResult.NotFound();

        var reservationToDelete = ((GetReservationResult.Success)getResult).Reservation;

        if (reservationToDelete.Status == ReservationStatus.Completed)
            return new DeleteReservationResult.Error("Cannot delete a completed reservation.");

        try
        {
            if (!await _reservations.Delete(reservationToDelete))
                return new DeleteReservationResult.Error("Database deletion failed.");

            return new DeleteReservationResult.Success();
        }
        catch (Exception ex)
        { return new DeleteReservationResult.Error(ex.Message); }
    }
}
