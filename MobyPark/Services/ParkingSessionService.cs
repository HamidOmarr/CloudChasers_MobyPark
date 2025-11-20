using System.Security.Cryptography;
using System.Text;
using MobyPark.DTOs.ParkingLot.Request;
using MobyPark.DTOs.ParkingSession.Request;
using MobyPark.Models;
using MobyPark.Models.Repositories.Interfaces;
using MobyPark.Services.Interfaces;
using MobyPark.Services.Results;
using MobyPark.Services.Results.ParkingLot;
using MobyPark.Services.Results.ParkingSession;
using MobyPark.Services.Results.Price;
using MobyPark.Services.Results.UserPlate;
using MobyPark.Validation;

namespace MobyPark.Services;

public class ParkingSessionService : IParkingSessionService
{
    private readonly IParkingSessionRepository _sessions;
    private readonly ParkingLotService _parkingLots;
    private readonly IUserPlateService _userPlates;
    private readonly IPricingService _pricing;
    private readonly IGateService _gate;
    private readonly IPreAuthService _preAuth;

    public ParkingSessionService(
        IParkingSessionRepository parkingSessions, 
        ParkingLotService parkingLots,
        IUserPlateService userPlates,
        IPricingService pricing,
        IGateService gate,
        IPreAuthService preAuth
        )
    {
        _sessions = parkingSessions;
        _parkingLots = parkingLots;
        _userPlates = userPlates;
        _pricing = pricing;
        _gate = gate;
        _preAuth = preAuth;
    }

    public async Task<CreateSessionResult> CreateParkingSession(CreateParkingSessionDto dto)
    {
        dto.LicensePlate = dto.LicensePlate.Upper();
        var exists = await _sessions.GetActiveSessionByLicensePlate(dto.LicensePlate);
        if (exists is not null)
            return new CreateSessionResult.AlreadyExists();

        var session = new ParkingSessionModel
        {
            ParkingLotId = dto.ParkingLotId,
            LicensePlateNumber = dto.LicensePlate,
            Started = dto.Started,
            Stopped = null,
            PaymentStatus = ParkingSessionStatus.PreAuthorized,
            Cost = null
        };

        try
        {
            (bool createdSuccessfully, long id) = await _sessions.CreateWithId(session);
            if (!createdSuccessfully)
                return new CreateSessionResult.Error("Database insertion failed.");
            session.Id = id;
            return new CreateSessionResult.Success(session);
        }
        catch (Exception ex)
        { return new CreateSessionResult.Error(ex.Message); }
    }

    public async Task<GetSessionResult> GetParkingSessionById(long id)
    {
        var session = await _sessions.GetById<ParkingSessionModel>(id);
        if (session is null)
            return new GetSessionResult.NotFound();
        return new GetSessionResult.Success(session);
    }

    public async Task<UpdateSessionResult> UpdateParkingSession(long id, UpdateParkingSessionDto dto)
    {
        var getResult = await GetParkingSessionById(id);
        if (getResult is not GetSessionResult.Success success)
        {
            return getResult switch
            {
                GetSessionResult.NotFound => new UpdateSessionResult.NotFound(),
                _ => new UpdateSessionResult.Error("Failed to retrieve session for update.")
            };
        }
        var existingSession = success.Session;

        bool changed = false;
        bool stoppedChanged = false;

        if (dto.Stopped.HasValue && dto.Stopped != existingSession.Stopped)
        {
            if (dto.Stopped.Value < existingSession.Started)
                return new UpdateSessionResult.Error("Stopped time cannot be before started time.");

            existingSession.Stopped = dto.Stopped.Value;
            changed = true;
            stoppedChanged = true;
        }
        if (dto.PaymentStatus.HasValue && dto.PaymentStatus != existingSession.PaymentStatus)
        {
            existingSession.PaymentStatus = dto.PaymentStatus.Value;
            changed = true;
        }


        if (!changed)
            return new UpdateSessionResult.NoChanges();

        if (stoppedChanged && existingSession.Stopped.HasValue)
        {
            var lot = await _parkingLots.GetParkingLotByIdAsync(existingSession.ParkingLotId);
            if (lot.Status != ServiceStatus.Success)
                return new UpdateSessionResult.Error("Failed to retrieve parking lot for cost recalculation.");

            var parkingLot = new ParkingLotModel
            {
                Id = lot.Data!.Id,
                Name = lot.Data.Name,
                Location = lot.Data.Location,
                Address = lot.Data.Address,
                Capacity = lot.Data.Capacity,
                Tariff = lot.Data.Tariff,
                DayTariff = lot.Data.DayTariff

            };
            
            var costResult = _pricing.CalculateParkingCost(
                parkingLot,
                existingSession.Started,
                existingSession.Stopped.Value
            );

            if (costResult is CalculatePriceResult.Success priceSuccess)
            {
                existingSession.Cost = priceSuccess.Price;
                existingSession.DurationMinutes =
                    (int)Math.Ceiling((existingSession.Stopped.Value - existingSession.Started).TotalMinutes);
            }
            else if (costResult is CalculatePriceResult.Error e)
            {
                var msg = string.IsNullOrWhiteSpace(e.Message)
                    ? "Failed to recalculate cost during update."
                    : e.Message;
                return new UpdateSessionResult.Error(msg);
            }
        }

        try
        {
            bool updated = await _sessions.Update(existingSession, dto);
            if (!updated)
                return new UpdateSessionResult.Error("Session failed to update.");

            return new UpdateSessionResult.Success(existingSession);
        }
        catch (Exception ex)
        { return new UpdateSessionResult.Error(ex.Message); }
    }

    public async Task<DeleteSessionResult> DeleteParkingSession(long id)
    {
        var session = await _sessions.GetById<ParkingSessionModel>(id);
        if (session is null) return new DeleteSessionResult.NotFound();

        try
        {
            if (!await _sessions.Delete(session))
                return new DeleteSessionResult.Error("Database delete failed.");
            return new DeleteSessionResult.Success();
        }
        catch (Exception ex)
        { return new DeleteSessionResult.Error(ex.Message); }
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

    public async Task<GetSessionListResult> GetAllParkingSessions()
    {
        var sessions = await _sessions.GetAll();
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
        licensePlate = licensePlate.Upper();
        var sessions = await _sessions.GetAllRecentSessionsByLicensePlate(licensePlate, recentDuration);
        if (sessions.Count == 0)
            return new GetSessionListResult.NotFound();
        return new GetSessionListResult.Success(sessions);
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

    public async Task<int> CountParkingSessions() => await _sessions.Count();

    public string GeneratePaymentHash(string sessionId, string licensePlate)
    {
        using var md5 = MD5.Create();
        var inputBytes = Encoding.UTF8.GetBytes(sessionId + licensePlate);
        var hashBytes = md5.ComputeHash(inputBytes);
        return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
    }

    public string GenerateTransactionValidationHash() => Guid.NewGuid().ToString("N");

    private async Task<PersistSessionResult> PersistSession(ParkingSessionModel session, ParkingLotModel lot)
    {
        int newReservedCount = Math.Clamp(lot.Reserved + 1, 0, lot.Capacity);

        var lotUpdateDto = new PatchParkingLotDto
        {
            Reserved = newReservedCount
        };

        var lotUpdateResult = await _parkingLots.PatchParkingLotByIdAsync(lot.Id, lotUpdateDto);

        if (lotUpdateResult.Status is not ServiceStatus.Success)
        {
            return new PersistSessionResult.Error(lotUpdateResult.Error!);
        }

        lot.Reserved = newReservedCount;

        try
        {
            (bool createdSuccessfully, long id) = await _sessions.CreateWithId(session);
            if (!createdSuccessfully)
            {
                var rollback = new PatchParkingLotDto { Reserved = Math.Max(0, newReservedCount - 1) };
                await _parkingLots.PatchParkingLotByIdAsync(lot.Id, lotUpdateDto);
                return new PersistSessionResult.Error("Failed to persist parking session (database error).");
            }
            session.Id = id;

            return new PersistSessionResult.Success(session);
        }
        catch (Exception ex)
        {
            var rollback = new PatchParkingLotDto { Reserved = Math.Max(0, newReservedCount - 1) };
            await _parkingLots.PatchParkingLotByIdAsync(lot.Id, lotUpdateDto);

            return new PersistSessionResult.Error(ex.Message);
        }
    }

    private async Task<bool> OpenSessionGate(ParkingSessionModel session, string licensePlate)
        => await _gate.OpenGateAsync(session.ParkingLotId, licensePlate);

    public async Task<StartSessionResult> StartSession(CreateParkingSessionDto sessionDto, string cardToken, decimal estimatedAmount, string? username, bool simulateInsufficientFunds = false)
    {
        var licensePlate = sessionDto.LicensePlate.Upper();

        var lot = await _parkingLots.GetParkingLotByIdAsync(sessionDto.ParkingLotId);
        if (lot.Status is ServiceStatus.NotFound or ServiceStatus.Fail )
            return new StartSessionResult.LotNotFound();
        
        var parkingLot = new ParkingLotModel
        {
            Id = lot.Data!.Id,
            Name = lot.Data.Name,
            Location = lot.Data.Location,
            Address = lot.Data.Address,
            Reserved = lot.Data.Reserved,
            Capacity = lot.Data.Capacity,
            Tariff = lot.Data.Tariff,
            DayTariff = lot.Data.DayTariff

        };
        
        if (parkingLot.Capacity - parkingLot.Reserved <= 0)
            return new StartSessionResult.LotFull();

        var activeSessionResult = await GetActiveParkingSessionByLicensePlate(licensePlate);
        if (activeSessionResult is GetSessionResult.Success)
            return new StartSessionResult.AlreadyActive();

        var preAuth = await _preAuth.PreauthorizeAsync(cardToken, estimatedAmount, simulateInsufficientFunds);
        if (!preAuth.Approved)
            return new StartSessionResult.PreAuthFailed(preAuth.Reason ?? "Card declined");

        var newSession = new ParkingSessionModel
        {
            ParkingLotId = sessionDto.ParkingLotId,
            LicensePlateNumber = licensePlate,
            Started = DateTime.UtcNow,
            Stopped = null,
            PaymentStatus = ParkingSessionStatus.PreAuthorized
        };
        

        var persistResult = await PersistSession(newSession, parkingLot);
        if (persistResult is not PersistSessionResult.Success sPersist)
        {
            return persistResult switch
            {
                PersistSessionResult.Error err => new StartSessionResult.Error(err.Message),
                _ => new StartSessionResult.Error("Unknown error during session persistence.")
            };
        }

        newSession = sPersist.Session;

        try
        {
            if (!await OpenSessionGate(newSession, licensePlate))
                throw new InvalidOperationException("Failed to open gate");
        }
        catch (Exception ex)
        {
            if (newSession.Id > 0)
                await DeleteParkingSession(newSession.Id);

            int rolledBackReservedCount = Math.Max(0, parkingLot.Reserved - 1);
            var rollback = new PatchParkingLotDto { Reserved = rolledBackReservedCount };
            await _parkingLots.PatchParkingLotByIdAsync(parkingLot.Id, rollback);

            return new StartSessionResult.Error("Failed to start session: " + ex.Message);
        }

        return new StartSessionResult.Success(newSession, parkingLot.AvailableSpots);
    }

    private async Task<Dictionary<string, DateTime>> GetPlateOwnershipMapAsync(long userId)
    {
        var userPlatesResult = await _userPlates.GetUserPlatesByUserId(userId);

        if (userPlatesResult is GetUserPlateListResult.Success s)
        {
            return s.Plates.ToDictionary(
                uPlate => uPlate.LicensePlateNumber,
                uPlate => uPlate.CreatedAt.ToDateTime(TimeOnly.MinValue)
            );
        }

        return new Dictionary<string, DateTime>();
    }
}
