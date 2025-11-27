using MobyPark.Models;

namespace MobyPark.Services.Results.ParkingSession;

public abstract record StopSessionResult
{
    public sealed record Success(ParkingSessionModel Session, decimal totalAmount) : StopSessionResult;
    public sealed record LotNotFound : StopSessionResult;
    public sealed record LicensePlateNotFound : StopSessionResult;
    public sealed record AlreadyStopped : StopSessionResult;
    public sealed record PaymentFailed(string Reason) : StopSessionResult;
    public sealed record ValidationFailed(string Reason) : StopSessionResult;
    public sealed record Error(string Message) : StopSessionResult;
}
