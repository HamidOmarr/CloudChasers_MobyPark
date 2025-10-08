using System.Security.Cryptography;
using System.Text;
using MobyPark.Models;
using MobyPark.Models.DataService;

namespace MobyPark.Services;

public class ParkingSessionService
{
    private readonly IDataAccess _dataAccess;
    private readonly PaymentPreauthService? _preauth;
    private readonly GateService? _gate;

    public ParkingSessionService(IDataAccess dataAccess, PaymentPreauthService preauth, GateService gate)
    {
        _dataAccess = dataAccess;
        _preauth = preauth;
        _gate = gate;

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

    // Contract: throws InvalidOperationException if not available; ArgumentException on invalid input; returns created session with id
    public async Task<ParkingSessionModel> StartSession(int parkingLotId, string licensePlate, string cardToken, decimal estimatedAmount, string? username, bool simulateInsufficientFunds = false)
    {
        if (string.IsNullOrWhiteSpace(licensePlate)) throw new ArgumentException("License plate required", nameof(licensePlate));
        if (string.IsNullOrWhiteSpace(cardToken)) throw new ArgumentException("Card token required", nameof(cardToken));
        if (estimatedAmount <= 0) throw new ArgumentException("Estimated amount must be > 0", nameof(estimatedAmount));

        // 1) Check lot availability
        var lot = await _dataAccess.ParkingLots.GetById(parkingLotId) ?? throw new KeyNotFoundException("Parking lot not found");
        var available = lot.Capacity - lot.Reserved;
        if (available <= 0)
            throw new InvalidOperationException("Parking lot is full");

        // 2) Payment pre authorization (placeholder!)
        if (_preauth is not null)
        {
            var preauth = await _preauth.PreauthorizeAsync(cardToken, estimatedAmount, simulateInsufficientFunds);
            if (!preauth.Approved)
                throw new UnauthorizedAccessException(preauth.Reason ?? "Card declined");
        }

        // 3) Resolve user by license plate link if present -> otherwise create a temporary user placeholder in DB
        int? userId = null;
        string userNameForSession = username ?? "TEMP";
        var existingVehicle = await _dataAccess.Vehicles.GetByLicensePlate(licensePlate);
        if (existingVehicle is not null)
        {
            userId = existingVehicle.UserId;
            // try resolve username if needed
            var existingUser = await _dataAccess.Users.GetById(userId.Value);
            if (existingUser is not null)
                userNameForSession = existingUser.Username;
        }

        // 4) Create session
        var session = new ParkingSessionModel
        {
            ParkingLotId = parkingLotId,
            LicensePlate = licensePlate,
            Started = DateTime.UtcNow,
            Stopped = null,
            User = userNameForSession,
            DurationMinutes = 0,
            Cost = 0,
            PaymentStatus = "Preauthorized"
        };

        var createResult = await _dataAccess.ParkingSessions.CreateWithId(session);
        if (createResult.success)
            session.Id = createResult.id;

        // 5) Update lot reserved
        lot.Reserved = Math.Clamp(lot.Reserved + 1, 0, lot.Capacity);
        await _dataAccess.ParkingLots.Update(lot);

        // 6) Open gate (placeholder)
        if (_gate is not null)
        {
            var opened = await _gate.OpenGateAsync(parkingLotId, licensePlate);
            if (!opened)
                throw new InvalidOperationException("Failed to open gate");
        }

        return session;
    }

    public async Task<int> StartParkingSession(int lotId, string licensePlate, string username, DateTime startTime)
    {
        // *** BUSINESS LOGIC HERE ***
        if (string.IsNullOrWhiteSpace(licensePlate))
        {
            throw new ArgumentException("License plate is required.");
        }

        // Check for existing active session (The core rule)
        var existingActiveSession = await GetActiveSessionByLicensePlate(licensePlate);
        if (existingActiveSession != null)
        {
            // Throw a custom exception indicating the business rule was violated
            throw new ActiveSessionAlreadyExistsException(licensePlate);
        }

        // If all rules pass, proceed to data access
        // ... logic to create and save the new session ...
        return newSessionId;
    }
}
