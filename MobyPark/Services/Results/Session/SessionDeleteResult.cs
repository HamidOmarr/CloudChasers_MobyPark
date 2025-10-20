namespace MobyPark.Services.Results.Session;

public abstract record SessionDeleteResult
{
    public sealed record Success() : SessionDeleteResult;
    public sealed record NotFound() : SessionDeleteResult;
    public sealed record Error(string Message) : SessionDeleteResult;
}
