using MobyPark.Models;

namespace MobyPark.Services.Results.User;

public abstract record RegisterResult
{
    public sealed record Success(UserModel User) : RegisterResult;
    public sealed record UsernameTaken : RegisterResult;
    public sealed record InvalidData(string Message) : RegisterResult;
    public sealed record Error(string Message) : RegisterResult;
}