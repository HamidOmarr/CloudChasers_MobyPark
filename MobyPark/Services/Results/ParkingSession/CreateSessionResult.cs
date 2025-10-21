namespace MobyPark.Services.Results.ParkingSession;

public abstract record CreateSessionResult
{
    public sealed record Success(long SessionId) : CreateSessionResult;
    public sealed record ValidationError(string Message) : CreateSessionResult;
    public sealed record Error(string Message) : CreateSessionResult;
}