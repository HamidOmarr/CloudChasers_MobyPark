using MobyPark.Models;

namespace MobyPark.Services.Results.ParkingSession;

public abstract record GetSessionResult
{
    public record Success(ParkingSessionModel Session) : GetSessionResult;
    public record NotFound : GetSessionResult;
    public record Forbidden : GetSessionResult;
}