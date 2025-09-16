using System.Security.Cryptography;
using System.Text;
using MobyPark.Models;

namespace MobyPark.Services;

public class ParkingSessionService
{
    public ParkingSessionService() { }

    public (decimal Price, int Hours, int Days) CalculatePrice(ParkingLotModel parkingLot, ParkingSessionModel session)
    {
        decimal price;
        DateTime start = session.Started;
        DateTime end = session.Stopped ?? DateTime.Now;
        TimeSpan diff = end - start;

        int hours = (int)Math.Ceiling(diff.TotalSeconds / 3600);
        int days = diff.Days; // only count full 24h periods

        if (diff.TotalSeconds < 180)
        {
            price = 0;
            hours = 0;
            days = 0;
        }
        else if (days > 0)
        {
            // Round up partial leftover hours to another day
            int billableDays = (int)Math.Ceiling(diff.TotalHours / 24.0);
            price = parkingLot.DayTariff * billableDays;
            days = billableDays;
        }
        else
        {
            price = parkingLot.Tariff * hours;
            if (price > parkingLot.DayTariff)
                price = parkingLot.DayTariff;
        }

        price = Math.Max(0, price);

        return (price, hours, days);
    }



    public string GeneratePaymentHash(string sessionId, string licensePlate)
    {
        using var md5 = MD5.Create();
        var inputBytes = Encoding.UTF8.GetBytes(sessionId + licensePlate);
        var hashBytes = md5.ComputeHash(inputBytes);
        return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
    }

    public string GenerateTransactionValidationHash() => Guid.NewGuid().ToString("N");
}
