using MobyPark.Models;
using MobyPark.Models.Repositories.Interfaces;

namespace MobyPark.Services;

public class LicensePlateService
{
    private readonly ILicensePlateRepository _licensePlates;

    public LicensePlateService(ILicensePlateRepository licensePlates)
    {
        _licensePlates = licensePlates;
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
