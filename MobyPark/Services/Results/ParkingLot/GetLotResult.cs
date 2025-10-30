using MobyPark.Models;

namespace MobyPark.Services.Results.ParkingLot;

public abstract record GetLotResult
{
    public sealed record Success(ParkingLotModel Lot) : GetLotResult;
    public sealed record NotFound() : GetLotResult;
    public sealed record InvalidInput(string Message) : GetLotResult;
}
