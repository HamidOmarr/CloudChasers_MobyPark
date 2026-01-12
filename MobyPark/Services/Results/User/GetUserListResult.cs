using MobyPark.Models;

namespace MobyPark.Services.Results.User;

public abstract record GetUserListResult
{
    public sealed record Success(List<UserModel> Users) : GetUserListResult;
    public sealed record NotFound : GetUserListResult;
}