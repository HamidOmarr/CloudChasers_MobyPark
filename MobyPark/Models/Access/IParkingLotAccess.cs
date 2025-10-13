using MobyPark.Services;

namespace MobyPark.Models.Access;

public interface IParkingLotAccess : IRepository<ParkingLotModel>
{
    Task<ParkingLotModel?> GetByName(string modelName);
    Task<ParkingLotModel?> GetParkingLotByID(int id);
    Task<ParkingLotModel?> GetParkingLotByAddress(string address);
    Task<int> AddParkingLotAsync(ParkingLotModel parkingLot);
}