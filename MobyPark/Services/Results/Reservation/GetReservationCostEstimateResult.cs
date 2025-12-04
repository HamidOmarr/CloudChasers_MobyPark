using MobyPark.Models;

namespace MobyPark.Services.Results.Reservation;

public abstract record GetReservationCostEstimateResult
{
    public sealed record Success(decimal EstimatedCost) : GetReservationCostEstimateResult;
    public sealed record LotNotFound : GetReservationCostEstimateResult;
    public sealed record InvalidTimeWindow(string Reason) : GetReservationCostEstimateResult;
    public sealed record LotClosed : GetReservationCostEstimateResult;
    public sealed record Error(string Message) : GetReservationCostEstimateResult;
}
