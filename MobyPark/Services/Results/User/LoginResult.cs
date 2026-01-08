using MobyPark.DTOs.User.Response;

namespace MobyPark.Services.Results.User;

public abstract record LoginResult
{
    public sealed record Success(AuthDto Response) : LoginResult;
    public sealed record InvalidCredentials : LoginResult;
    public sealed record Error(string Message) : LoginResult;
}