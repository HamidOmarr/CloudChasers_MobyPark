using MobyPark.DTOs.Hotel;
using MobyPark.Models;
using MobyPark.Models.Repositories.Interfaces;
using MobyPark.Services.Interfaces;
using MobyPark.Services.Results;

namespace MobyPark.Services;

public class HotelPassService : IHotelPassService
{
    private readonly IRepository<HotelPassModel> _hotelRepo;

    public HotelPassService(IRepository<HotelPassModel> hotelRepo)
    {
        _hotelRepo = hotelRepo;
    }
    
    public async Task<ServiceResult<ReadHotelPassDto>> GetHotelPassByIdAsync(long id)
    {
        var pass = await _hotelRepo.FindByIdAsync(id);
        if (pass is null) return ServiceResult<ReadHotelPassDto>.NotFound("No hotel pass with that id found");
        return ServiceResult<ReadHotelPassDto>.Ok(new ReadHotelPassDto
        {
            Id = id,
            LicensePlate = pass.LicensePlate,
            ParkingLotId = pass.ParkingLotId,
            Start =  pass.Start,
            End = pass.End,
            ExtraTime = pass.ExtraTime
        });
    }

    public async Task<ServiceResult<List<ReadHotelPassDto>>> GetHotelPassesByParkingLotIdAsync(long parkingLotId)
    {
        var passes = await _hotelRepo.GetByAsync(x => x.ParkingLotId == parkingLotId);
        if (!passes.Any()) return ServiceResult<List<ReadHotelPassDto>>.NotFound($"Parking lot with id {parkingLotId} has no hotel passes.");
        var dtoList = passes.Select(x => new ReadHotelPassDto
        {
            Id = x.Id,
            LicensePlate = x.LicensePlate,
            ParkingLotId = x.ParkingLotId,
            Start = x.Start,
            End = x.End,
            ExtraTime = x.ExtraTime
        }).ToList();

        return ServiceResult<List<ReadHotelPassDto>>.Ok(dtoList);
    }

    public async Task<ServiceResult<List<ReadHotelPassDto>>> GetHotelPassesByLicensePlateAsync(string licensePlate)
    {
        var passes = await _hotelRepo.GetByAsync(x => x.LicensePlate == licensePlate);
        if(!passes.Any()) return ServiceResult<List<ReadHotelPassDto>>.NotFound($"License plate {licensePlate} has no hotel passes.");
        var dtoList = passes.Select(x => new ReadHotelPassDto
        {
            Id = x.Id,
            LicensePlate = x.LicensePlate,
            ParkingLotId = x.ParkingLotId,
            Start = x.Start,
            End = x.End,
            ExtraTime = x.ExtraTime
        }).ToList();
        
        return ServiceResult<List<ReadHotelPassDto>>.Ok(dtoList);
    }

    public async Task<ServiceResult<ReadHotelPassDto>> GetActiveHotelPassByLicensePlateAndLotIdAsync(long parkingLotId, string licensePlate)
    {
        var now = DateTime.UtcNow;
        var pass = (await _hotelRepo
                .GetByAsync(x => x.ParkingLotId == parkingLotId && x.LicensePlate == licensePlate && x.Start >= now && x.End + x.ExtraTime <= now))
            .FirstOrDefault(); //Ik ga ervanuit dat 1 kenteken maar 1 actieve hotel pass kan hebben bij een bepaald hotel/parkinglot.
        if (pass is null)
            return ServiceResult<ReadHotelPassDto>.NotFound(
                $"No active hotel pass found for license plate {licensePlate} at parking lot with id {parkingLotId}");
        return ServiceResult<ReadHotelPassDto>.Ok(new ReadHotelPassDto
        {
            Id = pass.Id,
            LicensePlate = pass.LicensePlate,
            ParkingLotId = pass.ParkingLotId,
            Start = pass.Start,
            End = pass.End,
            ExtraTime = pass.ExtraTime
        });

    }

    public async Task<ServiceResult<ReadHotelPassDto>> CreateHotelPassAsync(CreateHotelPassDto pass)
    {
        var now = DateTime.UtcNow;
        //Check if there is already an active pass at the current hotel
        var alreadyActive = (await _hotelRepo.GetByAsync(x =>
            x.LicensePlate == pass.LicensePlate && x.ParkingLotId == pass.ParkingLotId && x.Start >= now &&
            x.End + x.ExtraTime <= now)).FirstOrDefault();
        if (alreadyActive is not null)
            return ServiceResult<ReadHotelPassDto>.Conflict(
                $"There is already an active hotel pass for license plate {pass.LicensePlate} at the current parkinglot");

        var hotelPass = new HotelPassModel
        {
            LicensePlate = pass.LicensePlate,
            ParkingLotId = pass.ParkingLotId,
            Start = pass.Start,
            End = pass.End,
            ExtraTime = pass.ExtraTime
        };

        _hotelRepo.Add(hotelPass);
        await _hotelRepo.SaveChangesAsync();
        return ServiceResult<ReadHotelPassDto>.Ok(new ReadHotelPassDto
        {
            Id = hotelPass.Id,
            LicensePlate = hotelPass.LicensePlate,
            ParkingLotId = hotelPass.ParkingLotId,
            Start = hotelPass.Start,
            End = hotelPass.End,
            ExtraTime = hotelPass.ExtraTime
        });
    }

    public async Task<ServiceResult<ReadHotelPassDto>> PatchHotelPassAsync(PatchHotelPassDto pass)
    {
        var existingPass = await _hotelRepo.FindByIdAsync(pass.Id);
        if (existingPass is null)
            return ServiceResult<ReadHotelPassDto>.NotFound($"Update failed. No pass with id {pass.Id} found");

        if (!string.IsNullOrWhiteSpace(pass.LicensePlate)) existingPass.LicensePlate = pass.LicensePlate;
        if (pass.Start.HasValue && pass.End.HasValue)
        {
            if (pass.Start >= pass.End)
                return ServiceResult<ReadHotelPassDto>.BadRequest("Start should be before end time");
            existingPass.Start = pass.Start.Value;
            existingPass.End = pass.End.Value;
        }
        if(pass.Start.HasValue && !pass.End.HasValue) existingPass.Start = pass.Start.Value;
        if(!pass.Start.HasValue && pass.End.HasValue) existingPass.End = pass.End.Value;
        if (pass.ExtraTime.HasValue) existingPass.ExtraTime = pass.ExtraTime.Value;

        _hotelRepo.Update(existingPass);
        await _hotelRepo.SaveChangesAsync();
        
        return ServiceResult<ReadHotelPassDto>.Ok(new ReadHotelPassDto
        {
            Id = existingPass.Id,
            LicensePlate = existingPass.LicensePlate,
            ParkingLotId = existingPass.ParkingLotId,
            Start = existingPass.Start,
            End = existingPass.End,
            ExtraTime = existingPass.ExtraTime
        });
    }

    public async Task<ServiceResult<bool>> DeleteHotelPassByIdAsync(long id)
    {
        var pass = await _hotelRepo.FindByIdAsync(id);
        if (pass is null) return ServiceResult<bool>.NotFound($"No hotel pass with id {id} found.");

        _hotelRepo.Deletee(pass);
        await _hotelRepo.SaveChangesAsync();
        return ServiceResult<bool>.Ok(true);
    }
}