namespace MobyPark.Services.Results.ParkingLot;

public abstract record ParkingLotExistsResult
{
    public sealed record Exists : ParkingLotExistsResult;
    public sealed record NotExists : ParkingLotExistsResult;
    public sealed record InvalidInput(string Message) : ParkingLotExistsResult;
    public sealed record Error(string Message) : ParkingLotExistsResult;
}
