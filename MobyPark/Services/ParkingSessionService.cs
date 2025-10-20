using System.Security.Cryptography;
using System.Text;
using MobyPark.DTOs.ParkingSession.Request;
using MobyPark.Models;
using MobyPark.Models.Repositories.Interfaces;
using MobyPark.Services.Helpers;
using MobyPark.Services.Interfaces;
using MobyPark.Services.Results.ParkingLot;
using MobyPark.Services.Results.Session;
using MobyPark.Validation;

namespace MobyPark.Services;

public class ParkingSessionService
{
    private readonly IParkingSessionRepository _sessions;
    private readonly IParkingLotService _parkingLots;
    private readonly IUserPlateService _userPlates;
    private readonly IUserService _users;

    public ParkingSessionService(
        IParkingSessionRepository parkingSessions, IParkingLotService parkingLots, IUserPlateService userPlates, IUserService users)
    {
        _sessions = parkingSessions;
        _parkingLots = parkingLots;
        _userPlates = userPlates;
        _users = users;
    }

    public async Task<ParkingSessionModel> CreateParkingSession(ParkingSessionModel session)
    {
        (bool createdSuccessfully, long id) = await _sessions.CreateWithId(session);
        if (createdSuccessfully) session.Id = id;
        return session;
    }

    public async Task<GetSessionResult> GetParkingSessionById(long id)
    {
        var session = await _sessions.GetById<ParkingSessionModel>(id);
        if (session is null)
            return new GetSessionResult.NotFound();
        return new GetSessionResult.Success(session);
    }

    public async Task<GetSessionListResult> GetParkingSessionsByParkingLotId(long lotId)
    {
        var sessions = await _sessions.GetByParkingLotId(lotId);
        if (sessions.Count == 0)
            return new GetSessionListResult.NotFound();
        return new GetSessionListResult.Success(sessions);
    }

    public async Task<GetSessionListResult> GetParkingSessionsByLicensePlate(string licensePlate)
    {
        var sessions = await _sessions.GetByLicensePlateNumber(licensePlate);
        if (sessions.Count == 0)
            return new GetSessionListResult.NotFound();
        return new GetSessionListResult.Success(sessions);
    }

    public async Task<GetSessionListResult> GetParkingSessionsByPaymentStatus(string status)
    {
        if (!Enum.TryParse<ParkingSessionStatus>(status, true, out var parsedStatus))
            return new GetSessionListResult.InvalidInput($"'{status}' is not a valid payment status.");

        var sessions = await _sessions.GetByPaymentStatus(parsedStatus);
        if (sessions.Count == 0)
            return new GetSessionListResult.NotFound();
        return new GetSessionListResult.Success(sessions);
    }

    public async Task<GetSessionListResult> GetActiveParkingSessions()
    {
        var sessions = await _sessions.GetActiveSessions();
        if (sessions.Count == 0)
            return new GetSessionListResult.NotFound();
        return new GetSessionListResult.Success(sessions);
    }

    public async Task<GetSessionResult> GetActiveParkingSessionByLicensePlate(string licensePlate)
    {
        var session = await _sessions.GetActiveSessionByLicensePlate(licensePlate);
        if (session is null)
            return new GetSessionResult.NotFound();
        return new GetSessionResult.Success(session);
    }

    public async Task<GetSessionListResult> GetAllRecentParkingSessionsByLicensePlate(string licensePlate, TimeSpan recentDuration)
    {
        licensePlate = ValHelper.NormalizePlate(licensePlate);
        var sessions = await _sessions.GetAllRecentSessionsByLicensePlate(licensePlate, recentDuration);
        if (sessions.Count == 0)
            return new GetSessionListResult.NotFound();
        return new GetSessionListResult.Success(sessions);
    }

    public async Task<List<ParkingSessionModel>> GetAllParkingSessions() => await _sessions.GetAll();

    public async Task<int> CountParkingSessions() => await _sessions.Count();

    public async Task<SessionUpdateResult> UpdateParkingSession(ParkingSessionModel session)
    {
        var existingSession = await _sessions.GetById<ParkingSessionModel>(session.Id);
        if (existingSession is null)
            return new SessionUpdateResult.NotFound();

        try
        {
            if (!await _sessions.Update(session))
                return new SessionUpdateResult.Error("Database update failed.");
            return new SessionUpdateResult.Success(session);
        }
        catch(Exception ex)
        { return new SessionUpdateResult.Error(ex.Message); }
    }

    public async Task<SessionDeleteResult> DeleteParkingSession(long id)
    {
        var session = await _sessions.GetById<ParkingSessionModel>(id);
        if (session is null)
            return new SessionDeleteResult.NotFound();

        try
        {
            if (!await _sessions.Delete(session))
                return new SessionDeleteResult.Error("Database delete failed.");
            return new SessionDeleteResult.Success();
        }
        catch (Exception ex)
        { return new SessionDeleteResult.Error(ex.Message); }
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

    private async Task<ParkingSessionModel> PersistSession(ParkingSessionModel session, ParkingLotModel lot)
    {
        lot.Reserved = Math.Clamp(lot.Reserved + 1, 0, lot.Capacity);

        var lotUpdateResult = await _parkingLots.UpdateParkingLot(lot);
        if (lotUpdateResult is not UpdateLotResult.Success)
            throw new InvalidOperationException("Failed to update parking lot capacity.");

        var createdSession = await CreateParkingSession(session);
        if (createdSession.Id <= 0)
            throw new InvalidOperationException("Failed to persist parking session");

        return createdSession;
    }

    private async Task<bool> OpenSessionGate(ParkingSessionModel session, string licensePlate)
        => await GateService.OpenGateAsync(session.ParkingLotId, licensePlate);

    public async Task<SessionStartResult> StartSession(ParkingSessionCreateDto sessionDto, string cardToken, decimal estimatedAmount, string? username, bool simulateInsufficientFunds = false)
    {
        var licensePlate = ValHelper.NormalizePlate(sessionDto.LicensePlate);

        var lotResult = await _parkingLots.GetParkingLotById(sessionDto.ParkingLotId);
        if (lotResult is GetLotResult.NotFound)
            return new SessionStartResult.LotNotFound();

        var lot = ((GetLotResult.Success)lotResult).Lot;

        if (lot.Capacity - lot.Reserved <= 0)
            return new SessionStartResult.LotFull();

        if (await _sessions.GetActiveSessionByLicensePlate(licensePlate) is not null)
            return new SessionStartResult.AlreadyActive();

        var preAuth = await PreAuth.PreauthorizeAsync(cardToken, estimatedAmount, simulateInsufficientFunds);
        if (!preAuth.Approved)
            return new SessionStartResult.PreAuthFailed(preAuth.Reason ?? "Card declined");

        var newSession = new ParkingSessionModel
        {
            ParkingLotId = sessionDto.ParkingLotId,
            LicensePlateNumber = licensePlate,
            Started = DateTime.UtcNow,
            Stopped = null,
            PaymentStatus = ParkingSessionStatus.PreAuthorized
        };

        try
        {
            newSession = await PersistSession(newSession, lot);
            if (!await OpenSessionGate(newSession, licensePlate))
                throw new InvalidOperationException("Failed to open gate");
        }
        catch (Exception ex)
        {
            if (newSession.Id > 0) await _sessions.Delete(newSession);

            lot.Reserved = Math.Max(0, lot.Reserved - 1);
            await _parkingLots.UpdateParkingLot(lot);

            return new SessionStartResult.Error("Failed to start session: " + ex.Message);
        }

        return new SessionStartResult.Success(newSession);
    }

    private async Task<UserModel> ResolveSessionUser(string plate, string? username)
    {
        plate = ValHelper.NormalizePlate(plate);

        var existingLink = await _userPlates.GetUserPlatesByPlate(plate);
        if (existingLink.Count > 0)
        {
            foreach (var link in existingLink)
            {
                var user = await _users.GetUserById(link.UserId);
                if (user is null) continue;
                if (string.IsNullOrWhiteSpace(username) || user.Username.ToLower().Equals(username.ToLower()))
                    return user;
            }
        }

        var defaultUser = await _users.GetUserById(UserPlateModel.DefaultUserId);
        var deletedLink = await _userPlates.GetUserPlateByUserIdAndPlate(UserPlateModel.DefaultUserId, plate);

        if (deletedLink is null)
            await _userPlates.AddLicensePlateToUser(UserPlateModel.DefaultUserId, plate);

        return defaultUser!;
    }

    public async Task<List<ParkingSessionModel>> GetAuthorizedSessionsAsync(long userId, int lotId, bool canManageSessions)
    {
        var sessionsResult = await GetParkingSessionsByParkingLotId(lotId);
        if (sessionsResult is not GetSessionListResult.Success success)
            return new List<ParkingSessionModel>();

        var sessions = success.Sessions;

        if (canManageSessions) return sessions;

        var plateOwnershipMap = await GetPlateOwnershipMapAsync(userId);

        var filteredSessions = sessions
            .Where(session =>
                plateOwnershipMap.TryGetValue(session.LicensePlateNumber, out var earliestValidDate) &&
                session.Started >= earliestValidDate).ToList();

        return filteredSessions;
    }

    public async Task<GetSessionResult> GetAuthorizedSessionAsync(long userId, int lotId, int sessionId, bool canManageSessions)
    {
        var sessionResult = await GetParkingSessionById(sessionId);
        if (sessionResult is not GetSessionResult.Success s)
            return new GetSessionResult.NotFound();

        var session = s.Session;
        if (session.ParkingLotId != lotId)
            return new GetSessionResult.NotFound();

        if (canManageSessions)
            return new GetSessionResult.Success(session);

        var plateOwnershipMap = await GetPlateOwnershipMapAsync(userId);
        bool ownsSession =
            plateOwnershipMap.TryGetValue(session.LicensePlateNumber, out var earliestValidDate) &&
            session.Started >= earliestValidDate;

        if (!ownsSession) return new GetSessionResult.Forbidden();
        return new GetSessionResult.Success(session);
    }

    private async Task<Dictionary<string, DateTime>> GetPlateOwnershipMapAsync(long userId)
    {
        var userPlates = await _userPlates.GetUserPlatesByUserId(userId);
        return userPlates.ToDictionary(uPlate => uPlate.LicensePlateNumber, uPlate => uPlate.CreatedAt.ToDateTime(TimeOnly.MinValue));
    }
}
