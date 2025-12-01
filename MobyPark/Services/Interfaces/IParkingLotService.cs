using MobyPark.DTOs.ParkingLot.Request;
using MobyPark.Models;
using MobyPark.Services.Results;
using MobyPark.Services.Results.ParkingLot;

namespace MobyPark.Services.Interfaces;

public interface IParkingLotService
{
    Task<ServiceResult<ReadParkingLotDto>> GetParkingLotByAddressAsync(string address);
    Task<ServiceResult<ReadParkingLotDto>> GetParkingLotByIdAsync(long id);
    Task<ServiceResult<ReadParkingLotDto>> CreateParkingLotAsync(CreateParkingLotDto parkingLot);
    Task<ServiceResult<ReadParkingLotDto>> PatchParkingLotByAddressAsync(string address, PatchParkingLotDto updateLot);
    Task<ServiceResult<ReadParkingLotDto>> PatchParkingLotByIdAsync(long id, PatchParkingLotDto updateLot);
    Task<ServiceResult<bool>> DeleteParkingLotByIdAsync(long id);
    Task<ServiceResult<bool>> DeleteParkingLotByAddressAsync(string address);
    Task<ServiceResult<List<ReadParkingLotDto>>> GetAllParkingLotsAsync();
    Task<ServiceResult<int>> GetAvailableSpotsByLotIdAsync(long id);
    Task<ServiceResult<int>> GetAvailableSpotsByAddressAsync(string address);

    Task<ServiceResult<int>> GetAvailableSpotsForPeriodAsync(
        long lotId,
        DateTime start,
        DateTime end);
}
