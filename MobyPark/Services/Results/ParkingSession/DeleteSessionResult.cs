namespace MobyPark.Services.Results.ParkingSession;

public abstract record DeleteSessionResult
{
    public sealed record Success() : DeleteSessionResult;
    public sealed record NotFound() : DeleteSessionResult;
    public sealed record Error(string Message) : DeleteSessionResult;
}
