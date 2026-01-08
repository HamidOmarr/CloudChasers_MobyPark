using MobyPark.DTOs.Business;
using MobyPark.Services.Results;

namespace MobyPark.Services.Interfaces;

public interface IBusinessParkingRegistrationService
{
    Task<ServiceResult<ReadBusinessRegDto>> CreateBusinessRegistrationAdminAsync(CreateBusinessRegAdminDto bReg);

    Task<ServiceResult<ReadBusinessRegDto>> CreateBusinessRegistrationAsync(CreateBusinessRegDto bReg,
        long currentUserId);

    Task<ServiceResult<ReadBusinessRegDto>> SetBusinessRegistrationActiveAdminAsync(
        PatchBusinessRegDto bReg);

    Task<ServiceResult<ReadBusinessRegDto>> SetBusinessRegistrationActiveAsync(
        PatchBusinessRegDto bReg, long currentUserId);

    Task<ServiceResult<bool>> AdminDeleteBusinessRegistrationAsync(
        long id);

    Task<ServiceResult<ReadBusinessRegDto>> GetBusinessRegistrationByIdAsync(long id);

    Task<ServiceResult<List<ReadBusinessRegDto>>> GetBusinessRegistrationsByBusinessAsync(long id);

    Task<ServiceResult<List<ReadBusinessRegDto>>> GetBusinessRegistrationByLicensePlateAsync(string licensePlate);

    Task<ServiceResult<ReadBusinessRegDto>> GetActiveBusinessRegistrationByLicencePlateAsync(string licensePlate);
}