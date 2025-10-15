using System.Security.Cryptography;
using System.Text;
using MobyPark.Models;
using MobyPark.Models.DataService;
using MobyPark.Models.Requests;
using MobyPark.Models.Requests.Session;
using MobyPark.Models.Access;

namespace MobyPark.Services;

public class ParkingSessionService
{
    private readonly IDataAccess _dataAccess;

    public ParkingSessionService(IDataAccess dataAccess)
    {
        _dataAccess = dataAccess;
    }

    public async Task<ParkingSessionModel> CreateParkingSession(ParkingSessionModel session)
    {
        if (string.IsNullOrWhiteSpace(session.LicensePlate) ||
            session.ParkingLotId == 0 ||
            session.Started == default ||
            string.IsNullOrWhiteSpace(session.User))
            throw new ArgumentException("Required fields not filled!");

        (bool success, int id) = await _dataAccess.ParkingSessions.CreateWithId(session);
        if (success) session.Id = id;
        return session;
    }

    public async Task<ParkingSessionModel> GetParkingSessionById(int id)
    {
        ParkingSessionModel? session = await _dataAccess.ParkingSessions.GetById(id);
        if (session is null) throw new KeyNotFoundException("Parking session not found");

        return session;
    }

    public async Task<List<ParkingSessionModel>> GetParkingSessionsByParkingLotId(int lotId)
    {
        List<ParkingSessionModel> sessions = await _dataAccess.ParkingSessions.GetByParkingLotId(lotId);
        if (sessions.Count == 0) throw new KeyNotFoundException("No sessions found");

        return sessions;
    }

    public async Task<List<ParkingSessionModel>> GetParkingSessionsByUser(string user)
    {
        List<ParkingSessionModel> sessions = await _dataAccess.ParkingSessions.GetByUser(user);
        if (sessions.Count == 0) throw new KeyNotFoundException("No sessions found");

        return sessions;
    }

    public async Task<List<ParkingSessionModel>> GetParkingSessionsByPaymentStatus(string status)
    {
        List<ParkingSessionModel> sessions = await _dataAccess.ParkingSessions.GetByPaymentStatus(status);
        if (sessions.Count == 0) throw new KeyNotFoundException("No sessions found");

        return sessions;
    }

    public async Task<ParkingSessionModel> GetParkingLotSessionByLicensePlateAndParkingLotId(int parkingLotId, StopSessionRequest request)
    {
        string licensePlate = request.LicensePlate.Trim().ToUpper();
        if (string.IsNullOrWhiteSpace(licensePlate))
        {
            throw new ArgumentException("License plate is required");
        }
        ParkingSessionModel sessions = await _dataAccess.ParkingSessions.GetByParkingLotIdAndLicensePlate(parkingLotId, licensePlate);
        if (sessions == null) throw new KeyNotFoundException("Parking session not found");

        return sessions;

    }

    public async Task<List<ParkingSessionModel>> GetAllParkingSessions() => await _dataAccess.ParkingSessions.GetAll();

    public async Task<int> CountParkingSessions() => await _dataAccess.ParkingSessions.Count();

    public async Task<bool> UpdateParkingSession(ParkingSessionModel session) => await _dataAccess.ParkingSessions.Update(session);

    public async Task<bool> DeleteParkingSession(int id)
    {
        await GetParkingSessionById(id);

        bool success = await _dataAccess.ParkingSessions.Delete(id);
        return success;
    }

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
            if (parkingLot.DayTariff is null)
            {
                // day tariff not set up, just charge normal tariff per hour
                price = parkingLot.Tariff * hours;
                days = 0;
                return (price, hours, days);
            }

            price = (decimal)parkingLot.DayTariff * billableDays;
            days = billableDays;
        }
        else
        {
            price = parkingLot.Tariff * hours;
            if (parkingLot.DayTariff is not null && price > parkingLot.DayTariff)
                price = (decimal)parkingLot.DayTariff;
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
