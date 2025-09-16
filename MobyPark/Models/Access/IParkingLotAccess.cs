using MobyPark.Services;

namespace MobyPark.Models.Access;

public interface IParkingLotAccess : IRepository<ParkingLotModel>
{
    Task<ParkingLotModel?> GetByName(string modelName);
}