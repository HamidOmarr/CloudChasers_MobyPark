using System.Security.Cryptography;
using System.Text;
using MobyPark.Models;
using MobyPark.Models.Repositories.Interfaces;
using MobyPark.Models.Repositories.RepositoryStack;
using MobyPark.Services.Services;

namespace MobyPark.Services;

public class ParkingSessionService
{
    private readonly IParkingSessionRepository _sessions;

    public ParkingSessionService(IRepositoryStack repoStack)
    {
        _sessions = repoStack.ParkingSessions;
    }

    public async Task<ParkingSessionModel> CreateParkingSession(ParkingSessionModel session)
    {
        Validator.ParkingSession(session);

        (bool createdSuccessfully, long id) = await _sessions.CreateWithId(session);
        if (createdSuccessfully) session.Id = id;
        return session;
    }

    public async Task<ParkingSessionModel> GetParkingSessionById(long id)
    {
        ParkingSessionModel? session = await _sessions.GetById<ParkingSessionModel>(id);
        if (session is null) throw new KeyNotFoundException("Parking session not found");

        return session;
    }

    public async Task<List<ParkingSessionModel>> GetParkingSessionsByParkingLotId(long lotId)
    {
        List<ParkingSessionModel> sessions = await _sessions.GetByParkingLotId(lotId);
        if (sessions.Count == 0) throw new KeyNotFoundException("No sessions found");

        return sessions;
    }

    public async Task<List<ParkingSessionModel>> GetParkingSessionsByLicensePlate(string licensePlate)
    {
        List<ParkingSessionModel> sessions = await _sessions.GetByLicensePlateNumber(licensePlate);
        if (sessions.Count == 0) throw new KeyNotFoundException("No sessions found");

        return sessions;
    }

    public async Task<List<ParkingSessionModel>> GetParkingSessionsByPaymentStatus(string status)
    {
        if (!Enum.TryParse<ParkingSessionStatus>(status, true, out var parsedStatus))
            throw new ArgumentException("Invalid payment status", nameof(status));
        List<ParkingSessionModel> sessions = await _sessions.GetByPaymentStatus(parsedStatus);
        if (sessions.Count == 0) throw new KeyNotFoundException("No sessions found");

        return sessions;
    }

    public async Task<List<ParkingSessionModel>> GetActiveParkingSessions()
    {
        List<ParkingSessionModel> sessions = await _sessions.GetActiveSessions();
        if (sessions.Count == 0) throw new KeyNotFoundException("No active sessions found");

        return sessions;
    }

    public async Task<ParkingSessionModel> GetActiveParkingSessionByLicensePlate(string licensePlate)
    {
        ParkingSessionModel? session = await _sessions.GetActiveSessionByLicensePlate(licensePlate);
        if (session is null) throw new KeyNotFoundException("No active session found for this license plate");

        return session;
    }

    public async Task<List<ParkingSessionModel>> GetAllParkingSessions() => await _sessions.GetAll();

    public async Task<int> CountParkingSessions() => await _sessions.Count();

    public async Task<bool> UpdateParkingSession(ParkingSessionModel session)
    {
        Validator.ParkingSession(session);

        var existingSession = await GetParkingSessionById(session.Id);
        if (existingSession is null) throw new KeyNotFoundException("Parking session not found");

        bool updatedSuccessfully = await _sessions.Update(session);
        return updatedSuccessfully;
    }

    public async Task<bool> DeleteParkingSession(long id)
    {
        var session = await GetParkingSessionById(id);
        if (session is null) throw new KeyNotFoundException("Parking session not found");

        bool deletedSuccessfully = await _sessions.Delete(session);
        return deletedSuccessfully;
    }

    public (decimal Price, int Hours, int Days) CalculatePrice(ParkingLotModel parkingLot, ParkingSessionModel session)
    {
        Validator.ParkingLot(parkingLot);
        Validator.ParkingSession(session);

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
