using MobyPark.Models;
using MobyPark.Services.Results.Price;

namespace MobyPark.Services.Interfaces;

public interface IPricingService
{
    CalculatePriceResult CalculateParkingCost(ParkingLotModel parkingLot, DateTime startTime, DateTime endTime);
}