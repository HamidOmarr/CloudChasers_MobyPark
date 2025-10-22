using MobyPark.Models;

namespace MobyPark.Services.Results.ParkingSession;

public abstract record UpdateSessionResult
{
    public sealed record Success(ParkingSessionModel Session) : UpdateSessionResult;
    public sealed record NoChanges : UpdateSessionResult;
    public sealed record NotFound : UpdateSessionResult;
    public sealed record Forbidden : UpdateSessionResult;
    public sealed record Error(string Message) : UpdateSessionResult;
}
