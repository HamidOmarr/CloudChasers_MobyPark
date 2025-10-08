using MobyPark.Models;

namespace MobyPark.Services.Results.User;

public abstract record UpdateProfileResult
{
    public record Success(UserModel User) : UpdateProfileResult;
    public record NotFound() : UpdateProfileResult;
    public record UsernameTaken() : UpdateProfileResult;
    public record EmailTaken() : UpdateProfileResult;
    public record InvalidData(string Message) : UpdateProfileResult;
    public record Error(string Message) : UpdateProfileResult;
}
