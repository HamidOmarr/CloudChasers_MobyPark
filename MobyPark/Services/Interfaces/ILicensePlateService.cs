using MobyPark.Models;
using MobyPark.Services.Results.LicensePlate;

namespace MobyPark.Services.Interfaces;

public interface ILicensePlateService
{
    Task<CreateLicensePlateResult> CreateLicensePlate(LicensePlateModel licensePlate);
    Task<GetLicensePlateResult> GetByLicensePlate(string licensePlate);
}
