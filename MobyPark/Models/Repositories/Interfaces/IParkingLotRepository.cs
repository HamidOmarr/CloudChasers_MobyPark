namespace MobyPark.Models.Repositories.Interfaces;

public interface IParkingLotRepository : IRepository<ParkingLotModel>
{
    Task<ParkingLotModel?> GetByName(string name);
    Task<List<ParkingLotModel>> GetByLocation(string location);
}
