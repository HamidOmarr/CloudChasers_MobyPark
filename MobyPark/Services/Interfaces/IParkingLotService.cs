using MobyPark.DTOs.ParkingLot.Request;
using MobyPark.Models;
using MobyPark.Services.Results.ParkingLot;

namespace MobyPark.Services.Interfaces;

public interface IParkingLotService
{
    Task<ParkingLotModel?> GetParkingLotByAddress(string address);
    Task<ParkingLotModel?> GetParkingLotById(int id);

    Task<RegisterResult> InsertParkingLotAsync(ParkingLotModel parkingLot);

    Task<RegisterResult> UpdateParkingLotByAddressAsync(ParkingLotModel parkingLot, string address);
    Task<RegisterResult> UpdateParkingLotByIDAsync(ParkingLotModel parkingLot, int id);

    Task<RegisterResult> DeleteParkingLotByIDAsync(int id);
    Task<RegisterResult> DeleteParkingLotByAddressAsync(string address);
}
