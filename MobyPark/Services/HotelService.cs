using Microsoft.AspNetCore.Authorization;
using MobyPark.DTOs.Hotel;
using MobyPark.Models;
using MobyPark.Models.Repositories.Interfaces;
using MobyPark.Services.Results;

namespace MobyPark.Services;

public class HotelService
{
    private readonly IRepository<HotelModel> _hotels;
    private readonly IRepository<ParkingLotModel> _lots;

    public HotelService(IRepository<HotelModel> hotels, IRepository<ParkingLotModel> lots)
    {
        _hotels = hotels;
        _lots = lots;
    }

    [Authorize("CanManageHotels")]
    public async Task<ServiceResult<ReadHotelDto>> CreateHotelAsync(CreateHotelDto hotel)
    {
        var parkingLotExists = await _lots.FindByIdAsync(hotel.HotelParkingLotId);
        if (parkingLotExists is null) return ServiceResult<ReadHotelDto>.NotFound("There was no parking lot found with that id");
        
        var addressTaken = await _hotels.GetByAsync(x => x.Address == hotel.Address);
        if (addressTaken.Any()) return ServiceResult<ReadHotelDto>.Conflict("Address already taken"); // PO moet nog bevestigen of dit moet
        var parkingLotTaken = await _hotels.GetByAsync(x => x.HotelParkingLotId == hotel.HotelParkingLotId);
        if (parkingLotTaken.Any()) return ServiceResult<ReadHotelDto>.Conflict("Parking lot already taken"); // PO moet nog bevestigen of dit moet

        var newHotel = new HotelModel
        {
            Name = hotel.Name,
            Address = hotel.Address,
            IBAN = hotel.IBAN,
            HotelParkingLotId = hotel.HotelParkingLotId
        };

        _hotels.Add(newHotel);
        await _hotels.SaveChangesAsync();

        return ServiceResult<ReadHotelDto>.Ok(new ReadHotelDto
        {
            Id = newHotel.Id,
            Name = newHotel.Name,
            Address = newHotel.Address,
            HotelParkingLotId = newHotel.HotelParkingLotId
            // IBAN voor nu weggelaten vanwege security redenen
        });
    }

    [Authorize("CanManageHotels")]
    public async Task<ServiceResult<PatchHotelDto>> PatchHotelAsync(PatchHotelDto hotelToUpdate) //Ik heb hier een patchdto returned zodat de IBAN zichtbaar is na het updaten. die return ik niet in de readDto
    {
        var hotel = await _hotels.FindByIdAsync(hotelToUpdate.Id);
        if (hotel is null) return ServiceResult<PatchHotelDto>.NotFound("No hotel found with that id");

        if (hotelToUpdate.HotelParkingLotId.HasValue)
        {
            var lotTaken = await _hotels.GetByAsync(x => x.HotelParkingLotId == hotelToUpdate.HotelParkingLotId);
            if (lotTaken.Any())
                return ServiceResult<PatchHotelDto>.Conflict("Parking lot is already taken by another hotel");
        }
        if (!string.IsNullOrWhiteSpace(hotelToUpdate.Name)) hotel.Name = hotelToUpdate.Name;
        
        if (!string.IsNullOrWhiteSpace(hotelToUpdate.Address)) hotel.Address = hotelToUpdate.Address; //Wachten op reactie van PO of address uniek moet zijn, zoja -> eerst controlleren
        if (!string.IsNullOrWhiteSpace(hotelToUpdate.IBAN)) hotel.IBAN = hotelToUpdate.IBAN;

        _hotels.Update(hotel);
        await _hotels.SaveChangesAsync();

        return ServiceResult<PatchHotelDto>.Ok(new PatchHotelDto
        {
            Id = hotel.Id,
            Name = hotel.Name,
            Address = hotel.Address,
            IBAN = hotel.IBAN,
            HotelParkingLotId = hotel.HotelParkingLotId
        });
    }

    [Authorize("CanManageHotels")]
    public async Task<ServiceResult<bool>> DeleteHotelAsync(long id)
    {
        var hotel = await _hotels.FindByIdAsync(id);
        if (hotel is null) return ServiceResult<bool>.NotFound("no hotel found with that id");
        _hotels.Deletee(hotel);
        await _hotels.SaveChangesAsync();

        return ServiceResult<bool>.Ok(true);
    }

    public async Task<ServiceResult<List<ReadHotelDto>>> GetAllHotelsAsync()
    {
        var hotels = await _hotels.ReadAllAsync();
        
        return ServiceResult<List<ReadHotelDto>>.Ok(hotels.Select(x => new ReadHotelDto
        {
            Id = x.Id,
            Name = x.Name,
            Address = x.Address,
            HotelParkingLotId = x.HotelParkingLotId
        }).ToList());
    }

    public async Task<ServiceResult<ReadHotelDto>> GetHotelByIdAsync(long id)
    {
        var hotel = await _hotels.FindByIdAsync(id);
        if (hotel is null) return ServiceResult<ReadHotelDto>.NotFound("No hotel found with that id");
        return ServiceResult<ReadHotelDto>.Ok(new ReadHotelDto
        {
            Id = hotel.Id,
            Name = hotel.Name,
            Address = hotel.Address,
            HotelParkingLotId = hotel.HotelParkingLotId
        });
    }
}