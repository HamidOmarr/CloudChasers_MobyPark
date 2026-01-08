using MobyPark.Models;

namespace MobyPark.Services.Results.User;

public abstract record UpdateUserResult
{
    public record Success(UserModel User) : UpdateUserResult;
    public record NoChangesMade : UpdateUserResult;
    public record NotFound : UpdateUserResult;
    public record UsernameTaken : UpdateUserResult;
    public record EmailTaken : UpdateUserResult;
    public record InvalidData(string Message) : UpdateUserResult;
    public record Error(string Message) : UpdateUserResult;
}