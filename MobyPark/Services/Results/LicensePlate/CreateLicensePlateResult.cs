using MobyPark.Models;

namespace MobyPark.Services.Results.LicensePlate;

public abstract record CreateLicensePlateResult
{
    public sealed record Success(LicensePlateModel plate) : CreateLicensePlateResult;
    public sealed record Error(string Message) : CreateLicensePlateResult;
}