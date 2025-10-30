using MobyPark.Models;

namespace MobyPark.Services.Results.ParkingSession;

public abstract record CreateSessionResult
{
    public sealed record Success(ParkingSessionModel Session) : CreateSessionResult;
    public sealed record ValidationError(string Message) : CreateSessionResult;
    public sealed record AlreadyExists : CreateSessionResult;
    public sealed record Error(string Message) : CreateSessionResult;
}