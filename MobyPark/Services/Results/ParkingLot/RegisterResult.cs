using MobyPark.Models;

namespace MobyPark.Services.Results.ParkingLot;

public abstract record RegisterResult
{
    public sealed record Success(ParkingLotModel ParkingLot) : RegisterResult;

    public sealed record AddressTaken() : RegisterResult;
    public sealed record InvalidData(string Message) : RegisterResult;
    public sealed record Error(string Message) : RegisterResult;
}