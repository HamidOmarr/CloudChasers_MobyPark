namespace MobyPark.Services.Results.Tokens;

public abstract record CreateJwtResult
{
    public sealed record Success(string JwtToken) : CreateJwtResult;
    public sealed record ConfigError(string Message) : CreateJwtResult;
}