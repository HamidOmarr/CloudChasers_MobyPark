using System.Security.Cryptography;
using System.Text;
using MobyPark.DTOs.ParkingSession.Request;
using MobyPark.Models;
using MobyPark.Models.Repositories.Interfaces;
using MobyPark.Models.Repositories.RepositoryStack;
using MobyPark.Services.Exceptions;
using MobyPark.Services.Helpers;
using MobyPark.Validation;

namespace MobyPark.Services;

public class ParkingSessionService
{
    private readonly IRepositoryStack _repo;
    private readonly IParkingSessionRepository _sessions;

    public ParkingSessionService(IRepositoryStack repoStack)
    {
        _repo = repoStack;
        _sessions = repoStack.ParkingSessions;
    }

    public async Task<ParkingSessionModel> CreateParkingSession(ParkingSessionModel session)
    {
        ServiceValidator.ParkingSession(session);

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

    public async Task<List<ParkingSessionModel>> GetAllRecentParkingSessionsByLicensePlate(string licensePlate, TimeSpan recentDuration)
    {
        licensePlate = ValHelper.NormalizePlate(licensePlate);
        var sessions = await _sessions.GetAllRecentSessionsByLicensePlate(licensePlate, recentDuration);
        if (sessions.Count == 0) throw new KeyNotFoundException("No recent sessions found for this license plate");

        return sessions;
    }

    public async Task<List<ParkingSessionModel>> GetAllParkingSessions() => await _sessions.GetAll();

    public async Task<int> CountParkingSessions() => await _sessions.Count();

    public async Task<bool> UpdateParkingSession(ParkingSessionModel session)
    {
        ServiceValidator.ParkingSession(session);

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
        ServiceValidator.ParkingLot(parkingLot);
        ServiceValidator.ParkingSession(session);

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

    private async Task<ParkingLotModel> ValidateAndGetLot(long lotId)
    {
        var lot = await _repo.ParkingLots.GetById<ParkingLotModel>(lotId) ?? throw new KeyNotFoundException("Parking lot not found");
        if (lot.Capacity - lot.Reserved <= 0) throw new InvalidOperationException("Parking lot is full");

        return lot;
    }

    private async Task<ParkingSessionModel> PersistSession(ParkingSessionModel session, ParkingLotModel lot)
    {
        lot.Reserved = Math.Clamp(lot.Reserved + 1, 0, lot.Capacity);
        await _repo.ParkingLots.Update(lot);

        var createdSession = await CreateParkingSession(session);
        if (createdSession.Id <= 0) throw new InvalidOperationException("Failed to persist parking session");

        return createdSession;
    }

    private async Task OpenSessionGate(ParkingSessionModel session, string licensePlate)
    {
        if (!await GateService.OpenGateAsync(session.ParkingLotId, licensePlate))
            throw new InvalidOperationException("Failed to open gate");
    }


    public async Task<ParkingSessionModel> StartSession(ParkingSessionCreateDto sessionDto, string cardToken, decimal estimatedAmount, string? username, bool simulateInsufficientFunds = false)
    {
        DtoValidator.ParkingSessionCreate(sessionDto);

        var licensePlate = ValHelper.NormalizePlate(sessionDto.LicensePlate);
        var lot = await ValidateAndGetLot(sessionDto.ParkingLotId);

        if (await _sessions.GetActiveSessionByLicensePlate(licensePlate) is not null)
            throw new ActiveSessionAlreadyExistsException(licensePlate);

        var preAuth = await PreAuth.PreauthorizeAsync(cardToken, estimatedAmount, simulateInsufficientFunds);
        if (!preAuth.Approved) throw new UnauthorizedAccessException(preAuth.Reason ?? "Card declined");

        var newSession = new ParkingSessionModel
        {
            ParkingLotId = sessionDto.ParkingLotId,
            LicensePlateNumber = licensePlate,
            Started = DateTime.UtcNow,
            Stopped = null,
            DurationMinutes = 0,
            Cost = 0,
            PaymentStatus = ParkingSessionStatus.PreAuthorized
        };

        try
        {
            newSession = await PersistSession(newSession, lot);
            await OpenSessionGate(newSession, licensePlate);
        }
        catch
        {
            // compensate
            if (newSession.Id > 0) await _sessions.Delete(newSession);

            lot.Reserved = Math.Max(0, lot.Reserved - 1);
            await _repo.ParkingLots.Update(lot);

            throw new InvalidOperationException("Failed to start parking session");
        }

        return newSession;
    }

    private async Task<UserModel> ResolveSessionUser(string plate, string? username)
    {
        plate = ValHelper.NormalizePlate(plate);

        var existingLink = await _repo.UserPlates.GetPlatesByPlate(plate);
        if (existingLink.Count > 0)
        {
            foreach (var link in existingLink)
            {
                var user = await _repo.Users.GetById<UserModel>(link.UserId);
                if (user is null) continue;
                if (string.IsNullOrWhiteSpace(username) || user.Username.ToLower().Equals(username.ToLower()))
                    return user;
            }
        }

        // NOTE: Using the default user now instead of creating guest users. This ensures sessions link to plates instead of users.
        var defaultUser = await _repo.Users.GetById<UserModel>(UserPlateModel.DefaultUserId);
        var deletedLink = await _repo.UserPlates.GetByUserIdAndPlate(UserPlateModel.DefaultUserId, plate);

        if (deletedLink is null)
            await _repo.UserPlates.AddPlateToUser(UserPlateModel.DefaultUserId, plate);

        return defaultUser!;
    }
}
