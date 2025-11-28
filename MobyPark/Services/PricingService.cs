using MobyPark.Models;
using MobyPark.Services.Interfaces;
using MobyPark.Services.Results.Price;

namespace MobyPark.Services;

public class PricingService : IPricingService
{
    public CalculatePriceResult CalculateParkingCost(ParkingLotModel parkingLot, DateTimeOffset startTime, DateTimeOffset endTime)
    {
        if (endTime <= startTime)
            return new CalculatePriceResult.Error("End time must be after start time");

        TimeSpan duration = endTime - startTime;
        int billableHours = (int)Math.Ceiling(duration.TotalHours);
        int billableDays = 0;
        decimal price;

        decimal hourlyPrice = billableHours * parkingLot.Tariff;

        if (parkingLot.DayTariff.HasValue)
        {
            if (duration.TotalHours > 24)
            {
                billableDays = (int)Math.Ceiling(duration.TotalDays);
                price = billableDays * parkingLot.DayTariff.Value;
            }
            else
            {
                price = Math.Min(hourlyPrice, parkingLot.DayTariff.Value);
                if (price == parkingLot.DayTariff.Value) billableDays = 1;
            }
        }
        else price = hourlyPrice;

        price = Math.Max(0, price);
        return new CalculatePriceResult.Success(price, billableHours, billableDays);
    }
}