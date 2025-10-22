using MobyPark.Models;

namespace MobyPark.Services.Results.ParkingLot;

public abstract record UpdateLotResult
{
    public sealed record Success(ParkingLotModel Lot) : UpdateLotResult;
    public sealed record NotFound : UpdateLotResult;
    public sealed record NoChangesMade : UpdateLotResult;
    public sealed record InvalidInput(string Message) : UpdateLotResult;
    public sealed record Error(string Message) : UpdateLotResult;
}
