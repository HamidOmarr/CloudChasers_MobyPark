namespace MobyPark.Services.Results.Permission;

public abstract record PermissionExistsResult
{
    public sealed record Exists : PermissionExistsResult;
    public sealed record NotExists : PermissionExistsResult;
    public sealed record InvalidInput(string Message) : PermissionExistsResult;
    public sealed record Error(string Message) : PermissionExistsResult;
}