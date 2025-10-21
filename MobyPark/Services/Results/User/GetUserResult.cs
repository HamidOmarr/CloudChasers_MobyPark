using MobyPark.Models;

namespace MobyPark.Services.Results.User;

public abstract record GetUserResult
{
    public sealed record Success(UserModel User) : GetUserResult;
    public sealed record NotFound() : GetUserResult;
}
