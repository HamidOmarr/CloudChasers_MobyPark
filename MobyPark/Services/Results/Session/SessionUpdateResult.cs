using MobyPark.Models;

namespace MobyPark.Services.Results.Session;

public abstract record SessionUpdateResult
{
    public sealed record Success(ParkingSessionModel Session) : SessionUpdateResult;
    public sealed record NotFound() : SessionUpdateResult;
    public sealed record Error(string Message) : SessionUpdateResult;
}
