using MobyPark.Models;

namespace MobyPark.Services.Results.Reservation;

public abstract record GetReservationListResult
{
    public sealed record Success(List<ReservationModel> Reservations) : GetReservationListResult;
    public sealed record NotFound : GetReservationListResult;
    public sealed record InvalidInput(string Message) : GetReservationListResult;
}
