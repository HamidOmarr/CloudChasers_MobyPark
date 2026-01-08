using MobyPark.Models;

namespace MobyPark.Services.Results.Reservation;

public abstract record ApplyUpdateResult
{
    public sealed record Success(ReservationModel UpdatedReservation, bool DatesChanged, bool ModelChanged) : ApplyUpdateResult;
    public sealed record CannotChangeStartedReservation : ApplyUpdateResult;
    public sealed record EndTimeBeforeStartTime : ApplyUpdateResult;
    public sealed record CannotChangeCompletedStatus : ApplyUpdateResult;
    public sealed record Error(string Message) : ApplyUpdateResult;
}