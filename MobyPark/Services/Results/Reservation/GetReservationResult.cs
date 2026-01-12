using MobyPark.Models;

namespace MobyPark.Services.Results.Reservation;

public abstract record GetReservationResult
{
    public sealed record Success(ReservationModel Reservation) : GetReservationResult;
    public sealed record NotFound : GetReservationResult;
}