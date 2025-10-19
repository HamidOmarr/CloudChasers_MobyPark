using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using MobyPark.Models;
// <<<<<<< HEAD
using MobyPark.Models.Repositories.Interfaces;
using MobyPark.Models.Repositories.RepositoryStack;
using MobyPark.Services.Services;
// =======
// using MobyPark.Models.DataService;
using MobyPark.Services.Exceptions;
// >>>>>>> main

namespace MobyPark.Services;

public class ParkingSessionService
{
// <<<<<<< HEAD
//     private readonly IParkingSessionRepository _sessions;
// // =======
//     private readonly IDataAccess _dataAccess;
//     private readonly PaymentPreauthService? _preauth;
//     private readonly GateService? _gate;
//     private readonly IPasswordHasher<UserModel>? _hasher; // optional if temp user creation requires hashing
// >>>>>>> main

    private readonly IRepositoryStack _repo;

    public ParkingSessionService(IRepositoryStack repoStack)
    {
        _repo = repoStack;
    }

    public ParkingSessionService(
        // IDataAccess dataAccess,
        // PaymentPreauthService preauth,
        // GateService gate,
        IPasswordHasher<UserModel>? hasher = null)
    {
        // _dataAccess = dataAccess;
        // _preauth = preauth;
        // _gate = gate;
        _hasher = hasher;
    }
    private static string NormalizePlate(string plate) => plate.Trim().ToUpperInvariant();



    private async Task<UserModel> ResolveGuestByPlateAsync(string normalizedPlate, string? requestedUsername)
    {
        // existing vehicle → user
        var vehicle = await _repo.LicensePlates.GetByNumber(normalizedPlate);
        if (vehicle is not null)
        {
            var user = await _repo.Users.GetById(vehicle.UserId);
            if (user != null) return user;
        }

        // optional requested username
        if (!string.IsNullOrWhiteSpace(requestedUsername))
        {
            var userByName = await _repo.Users.GetByUsername(requestedUsername);
            if (userByName != null) return userByName;
        }

        // create guest user + vehicle
        var guestId = normalizedPlate;
    var guestUsername = $"GUEST_{guestId}";
        var existingGuest = await _repo.Users.GetByUsername(guestUsername);
        if (existingGuest is not null)
        {
            // Ensure vehicle exists for this plate and user
            var existingVehicle = await _repo.LicensePlates.GetByNumber(normalizedPlate);
            if (existingVehicle is null)
            {
                var newVehicle = new VehicleModel
                {
                    UserId = existingGuest.Id,
                    LicensePlate = normalizedPlate,
                    Make = "Unknown",
                    Model = "Unknown",
                    Color = "Unknown",
                    Year = DateTime.UtcNow.Year,
                    CreatedAt = DateTime.UtcNow
                };
                await _dataAccess.Vehicles.Create(newVehicle);
            }
            return existingGuest;
        }

        var guestUser = new UserModel
        {
            Username = guestUsername,
            Name = $"Guest_{guestId}",
            Email = $"guest_{guestId.ToLowerInvariant()}@guest.local",
            Phone = "+31000000000",
            Role = "GUEST",
            BirthYear = DateTime.UtcNow.Year - 30,
            CreatedAt = DateTime.UtcNow,
            Active = true
        };

        if (_hasher is not null)
            guestUser.PasswordHash = _hasher.HashPassword(guestUser, Guid.NewGuid().ToString("N"));
        else
            guestUser.PasswordHash = Guid.NewGuid().ToString("N");

        var createUser = await _dataAccess.Users.CreateWithId(guestUser);
        if (!createUser.success)
            throw new InvalidOperationException("Failed to create guest user");
        guestUser.Id = createUser.id;

        var vehicleModel = new VehicleModel
        {
            UserId = guestUser.Id,
            LicensePlate = normalizedPlate,
            Make = "Unknown",
            Model = "Unknown",
            Color = "Unknown",
            Year = DateTime.UtcNow.Year,
            CreatedAt = DateTime.UtcNow
        };
        await _dataAccess.Vehicles.Create(vehicleModel);

        return guestUser;
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
// <<<<<<< HEAD
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

        bool deletedSuccessfully = await _repo.ParkingSessions.Delete(session);
        return deletedSuccessfully;
// =======
        await GetParkingSessionById(id);
        bool success = await _repo.ParkingSessions.Delete(id);
        return success;
// >>>>>>> main
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

    // returns created session with id; throws invalidoperationexception if not available; argumentexception on invalid input

    public async Task<ParkingSessionModel> StartSession(ParkingSessionModel session)
    {
        Validator.ParkingSession(session);

        var normalizedPlate = NormalizePlate(session.LicensePlateNumber);

        var lot = await _repo.ParkingLots.GetById<ParkingLotModel>(session.ParkingLotId) ?? throw new KeyNotFoundException("Parking lot not found");
        if (lot.Capacity - lot.Reserved <= 0) throw new InvalidOperationException("Parking lot is full");

        //  check if theres no active session exists for same plate
        var existingActive = await _repo.ParkingSessions.GetActiveSessionByLicensePlate(normalizedPlate);
        if (existingActive is not null) throw new ActiveSessionAlreadyExistsException(normalizedPlate);

        // payment preauth
    }





    // public async Task<ParkingSessionModel> StartSession(long parkingLotId, string licensePlate, string cardToken, decimal estimatedAmount, string? username, bool simulateInsufficientFunds = false)
    // {
    //     if (string.IsNullOrWhiteSpace(licensePlate)) throw new ArgumentException("License plate required", nameof(licensePlate));
    //     if (string.IsNullOrWhiteSpace(cardToken)) throw new ArgumentException("Card token required", nameof(cardToken));
    //     if (estimatedAmount <= 0) throw new ArgumentException("Estimated amount must be > 0", nameof(estimatedAmount));
    //
    //     var normalizedPlate = NormalizePlate(licensePlate);
    //
    //     // check lot availability
    //     var lot = await _repo.ParkingLots.GetById<ParkingLotModel>(parkingLotId) ?? throw new KeyNotFoundException("Parking lot not found");
    //     var available = lot.Capacity - lot.Reserved;
    //     if (available <= 0)
    //         throw new InvalidOperationException("Parking lot is full");
    //
    //     //  check if theres no active session exists for same plate
    //     var existingActive = await _repo.ParkingSessions.GetActiveSessionByLicensePlate(normalizedPlate);
    //     if (existingActive is not null)
    //         throw new ActiveSessionAlreadyExistsException(normalizedPlate);
    //
    //     // payment pre authorization (placeholder!!)!
    //     if (_preauth is not null)
    //     {
    //         var preauth = await _preauth.PreauthorizeAsync(cardToken, estimatedAmount, simulateInsufficientFunds);
    //         if (!preauth.Approved)
    //             throw new UnauthorizedAccessException(preauth.Reason ?? "Card declined");
    //     }
    //
    //     var userEntity = await ResolveGuestByPlateAsync(normalizedPlate, username);
    //     var userNameForSession = userEntity.Username;
    //
    //     // Create session
    //     var session = new ParkingSessionModel
    //     {
    //         ParkingLotId = parkingLotId,
    //         LicensePlate = normalizedPlate,
    //         Started = DateTime.UtcNow,
    //         Stopped = null,
    //         User = userNameForSession,
    //         DurationMinutes = 0,
    //         Cost = 0,
    //         PaymentStatus = "Preauthorized"
    //     };
    //
    //
    //     bool lotIncremented = false;
    //     bool sessionCreated = false;
    //     try
    //     {
    //         // increment
    //         lot.Reserved = Math.Clamp(lot.Reserved + 1, 0, lot.Capacity);
    //         await _repo.ParkingLots.Update(lot);
    //         lotIncremented = true;
    //
    //         var createResult = await _repo.ParkingSessions.CreateWithId(session);
    //         if (createResult.success)
    //         {
    //             session.Id = createResult.id;
    //             sessionCreated = true;
    //         }
    //         else throw new InvalidOperationException("Failed to persist parking session");
    //
    //         if (_gate is not null)
    //         {
    //             var opened = await _gate.OpenGateAsync(parkingLotId, normalizedPlate);
    //             if (!opened)
    //                 throw new InvalidOperationException("Failed to open gate");
    //         }
    //     }
    //     catch
    //     {
    //         // compensation
    //         if (sessionCreated)
    //             await _repo.ParkingSessions.Delete(session);
    //
    //         if (!lotIncremented) throw new InvalidOperationException("Failed to start parking session");
    //         lot.Reserved = Math.Max(0, lot.Reserved - 1);
    //         await _repo.ParkingLots.Update(lot);
    //         throw;
    //     }
    //
    //     return session;
    // }
}
