using MobyPark.Services.Results;

namespace MobyPark.Services.Interfaces;

public interface IBusinessService
{
    Task<ServiceResult<ReadBusinessDto>> CreateBusinessAsync(CreateBusinessDto business);
    Task<ServiceResult<ReadBusinessDto>> PatchBusinessAsync(long id, PatchBusinessDto businessPatch);
    Task<ServiceResult<bool>> DeleteBusinessByIdAsync(long id);
    Task<ServiceResult<List<ReadBusinessDto>>> GetAllAsync();
    Task<ServiceResult<ReadBusinessDto>> GetBusinessByIdAsync(long id);
    Task<ServiceResult<ReadBusinessDto>> GetBusinessByAddressAsync(string address);


}