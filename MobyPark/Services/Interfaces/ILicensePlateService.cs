using MobyPark.DTOs.LicensePlate.Request;
using MobyPark.Models;
using MobyPark.Services.Results.LicensePlate;

namespace MobyPark.Services.Interfaces;

public interface ILicensePlateService
{
    Task<CreateLicensePlateResult> CreateLicensePlate(CreateLicensePlateDto dto);
    Task<GetLicensePlateResult> GetByLicensePlate(string licensePlate);
}