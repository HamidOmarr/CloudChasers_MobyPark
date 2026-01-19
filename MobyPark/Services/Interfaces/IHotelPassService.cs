using MobyPark.DTOs.Hotel;
using MobyPark.Services.Results;

namespace MobyPark.Services.Interfaces;

public interface IHotelPassService
{
    Task<ServiceResult<ReadHotelPassDto>> GetHotelPassByIdAsync(long id);
    Task<ServiceResult<List<ReadHotelPassDto>>> GetHotelPassesByParkingLotIdAsync(long parkingLotId);
    Task<ServiceResult<List<ReadHotelPassDto>>> GetHotelPassesByLicensePlateAsync(string licensePlate);
    Task<ServiceResult<ReadHotelPassDto>> GetActiveHotelPassByLicensePlateAndLotIdAsync(long parkingLotId, string licensePlate);
    Task<ServiceResult<ReadHotelPassDto>> CreateHotelPassAsync(CreateHotelPassDto pass, long currentUserId);

    Task<ServiceResult<List<ReadHotelPassDto>>> GetHotelPassesByLicensePlateAndLotIdAsync(long parkingLotId,
        string licensePlate);
    Task<ServiceResult<ReadHotelPassDto>> CreateHotelPassAsync(AdminCreateHotelPassDto pass);
    Task<ServiceResult<ReadHotelPassDto>> PatchHotelPassAsync(PatchHotelPassDto pass);
    Task<ServiceResult<ReadHotelPassDto>> PatchHotelPassAsync(PatchHotelPassDto pass, long currentUserId);
    Task<ServiceResult<bool>> DeleteHotelPassByIdAsync(long id);
    Task<ServiceResult<bool>> DeleteHotelPassByIdAsync(long id, long currentUserId);
}