using MobyPark.Models;

namespace MobyPark.Services.Results.UserPlate;

public abstract record GetUserPlateListResult
{
    public sealed record Success(List<UserPlateModel> Plates) : GetUserPlateListResult;
    public sealed record NotFound : GetUserPlateListResult;
}