namespace MobyPark.Services.Results.UserPlate;

public abstract record DeleteUserPlateResult
{
    public sealed record Success() : DeleteUserPlateResult;
    public sealed record NotFound() : DeleteUserPlateResult;
    public sealed record InvalidOperation(string Message) : DeleteUserPlateResult;
    public sealed record Error(string Message) : DeleteUserPlateResult;
}
