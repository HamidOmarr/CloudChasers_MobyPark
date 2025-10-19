namespace MobyPark.Models.Repositories.Interfaces;

public interface IUserPlateRepository : IRepository<UserPlateModel>
{
    Task<bool> AddPlateToUser(long userId, string plate);
    Task<List<UserPlateModel>> GetPlatesByUserId(long userId);
    Task<List<UserPlateModel>> GetPlatesByPlate(string plate);
    Task<UserPlateModel?> GetPrimaryPlateByUserId(long userId);
    Task<UserPlateModel?> GetByUserIdAndPlate(long userId, string plate);
}
