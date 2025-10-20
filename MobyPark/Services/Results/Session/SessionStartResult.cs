using MobyPark.Models;

namespace MobyPark.Services.Results.Session;

public abstract record SessionStartResult
{
    public sealed record Success(ParkingSessionModel Session) : SessionStartResult;
    public sealed record LotNotFound() : SessionStartResult;
    public sealed record LotFull() : SessionStartResult;
    public sealed record AlreadyActive() : SessionStartResult;
    public sealed record PreAuthFailed(string Reason) : SessionStartResult;
    public sealed record Error(string Message) : SessionStartResult;
}
