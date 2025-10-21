using MobyPark.DTOs.Reservation.Request;
using MobyPark.Models;
using MobyPark.Models.Repositories.Interfaces;
using MobyPark.Services.Interfaces;
using MobyPark.Services.Results.LicensePlate;
using MobyPark.Services.Results.ParkingLot;
using MobyPark.Services.Results.Price;
using MobyPark.Services.Results.Reservation;
using MobyPark.Services.Results.User;
using MobyPark.Services.Results.UserPlate;
using MobyPark.Validation;

namespace MobyPark.Services;

public class ReservationService : IReservationService
{
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

    public async Task<CreateReservationResult> CreateReservation(ReservationModel reservation)
    {
        (bool createdSuccessfully, long id) = await _reservations.CreateWithId(reservation);
        if (!createdSuccessfully)
            return new CreateReservationResult.Error("Failed to create reservation");
        reservation.Id = id;
        return new CreateReservationResult.Success(reservation);
    }

    public async Task<CreateReservationResult> CreateReservationAsync(CreateReservationDto dto, long requestingUserId, bool isAdminRequest = false)
    {
        var lotValidationResult = await ValidateInputAndFetchLot(dto);
        if (lotValidationResult is not GetLotResult.Success lotSuccess)
        {
            return lotValidationResult switch
            {
                GetLotResult.NotFound => new CreateReservationResult.LotNotFound(),
                GetLotResult.InvalidInput i => new CreateReservationResult.InvalidInput(i.Message), // For invalid dates
                _ => new CreateReservationResult.Error("Failed to validate parking lot.")
            };
        }
        var lot = lotSuccess.Lot;

        string normalizedPlate = ValHelper.NormalizePlate(dto.LicensePlate);
        var userPlateValidationResult = await ResolveTargetUserAndValidatePlate(dto, normalizedPlate, requestingUserId, isAdminRequest);

        if (userPlateValidationResult is not ResolveUserPlateResult.Success)
        {
            return userPlateValidationResult switch
            {
                ResolveUserPlateResult.PlateNotFound => new CreateReservationResult.PlateNotFound(),
                ResolveUserPlateResult.UserNotFound notFound => new CreateReservationResult.UserNotFound(notFound.Username),
                ResolveUserPlateResult.PlateNotOwned notOwned => new CreateReservationResult.PlateNotOwned(notOwned.Message),
                ResolveUserPlateResult.Forbidden forbidden => new CreateReservationResult.Forbidden(forbidden.Message),
                ResolveUserPlateResult.Error err => new CreateReservationResult.Error(err.Message),
                _ => new CreateReservationResult.Error("Failed to validate user or plate ownership.")
            };
        }

        var overlapCheckResult = await CheckForOverlappingReservation(lot.Id, requestingUserId, normalizedPlate, dto.StartDate, dto.EndDate);
        if (overlapCheckResult is not null)
            return overlapCheckResult;

        var costResult = _pricing.CalculateParkingCost(lot, dto.StartDate, dto.EndDate);

        if (costResult is not CalculatePriceResult.Success cost)
            return new CreateReservationResult.Error("Failed to calculate reservation cost.");

        var reservationToCreate = new ReservationModel
        {
            LicensePlateNumber = normalizedPlate,
            ParkingLotId = lot.Id,
            StartTime = dto.StartDate,
            EndTime = dto.EndDate,
            Status = ReservationStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            Cost = cost.Price
        };
        return await PersistReservation(reservationToCreate);
    }

    private async Task<GetLotResult> ValidateInputAndFetchLot(CreateReservationDto dto)
    {
        if (dto.EndDate <= dto.StartDate)
            return new GetLotResult.InvalidInput("End date must be after start date.");
        if (dto.StartDate < DateTime.UtcNow.AddMinutes(-2)) // Small buffer for potential processing time
            return new GetLotResult.InvalidInput("Reservation start date cannot be in the past.");

        return await _parkingLots.GetParkingLotById(dto.ParkingLotId);
    }

    private async Task<ResolveUserPlateResult> ResolveTargetUserAndValidatePlate(
        CreateReservationDto dto, string normalizedPlate, long requestingUserId, bool isAdminRequest)
    {
        var plateResult = await _licensePlates.GetByLicensePlate(normalizedPlate);
        if (plateResult is not GetLicensePlateResult.Success plateSuccess)
            return new ResolveUserPlateResult.PlateNotFound();

        long targetUserId = requestingUserId;
        string? targetUsername = dto.Username;

        if (!string.IsNullOrWhiteSpace(targetUsername))
        {
            if (!isAdminRequest)
                return new ResolveUserPlateResult.Forbidden("Not authorized to create reservations for other users.");

            var userResult = await _users.GetUserByUsername(targetUsername);
            if (userResult is not GetUserResult.Success userSuccess)
                return new ResolveUserPlateResult.UserNotFound(targetUsername);
            targetUserId = userSuccess.User.Id;
        }

        var user = await _users.GetUserById(targetUserId);
        if (user is not GetUserResult.Success getUserSuccess)
            return new ResolveUserPlateResult.UserNotFound($"User ID {targetUserId} not found.");

        var ownershipResult = await _userPlates.GetUserPlateByUserIdAndPlate(targetUserId, normalizedPlate);
        return ownershipResult switch
        {
            GetUserPlateResult.NotFound => new ResolveUserPlateResult.PlateNotOwned(
                $"User {getUserSuccess.User.Username} does not own license plate {normalizedPlate}."),
            GetUserPlateResult.Error errorResult => new ResolveUserPlateResult.Error(
                $"Error checking plate ownership: {errorResult.Message}"),
            _ => new ResolveUserPlateResult.Success(targetUserId, plateSuccess.Plate.LicensePlateNumber)
        };
    }

    private async Task<CreateReservationResult?> CheckForOverlappingReservation(long parkingLotId, long requestingUserId, string licensePlateNumber, DateTime start, DateTime end)
    {
        var existingReservations = await GetReservationsByLicensePlate(licensePlateNumber, requestingUserId);

        return existingReservations switch
        {
            GetReservationListResult.NotFound => null,
            GetReservationListResult.InvalidInput invalidInput => new CreateReservationResult.InvalidInput(invalidInput.Message),
            GetReservationListResult.Success success when success.Reservations.Any(reservation =>
                reservation.ParkingLotId == parkingLotId && reservation.Status != ReservationStatus.Cancelled &&
                reservation.StartTime < end &&
                reservation.EndTime > start) => new CreateReservationResult.AlreadyExists(
                "A reservation already exists for this license plate in the specified time range at this location."),
            _ => null
        };
    }

    private async Task<CreateReservationResult> PersistReservation(ReservationModel reservation)
    {
        try
        {
            (bool success, long id) = await _reservations.CreateWithId(reservation);
             if (!success)
                 return new CreateReservationResult.Error("Failed to save reservation to database.");

            reservation.Id = id;
            var createdReservation = reservation;

            return new CreateReservationResult.Success(createdReservation);
        }
        catch(Exception ex)
        { return new CreateReservationResult.Error($"An error occurred while saving: {ex.Message}"); }
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
        string normalizedPlate = ValHelper.NormalizePlate(licensePlate);
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
        if (getResult is not GetReservationResult.Success s)
            return new UpdateReservationResult.NotFound();

        var existingReservation = s.Reservation;

        if ((dto.StartTime.HasValue && dto.StartTime.Value != existingReservation.StartTime) ||
            (dto.EndTime.HasValue && dto.EndTime.Value != existingReservation.EndTime))
        {
            if (existingReservation.StartTime < DateTime.UtcNow)
                return new UpdateReservationResult.Error(
                    "Cannot change dates of a reservation that has already started.");
        }

        if (dto.EndTime.HasValue && dto.EndTime.Value <= (dto.StartTime ?? existingReservation.StartTime))
            return new UpdateReservationResult.Error("End time must be after the start time.");
        if (dto.Status.HasValue && existingReservation.Status == ReservationStatus.Completed)
            return new UpdateReservationResult.Error("Cannot change the status of a completed reservation.");

        var applyResult = ApplyReservationUpdates(existingReservation, dto);

        if (applyResult is not ApplyUpdateResult.Success applyUpdateSuccess)
        {
            return applyResult switch
            {
                ApplyUpdateResult.CannotChangeStartedReservation => new UpdateReservationResult.Error(
                    "Cannot change dates of a reservation that has already started."),
                ApplyUpdateResult.EndTimeBeforeStartTime => new UpdateReservationResult.Error(
                    "End time must be after the start time."),
                ApplyUpdateResult.CannotChangeStatus => new UpdateReservationResult.Error(
                    "Cannot change the status of a completed reservation."),
                _ => new UpdateReservationResult.Error("An unknown error occurred while applying updates.")
            };
        }

        existingReservation = applyUpdateSuccess.UpdatedReservation;
        bool datesChanged = applyUpdateSuccess.DatesChanged;

        if (datesChanged)
        {
            var lotResult = await _parkingLots.GetParkingLotById(existingReservation.ParkingLotId);
            if (lotResult is GetLotResult.Success sLot)
            {
                var costResult = _pricing.CalculateParkingCost(sLot.Lot, existingReservation.StartTime, existingReservation.EndTime);
                if (costResult is CalculatePriceResult.Success c)
                    existingReservation.Cost = c.Price;

                else if (costResult is CalculatePriceResult.Error e)
                    return new UpdateReservationResult.Error($"Failed to recalculate cost: {e.Message}");
            }
            else
                return new UpdateReservationResult.Error("Failed to retrieve parking lot for cost recalculation.");
        }

        try
        {
            if (datesChanged || (dto.Status.HasValue && dto.Status.Value != s.Reservation.Status))
            {
                if (!await _reservations.Update(existingReservation))
                    return new UpdateReservationResult.Error("Database update failed.");
            }

            return new UpdateReservationResult.Success(existingReservation);
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
            if (existingReservation.StartTime < DateTime.UtcNow)
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
                return new ApplyUpdateResult.CannotChangeStatus();

            existingReservation.Status = dto.Status.Value;
            modelChanged = true;
        }

        return modelChanged
            ? new ApplyUpdateResult.Success(existingReservation, datesChanged)
            : new ApplyUpdateResult.Success(existingReservation, false);
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
