using MobyPark.Models;

namespace MobyPark.Services.Results.ParkingSession;

public abstract record PersistSessionResult
{
    public sealed record Success(ParkingSessionModel Session) : PersistSessionResult;
    public sealed record Error(string Message) : PersistSessionResult;
}