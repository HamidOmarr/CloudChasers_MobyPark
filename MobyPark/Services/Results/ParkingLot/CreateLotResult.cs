using MobyPark.Models;

namespace MobyPark.Services.Results.ParkingLot;

public abstract record CreateLotResult
{
    public sealed record Success(ParkingLotModel Lot) : CreateLotResult;
    public sealed record Error(string Message) : CreateLotResult;
}
