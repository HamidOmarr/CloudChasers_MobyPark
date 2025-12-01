using MobyPark.DTOs.Hotel;
using MobyPark.Services.Results;

namespace MobyPark.Services.Interfaces;

public interface IHotelPassService
{
    Task<ServiceResult<ReadHotelPassDto>> GetHotelPassByIdAsync(long id);
    Task<ServiceResult<List<ReadHotelPassDto>>> GetHotelPassesByParkingLotIdAsync(long parkingLotId);
    Task<ServiceResult<List<ReadHotelPassDto>>> GetHotelPassesByLicensePlateAsync(string licensePlate);
    Task<ServiceResult<ReadHotelPassDto>> GetActiveHotelPassByLicensePlateAndLotIdAsync(long parkingLotId, string licensePlate);
    Task<ServiceResult<ReadHotelPassDto>> CreateHotelPassAsync(CreateHotelPassDto pass);
    Task<ServiceResult<ReadHotelPassDto>> PatchHotelPassAsync(PatchHotelPassDto pass);
    Task<ServiceResult<bool>> DeleteHotelPassByIdAsync(long id);
}