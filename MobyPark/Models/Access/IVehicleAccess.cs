using MobyPark.Services;

namespace MobyPark.Models.Access;

public interface IVehicleAccess : IRepository<VehicleModel>
{
    Task<List<VehicleModel>> GetByUserId(int userId);
    Task<VehicleModel?> GetByLicensePlate(string licensePlate);
}