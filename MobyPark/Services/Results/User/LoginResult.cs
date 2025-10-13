using MobyPark.Models.Responses.User;

namespace MobyPark.Services.Results.User;

public abstract record LoginResult
{
    public sealed record Success(AuthResponse Response) : LoginResult;
    public sealed record InvalidCredentials() : LoginResult;
    public sealed record Error(string Message) : LoginResult;
}
