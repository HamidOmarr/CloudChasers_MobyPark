using MobyPark.Models;
using MobyPark.Services.Results.ParkingLot;

namespace MobyPark.Services.Interfaces;

public interface IParkingLotService
{
    Task<CreateLotResult> CreateParkingLot(ParkingLotModel lot);
    Task<GetLotResult> GetParkingLotById(long id);
    Task<GetLotResult> GetParkingLotByName(string name);
    Task<GetLotListResult> GetParkingLotsByLocation(string location);
    Task<GetLotListResult> GetAllParkingLots();
    Task<ParkingLotExistsResult> ParkingLotExists(string checkBy, string filterValue);
    Task<int> CountParkingLots();
    Task<UpdateLotResult> UpdateParkingLot(ParkingLotModel lot);
    Task<DeleteLotResult> DeleteParkingLot(long id);
}
