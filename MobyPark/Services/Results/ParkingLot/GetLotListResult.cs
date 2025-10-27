using MobyPark.Models;

namespace MobyPark.Services.Results.ParkingLot;

public abstract record GetLotListResult
{
    public sealed record Success(List<ParkingLotModel> Lots) : GetLotListResult;
    public sealed record NotFound() : GetLotListResult;
    public sealed record InvalidInput(string Message) : GetLotListResult;
}
