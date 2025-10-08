using MobyPark.Models;

namespace MobyPark.Services.Results;

public abstract record LoginResult
{
    public sealed record Success(AuthResponse Response) : LoginResult;
    public sealed record InvalidCredentials() : LoginResult;
    public sealed record Error(string Message) : LoginResult;
}