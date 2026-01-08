using MobyPark.DTOs.Hotel;
using MobyPark.Services.Results;

namespace MobyPark.Services;

public interface IHotelService
{
    Task<ServiceResult<ReadHotelDto>> CreateHotelAsync(CreateHotelDto hotel);
    Task<ServiceResult<PatchHotelDto>> PatchHotelAsync(PatchHotelDto hotelToUpdate);
    Task<ServiceResult<bool>> DeleteHotelAsync(long id);
    Task<ServiceResult<List<ReadHotelDto>>> GetAllHotelsAsync();
    Task<ServiceResult<ReadHotelDto>> GetHotelByIdAsync(long id);
}