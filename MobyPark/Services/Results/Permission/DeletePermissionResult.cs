namespace MobyPark.Services.Results.Permission;

public abstract record DeletePermissionResult
{
    public sealed record Success : DeletePermissionResult;
    public sealed record NotFound : DeletePermissionResult;
    public sealed record Conflict(string Message) : DeletePermissionResult;
    public sealed record Error(string Message) : DeletePermissionResult;
}
