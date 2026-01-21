using Microsoft.AspNetCore.Authorization;

using MobyPark.DTOs.ParkingLot.Request;
using MobyPark.Models;
using MobyPark.Models.Repositories.Interfaces;
using MobyPark.Services.Interfaces;
using MobyPark.Services.Results;

namespace MobyPark.Services;

public class ParkingLotService : IParkingLotService
{
    private readonly IRepository<ParkingLotModel> _parkingRepo;
    private readonly IRepository<HotelPassModel> _hotelRepo;
    private readonly IRepository<ParkingSessionModel> _sessionRepo;
    private readonly IRepository<ReservationModel> _reservationRepo;


    public ParkingLotService(IRepository<ParkingLotModel> parkingLots, IRepository<ParkingSessionModel> sessionRepo, IRepository<ReservationModel> reservationRepo, IRepository<HotelPassModel> hotelRepo)
    {
        _parkingRepo = parkingLots;
        _sessionRepo = sessionRepo;
        _reservationRepo = reservationRepo;
        _hotelRepo = hotelRepo;
    }

    public async Task<ServiceResult<ReadParkingLotDto>> GetParkingLotByAddressAsync(string address)
    {
        try
        {
            var normalized = address.Trim().ToLower();
            var lot = (await _parkingRepo.GetByAsync(x => x.Address.ToLower() == normalized)).FirstOrDefault();
            if (lot is null) return ServiceResult<ReadParkingLotDto>.NotFound($"No parking lot found at address {address}");

            return ServiceResult<ReadParkingLotDto>.Ok(new ReadParkingLotDto
            {
                Id = lot.Id,
                Name = lot.Name,
                Location = lot.Location,
                Address = lot.Address,
                Reserved = lot.Reserved,
                Capacity = lot.Capacity,
                Tariff = lot.Tariff,
                DayTariff = lot.DayTariff
            });
        }
        catch (Exception)
        {
            return ServiceResult<ReadParkingLotDto>.Exception("Unexpected error occurred.");
        }

    }

    public async Task<ServiceResult<ReadParkingLotDto>> GetParkingLotByIdAsync(long id)
    {
        try
        {
            var lot = await _parkingRepo.FindByIdAsync(id);
            if (lot is null) return ServiceResult<ReadParkingLotDto>.NotFound($"No lot with id: {id} found.");

            return ServiceResult<ReadParkingLotDto>.Ok(new ReadParkingLotDto
            {
                Id = lot.Id,
                Name = lot.Name,
                Location = lot.Location,
                Address = lot.Address,
                Reserved = lot.Reserved,
                Capacity = lot.Capacity,
                Tariff = lot.Tariff,
                DayTariff = lot.DayTariff
            });
        }
        catch (Exception)
        {
            return ServiceResult<ReadParkingLotDto>.Fail("Unexpected error occurred.");
        }

    }

    [Authorize("CanManageParkingLots")]
    public async Task<ServiceResult<ReadParkingLotDto>> CreateParkingLotAsync(CreateParkingLotDto parkingLot)
    {
        var normalized = parkingLot.Address.Trim().ToLower();

        try
        {
            var exists = (await _parkingRepo.GetByAsync(x => x.Address.ToLower() == normalized)).FirstOrDefault();
            if (exists is not null) return ServiceResult<ReadParkingLotDto>.Fail("Address taken");

            var lot = new ParkingLotModel
            {
                Name = parkingLot.Name,
                Location = parkingLot.Location,
                Address = parkingLot.Address,
                Capacity = parkingLot.Capacity,
                Reserved = 0,
                Tariff = parkingLot.Tariff,
                DayTariff = parkingLot.DayTariff
            };

            _parkingRepo.Add(lot);
            await _parkingRepo.SaveChangesAsync();

            return ServiceResult<ReadParkingLotDto>.Ok(new ReadParkingLotDto
            {
                Id = lot.Id,
                Name = lot.Name,
                Location = lot.Location,
                Address = lot.Address,
                Reserved = lot.Reserved,
                Capacity = lot.Capacity,
                Tariff = lot.Tariff,
                DayTariff = lot.DayTariff
            })
            ;
        }
        catch (Exception)
        {
            return ServiceResult<ReadParkingLotDto>.Fail("Unexpected error occurred.");
        }
    }

    [Authorize("CanManageParkingLots")]
    public async Task<ServiceResult<ReadParkingLotDto>> PatchParkingLotByAddressAsync(string address, PatchParkingLotDto updateLot)
    {
        var normalized = address.Trim().ToLower();
        try
        {
            var exists = (await _parkingRepo.GetByAsync(x => x.Address.ToLower() == normalized)).FirstOrDefault();
            if (exists is null)
                return ServiceResult<ReadParkingLotDto>.NotFound("No parking lot was found with that address");

            if (updateLot.Address is not null)
            {
                var newAddressNormalized = updateLot.Address.Trim().ToLower();
                var addressTaken = (await _parkingRepo.GetByAsync(x => x.Address.ToLower() == newAddressNormalized && x.Id != exists.Id)).FirstOrDefault();
                if (addressTaken is not null) return ServiceResult<ReadParkingLotDto>.BadRequest("There is already a parking lot assigned to the new address.");
            }

            if (!string.IsNullOrWhiteSpace(updateLot.Name)) exists.Name = updateLot.Name;
            if (!string.IsNullOrWhiteSpace(updateLot.Location)) exists.Location = updateLot.Location;
            if (!string.IsNullOrWhiteSpace(updateLot.Address)) exists.Address = updateLot.Address;
            if (updateLot.Capacity.HasValue) exists.Capacity = updateLot.Capacity.Value;
            if (updateLot.Tariff.HasValue) exists.Tariff = updateLot.Tariff.Value;
            if (updateLot.DayTariff.HasValue) exists.DayTariff = updateLot.DayTariff.Value;

            _parkingRepo.Update(exists);
            await _parkingRepo.SaveChangesAsync();
            return ServiceResult<ReadParkingLotDto>.Ok(new ReadParkingLotDto
            {
                Id = exists.Id,
                Name = exists.Name,
                Location = exists.Location,
                Address = exists.Address,
                Reserved = exists.Reserved,
                Capacity = exists.Capacity,
                Tariff = exists.Tariff,
                DayTariff = exists.DayTariff
            });
        }

        catch (Exception)
        {
            return ServiceResult<ReadParkingLotDto>.Exception("Unexpected error occurred.");
        }
    }

    [Authorize("CanManageParkingLots")]
    public async Task<ServiceResult<ReadParkingLotDto>> PatchParkingLotByIdAsync(long id, PatchParkingLotDto updateLot)
    {
        try
        {
            var exists = await _parkingRepo.FindByIdAsync(id);
            if (exists is null)
                return ServiceResult<ReadParkingLotDto>.NotFound("No parking lot was found with that id");

            if (updateLot.Address is not null)
            {
                var newAddressNormalized = updateLot.Address.Trim().ToLower();
                var addressTaken = (await _parkingRepo.GetByAsync(x => x.Address.ToLower() == newAddressNormalized && x.Id != exists.Id)).FirstOrDefault();
                if (addressTaken is not null) return ServiceResult<ReadParkingLotDto>.BadRequest("There is already a parking lot assigned to the new address.");
            }

            if (!string.IsNullOrWhiteSpace(updateLot.Name)) exists.Name = updateLot.Name;
            if (!string.IsNullOrWhiteSpace(updateLot.Location)) exists.Location = updateLot.Location;
            if (!string.IsNullOrWhiteSpace(updateLot.Address)) exists.Address = updateLot.Address;
            if (updateLot.Capacity.HasValue) exists.Capacity = updateLot.Capacity.Value;
            if (updateLot.Tariff.HasValue) exists.Tariff = updateLot.Tariff.Value;
            if (updateLot.DayTariff.HasValue) exists.DayTariff = updateLot.DayTariff.Value;

            _parkingRepo.Update(exists);
            await _parkingRepo.SaveChangesAsync();
            return ServiceResult<ReadParkingLotDto>.Ok(new ReadParkingLotDto
            {
                Id = exists.Id,
                Name = exists.Name,
                Location = exists.Location,
                Address = exists.Address,
                Reserved = exists.Reserved,
                Capacity = exists.Capacity,
                Tariff = exists.Tariff,
                DayTariff = exists.DayTariff
            });
        }

        catch (Exception)
        {
            return ServiceResult<ReadParkingLotDto>.Exception("Unexpected error occurred.");
        }
    }

    [Authorize("CanManageParkingLots")]
    public async Task<ServiceResult<bool>> DeleteParkingLotByIdAsync(long id)
    {
        try
        {
            var exists = await _parkingRepo.FindByIdAsync(id);
            if (exists is null) return ServiceResult<bool>.NotFound("No lot found with that id. Deletion failed.");

            _parkingRepo.Deletee(exists);
            await _parkingRepo.SaveChangesAsync();
            return ServiceResult<bool>.Ok(true);
        }
        catch (Exception)
        {
            return ServiceResult<bool>.Fail("Unexpected error occurred.");
        }
    }

    [Authorize("CanManageParkingLots")]
    public async Task<ServiceResult<bool>> DeleteParkingLotByAddressAsync(string address)
    {
        try
        {
            var normalized = address.Trim().ToLower();
            var exists = (await _parkingRepo.GetByAsync(x => x.Address.ToLower() == normalized)).FirstOrDefault();
            if (exists is null) return ServiceResult<bool>.NotFound("No lot found with that address. Deletion failed.");

            _parkingRepo.Deletee(exists);
            await _parkingRepo.SaveChangesAsync();
            return ServiceResult<bool>.Ok(true);
        }
        catch (Exception)
        {
            return ServiceResult<bool>.Fail("Unexpected error occurred.");
        }
    }

    public async Task<ServiceResult<List<ReadParkingLotDto>>> GetAllParkingLotsAsync()
    {
        try
        {
            var lots = await _parkingRepo.ReadAllAsync();
            var lotList = lots.Select(p => new ReadParkingLotDto
            {
                Id = p.Id,
                Name = p.Name,
                Location = p.Location,
                Address = p.Address,
                Reserved = p.Reserved,
                Capacity = p.Capacity,
                Tariff = p.Tariff,
                DayTariff = p.DayTariff
            }).ToList();

            return ServiceResult<List<ReadParkingLotDto>>.Ok(lotList);

        }
        catch (Exception)
        {
            return ServiceResult<List<ReadParkingLotDto>>.Fail("Unexpected error occurred.");
        }
    }

    public async Task<ServiceResult<int>> GetAvailableSpotsByLotIdAsync(long id)
    {
        var lot = await _parkingRepo.FindByIdAsync(id);
        if (lot is null) return ServiceResult<int>.NotFound("Parking lot not found");

        var now = DateTime.UtcNow;

        int activeSessions = (await _sessionRepo
                .GetByAsync(x => x.ParkingLotId == lot.Id && !x.Stopped.HasValue))
            .Count();

        int activeReservations = (await _reservationRepo
                .GetByAsync(x =>
                    x.ParkingLotId == lot.Id &&
                    (x.Status == ReservationStatus.Pending ||
                     x.Status == ReservationStatus.Confirmed) &&
                    x.StartTime < now &&
                    x.EndTime > now))
            .Count();

        int activeHotelPasses = (await _hotelRepo
                .GetByAsync(x =>
                    x.ParkingLotId == lot.Id &&
                    x.Start < now &&
                    (x.End + x.ExtraTime) > now))
            .Count();

        int occupied = activeSessions + activeReservations + activeHotelPasses;
        int availableSpots = lot.Capacity - occupied;
        if (availableSpots < 0)
            availableSpots = 0;

        return ServiceResult<int>.Ok(availableSpots);
    }
    public async Task<ServiceResult<int>> GetAvailableSpotsByAddressAsync(string address)
    {
        var trimmedAddress = address.Trim().ToLower();
        var lot = (await _parkingRepo
                .GetByAsync(x => x.Address.ToLower() == trimmedAddress))
            .FirstOrDefault();

        if (lot is null) return ServiceResult<int>.NotFound("Parking lot not found");

        var now = DateTime.UtcNow;

        int activeSessions = (await _sessionRepo
                .GetByAsync(x => x.ParkingLotId == lot.Id && !x.Stopped.HasValue))
            .Count();

        int activeReservations = (await _reservationRepo
                .GetByAsync(x =>
                    x.ParkingLotId == lot.Id &&
                    (x.Status == ReservationStatus.Pending ||
                     x.Status == ReservationStatus.Confirmed) &&
                    x.StartTime < now &&
                    x.EndTime > now))
            .Count();

        int activeHotelPasses = (await _hotelRepo
                .GetByAsync(x =>
                    x.ParkingLotId == lot.Id &&
                    x.Start < now &&
                    (x.End + x.ExtraTime) > now))
            .Count();

        int occupied = activeSessions + activeReservations + activeHotelPasses;
        int availableSpots = lot.Capacity - occupied;
        if (availableSpots < 0)
            availableSpots = 0;

        return ServiceResult<int>.Ok(availableSpots);
    }

    public async Task<ServiceResult<int>> GetAvailableSpotsForPeriodAsync(
        long lotId,
        DateTimeOffset start,
        DateTimeOffset end)
    {
        var lot = await _parkingRepo.FindByIdAsync(lotId);
        if (lot is null)
            return ServiceResult<int>.NotFound("Parking lot not found");

        if (end <= start)
            return ServiceResult<int>.BadRequest("End time must be after start time.");

        var overlappingSessions = await _sessionRepo.GetByAsync(x =>
            x.ParkingLotId == lotId &&
            // sessie is (nog) niet gestopt of stopt na de start
            (!x.Stopped.HasValue || x.Stopped.Value > start) &&
            // sessie is gestart voor het einde van de gevraage periode
            x.Started < end
        );
        int activeSessions = overlappingSessions.Count();

        var overlappingReservations = await _reservationRepo.GetByAsync(x =>
            x.ParkingLotId == lotId &&
            (x.Status == ReservationStatus.Pending ||
             x.Status == ReservationStatus.Confirmed) &&
            x.StartTime < end &&
            x.EndTime > start
        );
        int activeReservations = overlappingReservations.Count();

        var overlappingHotelPasses = await _hotelRepo.GetByAsync(x =>
            x.ParkingLotId == (int)lotId &&
            x.Start < end &&
            (x.End + x.ExtraTime) > start
        );
        int activeHotelPasses = overlappingHotelPasses.Count();

        int occupied = activeSessions + activeReservations + activeHotelPasses;
        int availableSpots = lot.Capacity - occupied;
        if (availableSpots < 0)
            availableSpots = 0;

        return ServiceResult<int>.Ok(availableSpots);
    }
}