using MobyPark.Models;

namespace MobyPark.Services.Results.Reservation;

public abstract record UpdateReservationResult
{
    public sealed record Success(ReservationModel Reservation) : UpdateReservationResult;
    public sealed record NoChangesMade : UpdateReservationResult;
    public sealed record NotFound : UpdateReservationResult;
    public sealed record Error(string Message) : UpdateReservationResult;
}