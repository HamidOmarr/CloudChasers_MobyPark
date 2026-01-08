using MobyPark.Models;

namespace MobyPark.Services.Results.ParkingSession;

public abstract record GetSessionListResult
{
    public sealed record Success(List<ParkingSessionModel> Sessions) : GetSessionListResult;
    public sealed record NotFound : GetSessionListResult;
    public sealed record InvalidInput(string Message) : GetSessionListResult;
}