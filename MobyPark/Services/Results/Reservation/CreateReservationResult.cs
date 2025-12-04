using MobyPark.Models;

namespace MobyPark.Services.Results.Reservation;

public abstract record CreateReservationResult
{
    public sealed record Success(ReservationModel Reservation, decimal EstimatedAmount) : CreateReservationResult;
    public sealed record LotNotFound() : CreateReservationResult;
    public sealed record PlateNotFound() : CreateReservationResult;
    public sealed record UserNotFound(string Username) : CreateReservationResult;
    public sealed record PlateNotOwned(string Message) : CreateReservationResult;
    public sealed record AlreadyExists(string Message) : CreateReservationResult;
    public sealed record LotFull() : CreateReservationResult;
    public sealed record Forbidden(string? Message = null) : CreateReservationResult;
    public sealed record InvalidInput(string Message) : CreateReservationResult;
    public sealed record Error(string Message) : CreateReservationResult;
}
