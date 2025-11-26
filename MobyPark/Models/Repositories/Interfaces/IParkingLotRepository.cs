namespace MobyPark.Models.Repositories.Interfaces;

public interface IParkingLotRepository : IRepository<ParkingLotModel>
{
    Task<ParkingLotModel?> GetByName(string modelName);
    Task<ParkingLotModel?> GetParkingLotByID(int id);
    Task<ParkingLotModel?> GetParkingLotByAddress(string address);
    Task<int> AddParkingLotAsync(ParkingLotModel parkingLot);
    Task<ParkingLotModel?> UpdateParkingLotByID(ParkingLotModel parkingLot, int id);
    Task<ParkingLotModel?> UpdateParkingLotByAddress(ParkingLotModel parkingLot, string address);
    Task<bool> DeleteParkingLotByID(int id);
    Task<bool> DeleteParkingLotByAddress(string address);
}
