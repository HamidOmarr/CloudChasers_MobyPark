namespace MobyPark.Services.Results.Role;

public abstract record DeleteRoleResult
{
    public sealed record Success : DeleteRoleResult;
    public sealed record NotFound : DeleteRoleResult;
    public sealed record Forbidden(string Message) : DeleteRoleResult;
    public sealed record Conflict(string Message) : DeleteRoleResult;
    public sealed record Error(string Message) : DeleteRoleResult;
}