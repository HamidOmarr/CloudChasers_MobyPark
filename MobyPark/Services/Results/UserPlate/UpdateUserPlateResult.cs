using MobyPark.Models;

namespace MobyPark.Services.Results.UserPlate;

public abstract record UpdateUserPlateResult
{
    public sealed record Success(UserPlateModel Plate) : UpdateUserPlateResult;
    public sealed record NotFound : UpdateUserPlateResult;
    public sealed record InvalidOperation(string Message) : UpdateUserPlateResult;
    public sealed record Error(string Message) : UpdateUserPlateResult;
}