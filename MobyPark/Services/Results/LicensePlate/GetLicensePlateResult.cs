using MobyPark.Models;

namespace MobyPark.Services.Results.LicensePlate;

public abstract record GetLicensePlateResult
{
    public sealed record Success(LicensePlateModel Plate) : GetLicensePlateResult;
    public sealed record NotFound(string Message) : GetLicensePlateResult;
}