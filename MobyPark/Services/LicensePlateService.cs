using MobyPark.Models;
using MobyPark.Models.Repositories.Interfaces;
using MobyPark.Models.Repositories.RepositoryStack;

namespace MobyPark.Services;

public class LicensePlateService
{
    private readonly ILicensePlateRepository _licensePlates;

    public LicensePlateService(IRepositoryStack repoStack)
    {
        _licensePlates = repoStack.LicensePlates;
    }

    public async Task<LicensePlateModel> CreateLicensePlate(LicensePlateModel licensePlate)
    {
        bool createdSuccessfully = await _licensePlates.Create(licensePlate);
        if (!createdSuccessfully) throw new Exception("Failed to create license plate");
        return licensePlate;
    }

    public async Task<LicensePlateModel?> GetByLicensePlate(string licensePlate)
    {
        LicensePlateModel? plate = await _licensePlates.GetByNumber(licensePlate);
        return plate ?? throw new KeyNotFoundException("License plate not found");
    }
}
