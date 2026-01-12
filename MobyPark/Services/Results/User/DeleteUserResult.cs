namespace MobyPark.Services.Results.User;

public abstract record DeleteUserResult
{
    public sealed record Success : DeleteUserResult;
    public sealed record NotFound : DeleteUserResult;
    public sealed record Error(string Message) : DeleteUserResult;
}