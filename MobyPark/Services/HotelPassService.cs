using MobyPark.DTOs.Hotel;
using MobyPark.Models;
using MobyPark.Models.Repositories.Interfaces;
using MobyPark.Services.Interfaces;
using MobyPark.Services.Results;

namespace MobyPark.Services;

public class HotelPassService : IHotelPassService
{
    private readonly IRepository<HotelPassModel> _hotelRepo;
    private readonly IParkingLotService _lotService;

    public HotelPassService(IRepository<HotelPassModel> hotelRepo, IParkingLotService lotService)
    {
        _hotelRepo = hotelRepo;
        _lotService = lotService;
    }
    
    public async Task<ServiceResult<ReadHotelPassDto>> GetHotelPassByIdAsync(long id)
    {
        try
        {
            var pass = await _hotelRepo.FindByIdAsync(id);
            if (pass is null) return ServiceResult<ReadHotelPassDto>.NotFound("No hotel pass with that id found");
            return ServiceResult<ReadHotelPassDto>.Ok(new ReadHotelPassDto
            {
                Id = id,
                LicensePlate = pass.LicensePlateNumber,
                ParkingLotId = pass.ParkingLotId,
                Start =  pass.Start,
                End = pass.End,
                ExtraTime = pass.ExtraTime
            });
        }catch (Exception ex)
        {
            return ServiceResult<ReadHotelPassDto>.Exception("Unexpected error occurred.");
        }
        
    }

    public async Task<ServiceResult<List<ReadHotelPassDto>>> GetHotelPassesByParkingLotIdAsync(long parkingLotId)
    {
        try
        {
            var passes = await _hotelRepo.GetByAsync(x => x.ParkingLotId == parkingLotId);
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
        }catch (Exception ex)
        {
            return ServiceResult<List<ReadHotelPassDto>>.Exception("Unexpected error occurred.");
        }
    }

    public async Task<ServiceResult<List<ReadHotelPassDto>>> GetHotelPassesByLicensePlateAsync(string licensePlate)
    {
        try
        {
            
        }catch (Exception ex)
        {
            return ServiceResult<List<ReadHotelPassDto>>.Exception("Unexpected error occurred.");
        }
        var passes = await _hotelRepo.GetByAsync(x => x.LicensePlateNumber == licensePlate);
        if(!passes.Any()) return ServiceResult<List<ReadHotelPassDto>>.NotFound($"License plate {licensePlate} has no hotel passes.");
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

    public async Task<ServiceResult<ReadHotelPassDto>> GetActiveHotelPassByLicensePlateAndLotIdAsync(long parkingLotId, string licensePlate)
    {
        try
        {
            var now = DateTime.UtcNow;
            var pass = (await _hotelRepo
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
        } catch (Exception ex)
        {
            return ServiceResult<ReadHotelPassDto>.Exception("Unexpected error occurred.");
        }

    }

    
    public async Task<ServiceResult<ReadHotelPassDto>> CreateHotelPassAsync(CreateHotelPassDto pass)
    {
        try
        {
            string plate = pass.LicensePlate.Trim().ToUpperInvariant();

        DateTime reservationStart = pass.Start.ToUniversalTime();
        DateTime reservationEnd = pass.End.ToUniversalTime();

        if (reservationEnd <= reservationStart)
            return ServiceResult<ReadHotelPassDto>.BadRequest("End must be after Start.");
        
        DateTime reservationEndWithExtra = reservationEnd + pass.ExtraTime;
        
        var overlappingExistingPass = (await _hotelRepo.GetByAsync(x =>
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

        _hotelRepo.Add(newPass);
        await _hotelRepo.SaveChangesAsync();

        return ServiceResult<ReadHotelPassDto>.Ok(new ReadHotelPassDto
        {
            Id = newPass.Id,
            LicensePlate = newPass.LicensePlateNumber,
            ParkingLotId = newPass.ParkingLotId,
            Start = newPass.Start,
            End = newPass.End,
            ExtraTime = newPass.ExtraTime
        });
        } catch (Exception ex)
        {
            return ServiceResult<ReadHotelPassDto>.Exception("Unexpected error occurred.");
        }
    }

    public async Task<ServiceResult<ReadHotelPassDto>> PatchHotelPassAsync(PatchHotelPassDto pass)
    {
        try
        {
            var existingPass = await _hotelRepo.FindByIdAsync(pass.Id);
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
        
        var overlapping = (await _hotelRepo.GetByAsync(x =>
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
        
        _hotelRepo.Update(existingPass);
        await _hotelRepo.SaveChangesAsync();
        
        return ServiceResult<ReadHotelPassDto>.Ok(new ReadHotelPassDto
        {
            Id = existingPass.Id,
            LicensePlate = existingPass.LicensePlateNumber,
            ParkingLotId = existingPass.ParkingLotId,
            Start = existingPass.Start,
            End = existingPass.End,
            ExtraTime = existingPass.ExtraTime
        });
        } catch (Exception ex)
        {
            return ServiceResult<ReadHotelPassDto>.Exception("Unexpected error occurred.");
        }
    }

    public async Task<ServiceResult<bool>> DeleteHotelPassByIdAsync(long id)
    {
        try
        {
            var pass = await _hotelRepo.FindByIdAsync(id);
            if (pass is null) return ServiceResult<bool>.NotFound($"No hotel pass with id {id} found.");

            _hotelRepo.Deletee(pass);
            await _hotelRepo.SaveChangesAsync();
            return ServiceResult<bool>.Ok(true);
        } catch (Exception ex)
        {
            return ServiceResult<bool>.Exception("Unexpected error occurred.");
        }
    }
}