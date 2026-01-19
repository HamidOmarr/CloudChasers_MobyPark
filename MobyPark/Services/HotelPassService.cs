using MobyPark.DTOs.Hotel;
using MobyPark.Models;
using MobyPark.Models.Repositories.Interfaces;
using MobyPark.Services.Interfaces;
using MobyPark.Services.Results;

namespace MobyPark.Services;

public class HotelPassService : IHotelPassService
{
    private readonly IRepository<HotelPassModel> _passRepo;
    private readonly IParkingLotService _lotService;
    private readonly IRepository<UserModel> _userRepo;
    private readonly IRepository<HotelModel> _hotelRepo;
    private readonly IRepository<ParkingLotModel> _lotRepo;

    public HotelPassService(IRepository<HotelPassModel> passRepo, IParkingLotService lotService, IRepository<UserModel> userRepo, IRepository<HotelModel> hotelRepo, IRepository<ParkingLotModel> lotRepo)
    {
        _passRepo = passRepo;
        _lotService = lotService;
        _userRepo = userRepo;
        _hotelRepo = hotelRepo;
        _lotRepo = lotRepo;
    }

    public async Task<ServiceResult<ReadHotelPassDto>> GetHotelPassByIdAsync(long id)
    {
        try
        {
            var pass = await _passRepo.FindByIdAsync(id);
            if (pass is null) return ServiceResult<ReadHotelPassDto>.NotFound("No hotel pass with that id found");
            return ServiceResult<ReadHotelPassDto>.Ok(new ReadHotelPassDto
            {
                Id = id,
                LicensePlate = pass.LicensePlateNumber,
                ParkingLotId = pass.ParkingLotId,
                Start = pass.Start,
                End = pass.End,
                ExtraTime = pass.ExtraTime
            });
        }
        catch (Exception)
        {
            return ServiceResult<ReadHotelPassDto>.Exception("Unexpected error occurred.");
        }

    }

    public async Task<ServiceResult<List<ReadHotelPassDto>>> GetHotelPassesByParkingLotIdAsync(long parkingLotId)
    {
        try
        {
            var passes = await _passRepo.GetByAsync(x => x.ParkingLotId == parkingLotId);
            if (!passes.Any()) return ServiceResult<List<ReadHotelPassDto>>.NotFound($"Parking lot with id {parkingLotId} has no hotel passes.");
            var dtoList = passes.Select(x => new ReadHotelPassDto
            {
                Id = x.Id,
                LicensePlate = x.LicensePlateNumber,
                ParkingLotId = x.ParkingLotId,
                Start = x.Start,
                End = x.End,
                ExtraTime = x.ExtraTime
            }).ToList();

            return ServiceResult<List<ReadHotelPassDto>>.Ok(dtoList);
        }
        catch (Exception)
        {
            return ServiceResult<List<ReadHotelPassDto>>.Exception("Unexpected error occurred.");
        }
    }

    public async Task<ServiceResult<List<ReadHotelPassDto>>> GetHotelPassesByLicensePlateAsync(string licensePlate)
    {
        try
        {
            var passes = await _passRepo.GetByAsync(x => x.LicensePlateNumber == licensePlate);
            if (!passes.Any()) return ServiceResult<List<ReadHotelPassDto>>.NotFound($"License plate {licensePlate} has no hotel passes.");
            var dtoList = passes.Select(x => new ReadHotelPassDto
            {
                Id = x.Id,
                LicensePlate = x.LicensePlateNumber,
                ParkingLotId = x.ParkingLotId,
                Start = x.Start,
                End = x.End,
                ExtraTime = x.ExtraTime
            }).ToList();

            return ServiceResult<List<ReadHotelPassDto>>.Ok(dtoList);
        }
        catch (Exception)
        {
            return ServiceResult<List<ReadHotelPassDto>>.Exception("Unexpected error occurred.");
        }
    }

    public async Task<ServiceResult<List<ReadHotelPassDto>>> GetHotelPassesByLicensePlateAndLotIdAsync(long parkingLotId,
        string licensePlate)
    {
        try
        {
            var pass = await _passRepo.GetByAsync(x =>
                x.ParkingLotId == parkingLotId && x.LicensePlateNumber == licensePlate);

            if (!pass.Any()) return ServiceResult<List<ReadHotelPassDto>>.NotFound("No hotel pass found for this license plate and lot id");

            var dtoList = pass.Select(x => new ReadHotelPassDto
            {
                Id = x.Id,
                LicensePlate = x.LicensePlateNumber,
                ParkingLotId = x.ParkingLotId,
                Start = x.Start,
                End = x.End,
                ExtraTime = x.ExtraTime
            }).ToList();

            return ServiceResult<List<ReadHotelPassDto>>.Ok(dtoList);
        }
        catch (Exception)
        {
            return ServiceResult<List<ReadHotelPassDto>>.Exception("Unexpected error occurred.");
        }
    }

    public async Task<ServiceResult<ReadHotelPassDto>> GetActiveHotelPassByLicensePlateAndLotIdAsync(long parkingLotId, string licensePlate)
    {
        try
        {
            var now = DateTime.UtcNow;
            var pass = (await _passRepo
                    .GetByAsync(x => x.ParkingLotId == parkingLotId && x.LicensePlateNumber == licensePlate && x.Start >= now && x.End + x.ExtraTime <= now))
                .FirstOrDefault(); //Ik ga ervanuit dat 1 kenteken maar 1 actieve hotel pass kan hebben bij een bepaald hotel/parkinglot.
            if (pass is null)
                return ServiceResult<ReadHotelPassDto>.NotFound(
                    $"No active hotel pass found for license plate {licensePlate} at parking lot with id {parkingLotId}");
            return ServiceResult<ReadHotelPassDto>.Ok(new ReadHotelPassDto
            {
                Id = pass.Id,
                LicensePlate = pass.LicensePlateNumber,
                ParkingLotId = pass.ParkingLotId,
                Start = pass.Start,
                End = pass.End,
                ExtraTime = pass.ExtraTime
            });
        }
        catch (Exception)
        {
            return ServiceResult<ReadHotelPassDto>.Exception("Unexpected error occurred.");
        }

    }

    public async Task<ServiceResult<ReadHotelPassDto>> CreateHotelPassAsync(AdminCreateHotelPassDto pass)
    {
        try
        {
            string plate = pass.LicensePlate.Trim().ToUpperInvariant();

            DateTime reservationStart = pass.Start.ToUniversalTime();
            DateTime reservationEnd = pass.End.ToUniversalTime();

            if (reservationEnd <= reservationStart)
                return ServiceResult<ReadHotelPassDto>.BadRequest("End must be after Start.");

            DateTime reservationEndWithExtra = reservationEnd + pass.ExtraTime;

            var overlappingExistingPass = (await _passRepo.GetByAsync(x =>
                    x.LicensePlateNumber == plate &&
                    x.ParkingLotId == pass.ParkingLotId &&
                    x.Start < reservationEndWithExtra &&
                    (x.End + x.ExtraTime) > reservationStart
                ))
                .FirstOrDefault();

            if (overlappingExistingPass is not null)
            {
                return ServiceResult<ReadHotelPassDto>.Conflict(
                    $"There is already a hotel pass for {plate} during this period.");
            }

            var availability = await _lotService.GetAvailableSpotsForPeriodAsync(
                pass.ParkingLotId,
                reservationStart,
                reservationEndWithExtra);

            if (availability.Status != ServiceStatus.Success)
                return ServiceResult<ReadHotelPassDto>.Fail(
                    availability.Error ?? "Failed to check parking lot availability.");

            if (availability.Data <= 0)
            {
                return ServiceResult<ReadHotelPassDto>.BadRequest(
                    "No available spots for the selected time period.");
            }

            var newPass = new HotelPassModel
            {
                LicensePlateNumber = plate,
                ParkingLotId = pass.ParkingLotId,
                Start = reservationStart,
                End = reservationEnd,
                ExtraTime = pass.ExtraTime
            };

            _passRepo.Add(newPass);
            await _passRepo.SaveChangesAsync();

            return ServiceResult<ReadHotelPassDto>.Ok(new ReadHotelPassDto
            {
                Id = newPass.Id,
                LicensePlate = newPass.LicensePlateNumber,
                ParkingLotId = newPass.ParkingLotId,
                Start = newPass.Start,
                End = newPass.End,
                ExtraTime = newPass.ExtraTime
            });
        }
        catch (Exception)
        {
            return ServiceResult<ReadHotelPassDto>.Exception("Unexpected error occurred.");
        }
    }

    public async Task<ServiceResult<ReadHotelPassDto>> CreateHotelPassAsync(CreateHotelPassDto pass, long currentUserId)
    {
        try
        {
            var user = await _userRepo.FindByIdAsync(currentUserId);
            if (user is null) return ServiceResult<ReadHotelPassDto>.NotFound("user not found");
            if (user.HotelId is null)
            {
                return ServiceResult<ReadHotelPassDto>.Conflict(
                    "This user is not authorized to create hotel passes for any hotel");
            }
            var hotel = await _hotelRepo.FindByIdAsync(user.HotelId);
            if (hotel is null) return ServiceResult<ReadHotelPassDto>.NotFound("hotel not found");
            var parkingLot = await _lotRepo.FindByIdAsync(hotel.HotelParkingLotId);
            if (parkingLot is null) return ServiceResult<ReadHotelPassDto>.NotFound("parking lot not found");

            string plate = pass.LicensePlate.Trim().ToUpperInvariant();

            DateTime reservationStart = pass.Start.ToUniversalTime();
            DateTime reservationEnd = pass.End.ToUniversalTime();

            if (reservationEnd <= reservationStart)
                return ServiceResult<ReadHotelPassDto>.BadRequest("End must be after Start.");

            DateTime reservationEndWithExtra = reservationEnd + pass.ExtraTime;

            var overlappingExistingPass = (await _passRepo.GetByAsync(x =>
                    x.LicensePlateNumber == plate &&
                    x.ParkingLotId == parkingLot.Id &&
                    x.Start < reservationEndWithExtra &&
                    (x.End + x.ExtraTime) > reservationStart
                ))
                .FirstOrDefault();

            if (overlappingExistingPass is not null)
            {
                return ServiceResult<ReadHotelPassDto>.Conflict(
                    $"There is already a hotel pass for {plate} during this period.");
            }

            var availability = await _lotService.GetAvailableSpotsForPeriodAsync(
                parkingLot.Id,
                reservationStart,
                reservationEndWithExtra);

            if (availability.Status != ServiceStatus.Success)
                return ServiceResult<ReadHotelPassDto>.Fail(
                    availability.Error ?? "Failed to check parking lot availability.");

            if (availability.Data <= 0)
            {
                return ServiceResult<ReadHotelPassDto>.BadRequest(
                    "No available spots for the selected time period.");
            }

            var newPass = new HotelPassModel
            {
                LicensePlateNumber = plate,
                ParkingLotId = parkingLot.Id,
                Start = reservationStart,
                End = reservationEnd,
                ExtraTime = pass.ExtraTime
            };

            _passRepo.Add(newPass);
            await _passRepo.SaveChangesAsync();

            return ServiceResult<ReadHotelPassDto>.Ok(new ReadHotelPassDto
            {
                Id = newPass.Id,
                LicensePlate = newPass.LicensePlateNumber,
                ParkingLotId = newPass.ParkingLotId,
                Start = newPass.Start,
                End = newPass.End,
                ExtraTime = newPass.ExtraTime
            });
        }
        catch (Exception)
        {
            return ServiceResult<ReadHotelPassDto>.Exception("Unexpected error occurred.");
        }
    }

    public async Task<ServiceResult<ReadHotelPassDto>> PatchHotelPassAsync(PatchHotelPassDto pass)
    {
        try
        {
            var existingPass = await _passRepo.FindByIdAsync(pass.Id);
            if (existingPass is null)
                return ServiceResult<ReadHotelPassDto>.NotFound($"Update failed. No pass with id {pass.Id} found");

            if (!string.IsNullOrWhiteSpace(pass.LicensePlate))
                existingPass.LicensePlateNumber = pass.LicensePlate.Trim().ToUpperInvariant();

            if (pass.Start.HasValue)
                existingPass.Start = pass.Start.Value.ToUniversalTime();

            if (pass.End.HasValue)
                existingPass.End = pass.End.Value.ToUniversalTime();

            if (existingPass.End <= existingPass.Start)
                return ServiceResult<ReadHotelPassDto>.BadRequest("End must be after Start.");

            if (pass.ExtraTime.HasValue)
                existingPass.ExtraTime = pass.ExtraTime.Value;

            DateTime updatedEndWithExtra = existingPass.End + existingPass.ExtraTime;

            var overlapping = (await _passRepo.GetByAsync(x =>
                    x.Id != existingPass.Id &&
                    x.ParkingLotId == existingPass.ParkingLotId &&
                    x.LicensePlateNumber == existingPass.LicensePlateNumber &&
                    x.Start < updatedEndWithExtra &&
                    (x.End + x.ExtraTime) > existingPass.Start
                ))
                .FirstOrDefault();

            if (overlapping is not null)
            {
                return ServiceResult<ReadHotelPassDto>.Conflict(
                    $"Another hotel pass overlaps with the selected period.");
            }

            var availability = await _lotService.GetAvailableSpotsForPeriodAsync(
                existingPass.ParkingLotId,
                existingPass.Start,
                updatedEndWithExtra);

            if (availability.Status != ServiceStatus.Success)
                return ServiceResult<ReadHotelPassDto>.Fail(availability.Error!);

            if (availability.Data <= 0)
                return ServiceResult<ReadHotelPassDto>.BadRequest("No available spots for the selected time period.");

            _passRepo.Update(existingPass);
            await _passRepo.SaveChangesAsync();

            return ServiceResult<ReadHotelPassDto>.Ok(new ReadHotelPassDto
            {
                Id = existingPass.Id,
                LicensePlate = existingPass.LicensePlateNumber,
                ParkingLotId = existingPass.ParkingLotId,
                Start = existingPass.Start,
                End = existingPass.End,
                ExtraTime = existingPass.ExtraTime
            });
        }
        catch (Exception)
        {
            return ServiceResult<ReadHotelPassDto>.Exception("Unexpected error occurred.");
        }
    }

    public async Task<ServiceResult<ReadHotelPassDto>> PatchHotelPassAsync(PatchHotelPassDto pass, long currentUserId)
    {
        try
        {
            var user = await _userRepo.FindByIdAsync(currentUserId);
            if (user is null) return ServiceResult<ReadHotelPassDto>.NotFound("user not found");
            if (user.HotelId is null)
            {
                return ServiceResult<ReadHotelPassDto>.Conflict(
                    "This user is not authorized to create hotel passes for any hotel");
            }
            var hotel = await _hotelRepo.FindByIdAsync(user.HotelId);
            if (hotel is null) return ServiceResult<ReadHotelPassDto>.NotFound("hotel not found");
            var parkingLot = await _lotRepo.FindByIdAsync(hotel.HotelParkingLotId);
            if (parkingLot is null) return ServiceResult<ReadHotelPassDto>.NotFound("parking lot not found");

            var existingPass = await _passRepo.FindByIdAsync(pass.Id);
            if (existingPass is null)
                return ServiceResult<ReadHotelPassDto>.NotFound($"Update failed. No pass with id {pass.Id} found");
            if (existingPass.ParkingLotId != parkingLot.Id)
                return ServiceResult<ReadHotelPassDto>.Forbidden(
                    $"Can only update a pass for your authorized parking lot with id {parkingLot.Id}");

            if (!string.IsNullOrWhiteSpace(pass.LicensePlate))
                existingPass.LicensePlateNumber = pass.LicensePlate.Trim().ToUpperInvariant();

            if (pass.Start.HasValue)
                existingPass.Start = pass.Start.Value.ToUniversalTime();

            if (pass.End.HasValue)
                existingPass.End = pass.End.Value.ToUniversalTime();

            if (existingPass.End <= existingPass.Start)
                return ServiceResult<ReadHotelPassDto>.BadRequest("End must be after Start.");

            if (pass.ExtraTime.HasValue)
                existingPass.ExtraTime = pass.ExtraTime.Value;

            DateTime updatedEndWithExtra = existingPass.End + existingPass.ExtraTime;

            var overlapping = (await _passRepo.GetByAsync(x =>
                    x.Id != existingPass.Id &&
                    x.ParkingLotId == existingPass.ParkingLotId &&
                    x.LicensePlateNumber == existingPass.LicensePlateNumber &&
                    x.Start < updatedEndWithExtra &&
                    (x.End + x.ExtraTime) > existingPass.Start
                ))
                .FirstOrDefault();

            if (overlapping is not null)
            {
                return ServiceResult<ReadHotelPassDto>.Conflict(
                    $"Another hotel pass overlaps with the selected period.");
            }

            var availability = await _lotService.GetAvailableSpotsForPeriodAsync(
                existingPass.ParkingLotId,
                existingPass.Start,
                updatedEndWithExtra);

            if (availability.Status != ServiceStatus.Success)
                return ServiceResult<ReadHotelPassDto>.Fail(availability.Error!);

            if (availability.Data <= 0)
                return ServiceResult<ReadHotelPassDto>.BadRequest("No available spots for the selected time period.");

            _passRepo.Update(existingPass);
            await _passRepo.SaveChangesAsync();

            return ServiceResult<ReadHotelPassDto>.Ok(new ReadHotelPassDto
            {
                Id = existingPass.Id,
                LicensePlate = existingPass.LicensePlateNumber,
                ParkingLotId = existingPass.ParkingLotId,
                Start = existingPass.Start,
                End = existingPass.End,
                ExtraTime = existingPass.ExtraTime
            });
        }
        catch (Exception)
        {
            return ServiceResult<ReadHotelPassDto>.Exception("Unexpected error occurred.");
        }
    }

    public async Task<ServiceResult<bool>> DeleteHotelPassByIdAsync(long id)
    {
        try
        {
            var pass = await _passRepo.FindByIdAsync(id);
            if (pass is null) return ServiceResult<bool>.NotFound($"No hotel pass with id {id} found.");

            _passRepo.Deletee(pass);
            await _passRepo.SaveChangesAsync();
            return ServiceResult<bool>.Ok(true);
        }
        catch (Exception)
        {
            return ServiceResult<bool>.Exception("Unexpected error occurred.");
        }
    }

    public async Task<ServiceResult<bool>> DeleteHotelPassByIdAsync(long id, long currentUserId)
    {
        try
        {
            var user = await _userRepo.FindByIdAsync(currentUserId);
            if (user is null) return ServiceResult<bool>.NotFound("user not found");
            if (user.HotelId is null)
            {
                return ServiceResult<bool>.Conflict(
                    "This user is not authorized to delete hotel passes for any hotel");
            }
            var hotel = await _hotelRepo.FindByIdAsync(user.HotelId);
            if (hotel is null) return ServiceResult<bool>.NotFound("hotel not found");
            var parkingLot = await _lotRepo.FindByIdAsync(hotel.HotelParkingLotId);
            if (parkingLot is null) return ServiceResult<bool>.NotFound("parking lot not found");

            var pass = await _passRepo.FindByIdAsync(id);
            if (pass is null) return ServiceResult<bool>.NotFound($"No hotel pass with id {id} found.");

            if (parkingLot.Id != pass.ParkingLot.Id)
                return ServiceResult<bool>.Forbidden("Hotel is only authorized to delete its own hotel passes");

            _passRepo.Deletee(pass);
            await _passRepo.SaveChangesAsync();
            return ServiceResult<bool>.Ok(true);
        }
        catch (Exception)
        {
            return ServiceResult<bool>.Exception("Unexpected error occurred.");
        }
    }
}