using MobyPark.Models;

namespace MobyPark.Services.Results.UserPlate;

public abstract record CreateUserPlateResult
{
    public sealed record Success(UserPlateModel Plate) : CreateUserPlateResult;
    public sealed record AlreadyExists : CreateUserPlateResult;
    public sealed record Error(string Message) : CreateUserPlateResult;
}
