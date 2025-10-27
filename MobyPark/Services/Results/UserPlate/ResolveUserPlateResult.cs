namespace MobyPark.Services.Results.UserPlate;

public abstract record ResolveUserPlateResult
{
    public sealed record Success(long UserId, string LicensePlate) : ResolveUserPlateResult;
    public sealed record PlateNotFound : ResolveUserPlateResult;
    public sealed record UserNotFound(string Username) : ResolveUserPlateResult;
    public sealed record PlateNotOwned(string Message) : ResolveUserPlateResult;
    public sealed record Forbidden(string Message) : ResolveUserPlateResult;
    public sealed record Error(string Message) : ResolveUserPlateResult;
}