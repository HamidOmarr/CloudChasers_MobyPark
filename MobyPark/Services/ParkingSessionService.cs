using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using MobyPark.Models;
using MobyPark.Models.DataService;
using MobyPark.Services.Exceptions;

namespace MobyPark.Services;

public class ParkingSessionService
{
    private readonly IDataAccess _dataAccess;
    private readonly PaymentPreauthService? _preauth;
    private readonly GateService? _gate;
    private readonly IPasswordHasher<UserModel>? _hasher; // optional if temp user creation requires hashing

    public ParkingSessionService(IDataAccess dataAccess)
    {
        _dataAccess = dataAccess;
    }

    public ParkingSessionService(
        IDataAccess dataAccess,
        PaymentPreauthService preauth,
        GateService gate,
        IPasswordHasher<UserModel>? hasher = null)
    {
        _dataAccess = dataAccess;
        _preauth = preauth;
        _gate = gate;
        _hasher = hasher;
    }
    private static string NormalizePlate(string plate) => plate.Trim().ToUpperInvariant();



    private async Task<UserModel> EnsureUserForPlateAsync(string normalizedPlate, string? usernameHint)
    {
        // 1. Try find existing vehicle -> user
        var vehicle = await _dataAccess.Vehicles.GetByLicensePlate(normalizedPlate);
        if (vehicle is not null)
        {
            var user = await _dataAccess.Users.GetById(vehicle.UserId);
            if (user != null) return user;
        }

        // 2. Try username hint
        if (!string.IsNullOrWhiteSpace(usernameHint))
        {
            var userByName = await _dataAccess.Users.GetByUsername(usernameHint);
            if (userByName != null) return userByName;
        }

        // 3. Create temporary user + vehicle 
        var tempUser = new UserModel
        {
            Username = $"TEMP_{Guid.NewGuid().ToString("N")[..8]}",
            Name = "Temporary User",
            Email = $"temp_{Guid.NewGuid().ToString("N")[..8]}@temp.local",
            Phone = "+31000000000",
            Role = "TEMP", // TEMP role
            BirthYear = DateTime.UtcNow.Year - 30,
            CreatedAt = DateTime.UtcNow,
            Active = true
        };
        if (_hasher is not null)
            tempUser.PasswordHash = _hasher.HashPassword(tempUser, Guid.NewGuid().ToString("N"));
        else
            tempUser.PasswordHash = Guid.NewGuid().ToString("N");

        var createUser = await _dataAccess.Users.CreateWithId(tempUser);
        if (createUser.success)
            tempUser.Id = createUser.id;

        // create vehicle record
        var vehicleModel = new VehicleModel
        {
            UserId = tempUser.Id,
            LicensePlate = normalizedPlate,
            Make = "Unknown",
            Model = "Unknown",
            Color = "Unknown",
            Year = DateTime.UtcNow.Year,
            CreatedAt = DateTime.UtcNow
        };
        await _dataAccess.Vehicles.Create(vehicleModel);

        return tempUser;
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

    // returns created session with id; throws InvalidOperationException if not available; ArgumentException on invalid input
    public async Task<ParkingSessionModel> StartSession(int parkingLotId, string licensePlate, string cardToken, decimal estimatedAmount, string? username, bool simulateInsufficientFunds = false)
    {
        if (string.IsNullOrWhiteSpace(licensePlate)) throw new ArgumentException("License plate required", nameof(licensePlate));
        if (string.IsNullOrWhiteSpace(cardToken)) throw new ArgumentException("Card token required", nameof(cardToken));
        if (estimatedAmount <= 0) throw new ArgumentException("Estimated amount must be > 0", nameof(estimatedAmount));

        var normalizedPlate = NormalizePlate(licensePlate);

        // 1) Check lot availability
        var lot = await _dataAccess.ParkingLots.GetById(parkingLotId) ?? throw new KeyNotFoundException("Parking lot not found");
        var available = lot.Capacity - lot.Reserved;
        if (available <= 0)
            throw new InvalidOperationException("Parking lot is full");

        //  Ensure no active session exists for same plate
        var existingActive = await _dataAccess.ParkingSessions.GetActiveByLicensePlate(normalizedPlate);
        if (existingActive is not null)
            throw new ActiveSessionAlreadyExistsException(normalizedPlate);

        // Payment pre authorization (placeholder!!)!
        if (_preauth is not null)
        {
            var preauth = await _preauth.PreauthorizeAsync(cardToken, estimatedAmount, simulateInsufficientFunds);
            if (!preauth.Approved)
                throw new UnauthorizedAccessException(preauth.Reason ?? "Card declined");
        }

        // create user
        var userEntity = await EnsureUserForPlateAsync(normalizedPlate, username);
        var userNameForSession = userEntity.Username;

        // Create session
        var session = new ParkingSessionModel
        {
            ParkingLotId = parkingLotId,
            LicensePlate = normalizedPlate,
            Started = DateTime.UtcNow,
            Stopped = null,
            User = userNameForSession,
            DurationMinutes = 0,
            Cost = 0,
            PaymentStatus = "Preauthorized"
        };


        bool lotIncremented = false;
        bool sessionCreated = false;
        try
        {
            // increment
            lot.Reserved = Math.Clamp(lot.Reserved + 1, 0, lot.Capacity);
            await _dataAccess.ParkingLots.Update(lot);
            lotIncremented = true;

            var createResult = await _dataAccess.ParkingSessions.CreateWithId(session);
            if (createResult.success)
            {
                session.Id = createResult.id;
                sessionCreated = true;
            }
            else throw new InvalidOperationException("Failed to persist parking session");

            if (_gate is not null)
            {
                var opened = await _gate.OpenGateAsync(parkingLotId, normalizedPlate);
                if (!opened)
                    throw new InvalidOperationException("Failed to open gate");
            }
        }
        catch
        {
            // compensation
            if (sessionCreated)
            {
                await _dataAccess.ParkingSessions.Delete(session.Id);
            }
            if (lotIncremented)
            {
                lot.Reserved = Math.Max(0, lot.Reserved - 1);
                await _dataAccess.ParkingLots.Update(lot);
            }
            throw;
        }

        return session;
    }
}
