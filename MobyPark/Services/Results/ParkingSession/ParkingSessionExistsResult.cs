namespace MobyPark.Services.Results.ParkingSession;

public abstract record ParkingSessionExistsResult
{
    public sealed record Exists : ParkingSessionExistsResult;
    public sealed record NotExists : ParkingSessionExistsResult;
    public sealed record InvalidInput(string Message) : ParkingSessionExistsResult;
    public sealed record Error(string Message) : ParkingSessionExistsResult;
}