namespace MobyPark.Services.Results.UserPlate;

public abstract record UserPlateExistsResult
{
    public sealed record Exists() : UserPlateExistsResult;
    public sealed record NotExists() : UserPlateExistsResult;
    public sealed record InvalidInput(string Message) : UserPlateExistsResult;
}
