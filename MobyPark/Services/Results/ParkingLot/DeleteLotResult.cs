namespace MobyPark.Services.Results.ParkingLot;

public abstract record DeleteLotResult
{
    public sealed record Success() : DeleteLotResult;
    public sealed record NotFound() : DeleteLotResult;
    public sealed record Error(string Message) : DeleteLotResult;
}
