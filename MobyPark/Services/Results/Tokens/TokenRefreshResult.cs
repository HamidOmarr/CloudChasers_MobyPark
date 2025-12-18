namespace MobyPark.Services.Results.Tokens;

public abstract record TokenRefreshResult
{
    public sealed record Success(string JwtToken, string RefreshToken) : TokenRefreshResult;
    public sealed record InvalidToken(string Message) : TokenRefreshResult;
    public sealed record Error(string Message) : TokenRefreshResult;
}
