namespace MobyPark.Services.Results.Reservation;

public abstract record DeleteReservationResult
{
    public sealed record Success() : DeleteReservationResult;
    public sealed record NotFound() : DeleteReservationResult;
    public sealed record Forbidden() : DeleteReservationResult;
    public sealed record Error(string Message) : DeleteReservationResult;
}
