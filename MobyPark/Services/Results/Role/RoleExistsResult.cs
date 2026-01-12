namespace MobyPark.Services.Results.Role;

public abstract record RoleExistsResult
{
    public sealed record Exists : RoleExistsResult;
    public sealed record NotExists : RoleExistsResult;
    public sealed record InvalidInput(string Message) : RoleExistsResult;
    public sealed record Error(string Message) : RoleExistsResult;
}