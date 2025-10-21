using MobyPark.Models;

namespace MobyPark.Services.Results.UserPlate;

public abstract record GetUserPlateResult
{
    public sealed record Success(UserPlateModel Plate) : GetUserPlateResult;
    public sealed record NotFound() : GetUserPlateResult;
    public sealed record Error(string Message) : GetUserPlateResult;
}
