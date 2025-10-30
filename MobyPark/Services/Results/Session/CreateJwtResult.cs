namespace MobyPark.Services.Results.Session;

public abstract record CreateJwtResult
{
    public sealed record Success(string JwtToken) : CreateJwtResult;
    public sealed record ConfigError(string Message) : CreateJwtResult;
}