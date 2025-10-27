using MobyPark.DTOs.LicensePlate.Request;
using MobyPark.Models;
using MobyPark.Models.Repositories.Interfaces;
using MobyPark.Services.Interfaces;
using MobyPark.Services.Results.LicensePlate;
using MobyPark.Validation;

namespace MobyPark.Services;

public class LicensePlateService : ILicensePlateService
{
    private readonly ILicensePlateRepository _licensePlates;

    public LicensePlateService(ILicensePlateRepository licensePlates)
    {
        _licensePlates = licensePlates;
    }

    public async Task<CreateLicensePlateResult> CreateLicensePlate(CreateLicensePlateDto dto)
    {
        string normalizedPlateNumber = dto.LicensePlate.Upper();

        var existingResult = await GetByLicensePlate(normalizedPlateNumber);
        if (existingResult is GetLicensePlateResult.Success)
            return new CreateLicensePlateResult.AlreadyExists();

        var licensePlate = new LicensePlateModel
        {
            LicensePlateNumber = normalizedPlateNumber,
        };

        try
        {
            if (!await _licensePlates.Create(licensePlate))
                return new CreateLicensePlateResult.Error("Database insertion failed.");

            return new CreateLicensePlateResult.Success(licensePlate);
        }
        catch (Exception ex)
        { return new CreateLicensePlateResult.Error(ex.Message); }
    }

    public async Task<GetLicensePlateResult> GetByLicensePlate(string licensePlate)
    {
        string normalizedPlate = licensePlate.Upper();

        var plate = await _licensePlates.GetByNumber(normalizedPlate);
        if (plate is null)
            return new GetLicensePlateResult.NotFound("License plate not found.");
        return new GetLicensePlateResult.Success(plate);
    }
}
