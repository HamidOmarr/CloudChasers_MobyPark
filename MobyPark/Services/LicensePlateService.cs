using MobyPark.Models;
using MobyPark.Models.Repositories.Interfaces;
using MobyPark.Services.Interfaces;
using MobyPark.Services.Results.LicensePlate;

namespace MobyPark.Services;

public class LicensePlateService : ILicensePlateService
{
    private readonly ILicensePlateRepository _licensePlates;

    public LicensePlateService(ILicensePlateRepository licensePlates)
    {
        _licensePlates = licensePlates;
    }

    public async Task<CreateLicensePlateResult> CreateLicensePlate(LicensePlateModel licensePlate)
    {
        var existingPlate = await _licensePlates.GetByNumber(licensePlate.LicensePlateNumber);
        if (existingPlate is not null)
            return new CreateLicensePlateResult.Error("License plate already exists");

        bool createdSuccessfully = await _licensePlates.Create(licensePlate);
        if (!createdSuccessfully)
            return new CreateLicensePlateResult.Error("Failed to create license plate");
        return new CreateLicensePlateResult.Success(licensePlate);
    }

    public async Task<GetLicensePlateResult> GetByLicensePlate(string licensePlate)
    {
        var plate = await _licensePlates.GetByNumber(licensePlate);
        if (plate is not null)
            return new GetLicensePlateResult.Success(plate);
        return new GetLicensePlateResult.NotFound("License plate not found");
    }
}
