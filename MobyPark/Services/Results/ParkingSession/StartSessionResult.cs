using MobyPark.Models;

namespace MobyPark.Services.Results.ParkingSession;

public abstract record StartSessionResult
{
    public sealed record Success(ParkingSessionModel Session, int AvailableSpots) : StartSessionResult;
    public sealed record LotNotFound : StartSessionResult;
    public sealed record LotFull : StartSessionResult;
    public sealed record AlreadyActive : StartSessionResult;
    public sealed record PreAuthFailed(string Reason) : StartSessionResult;
    public sealed record PaymentRequired(string Reason) : StartSessionResult;
    public sealed record Error(string Message) : StartSessionResult;
}