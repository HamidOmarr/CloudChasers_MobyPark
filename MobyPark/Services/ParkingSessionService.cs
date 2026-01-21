using System.Security.Cryptography;
using System.Text;

using MobyPark.DTOs.Cards;
using MobyPark.DTOs.Hotel;
using MobyPark.DTOs.Invoice;
using MobyPark.DTOs.ParkingLot.Request;
using MobyPark.DTOs.ParkingSession.Request;
using MobyPark.Models;
using MobyPark.Models.Repositories;
using MobyPark.Models.Repositories.Interfaces;
using MobyPark.Services.Interfaces;
using MobyPark.Services.Results;
using MobyPark.Services.Results.Invoice;
using MobyPark.Services.Results.ParkingSession;
using MobyPark.Services.Results.Payment;
using MobyPark.Services.Results.Price;
using MobyPark.Services.Results.Transaction;
using MobyPark.Services.Results.UserPlate;
using MobyPark.Validation;

namespace MobyPark.Services;

public class ParkingSessionService : IParkingSessionService
{
    private readonly IParkingSessionRepository _sessions;
    private readonly IPaymentRepository _paymentRepo;
    private readonly ITransactionRepository _transactionRepo;
    private readonly IInvoiceRepository _invoiceRepository;
    private readonly IParkingLotService _parkingLots;
    private readonly IUserPlateService _userPlates;
    private readonly IPricingService _pricing;
    private readonly IGateService _gate;
    private readonly IPaymentService _payments;
    private readonly ITransactionService _transactions;
    private readonly IPreAuthService _preAuth;
    private readonly IHotelPassService _passService;
    private readonly IAutomatedInvoiceService _invoiceService;
    private readonly IBusinessParkingRegistrationService _registrationService;

    public ParkingSessionService(
        IParkingSessionRepository parkingSessions,
        IPaymentRepository paymentRepo,
    ITransactionRepository transactionRepo,
        IParkingLotService parkingLots,
        IUserPlateService userPlates,
        IPricingService pricing,
        IGateService gate,
        IPaymentService payments,
        ITransactionService transactions,
        IPreAuthService preAuth,
        IHotelPassService passService,
        IAutomatedInvoiceService invoiceService,
        IInvoiceRepository invoiceRepository,
        IBusinessParkingRegistrationService registrationService

        )
    {
        _sessions = parkingSessions;
        _paymentRepo = paymentRepo;
        _transactionRepo = transactionRepo;
        _parkingLots = parkingLots;
        _userPlates = userPlates;
        _pricing = pricing;
        _gate = gate;
        _payments = payments;
        _transactions = transactions;
        _preAuth = preAuth;
        _passService = passService;
        _invoiceService = invoiceService;
        _invoiceRepository = invoiceRepository;
        _registrationService = registrationService;
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
                await _parkingLots.PatchParkingLotByIdAsync(lot.Id, rollback);
                return new PersistSessionResult.Error("Failed to persist parking session (database error).");
            }
            session.Id = id;

            return new PersistSessionResult.Success(session);
        }
        catch (Exception ex)
        {
            var rollback = new PatchParkingLotDto { Reserved = Math.Max(0, newReservedCount - 1) };
            await _parkingLots.PatchParkingLotByIdAsync(lot.Id, rollback);

            return new PersistSessionResult.Error(ex.Message);
        }
    }

    private async Task<bool> OpenSessionGate(ParkingSessionModel session, string licensePlate)
        => await _gate.OpenGateAsync(session.ParkingLotId, licensePlate);

    // public async Task<StartSessionResult> StartSession(
    //     CreateParkingSessionDto sessionDto,
    //     string cardToken,
    //     decimal estimatedAmount,
    //     string? username,
    //     bool simulateInsufficientFunds = false)
    // {
    //     var licensePlate = sessionDto.LicensePlate.Upper();
    //
    //     var lot = await _parkingLots.GetParkingLotByIdAsync(sessionDto.ParkingLotId);
    //     if (lot.Status is ServiceStatus.NotFound or ServiceStatus.Fail)
    //         return new StartSessionResult.LotNotFound();
    //
    //     if (lot.Status != ServiceStatus.Success)
    //     {
    //         if (lot.Error is null)
    //             throw new InvalidOperationException("ServiceResult error is null for non-success status");
    //         return new StartSessionResult.Error(lot.Error);
    //     }
    //
    //     var parkingLot = new ParkingLotModel
    //     {
    //         Id = lot.Data!.Id,
    //         Name = lot.Data.Name,
    //         Location = lot.Data.Location,
    //         Address = lot.Data.Address,
    //         Reserved = lot.Data.Reserved,
    //         Capacity = lot.Data.Capacity,
    //         Tariff = lot.Data.Tariff,
    //         DayTariff = lot.Data.DayTariff
    //
    //     };
    //
    //     if (parkingLot.Capacity - parkingLot.Reserved <= 0)
    //         return new StartSessionResult.LotFull();
    //
    //     var activeSessionResult = await GetActiveParkingSessionByLicensePlate(licensePlate);
    //     if (activeSessionResult is GetSessionResult.Success)
    //         return new StartSessionResult.AlreadyActive();
    //
    //     var hasHotelPass =
    //         await _passService.GetActiveHotelPassByLicensePlateAndLotIdAsync(parkingLot.Id, licensePlate);
    //     var hasActiveBusinessRegistration = await _registrationService.GetActiveBusinessRegistrationByLicencePlateAsync(licensePlate);
    //
    //     ParkingSessionModel session = new ParkingSessionModel();
    //     if (hasHotelPass.Status == ServiceStatus.Success)
    //     {
    //         session.ParkingLotId = sessionDto.ParkingLotId;
    //         session.LicensePlateNumber = licensePlate;
    //         session.Started = DateTimeOffset.UtcNow;
    //         session.Stopped = null;
    //         session.PaymentStatus = ParkingSessionStatus.HotelPass;
    //         session.HotelPassId = hasHotelPass.Data!.Id;
    //     }
    //     else if (hasActiveBusinessRegistration.Status == ServiceStatus.Success)
    //     {
    //         session.ParkingLotId = sessionDto.ParkingLotId;
    //         session.LicensePlateNumber = licensePlate;
    //         session.Started = DateTimeOffset.UtcNow;
    //         session.Stopped = null;
    //         session.PaymentStatus = ParkingSessionStatus.BusinessParking;
    //         session.BusinessParkingRegistrationId = hasActiveBusinessRegistration.Data!.Id;
    //     }
    //     else
    //     {
    //         var preAuth = await _preAuth.PreauthorizeAsync(cardToken, estimatedAmount);
    //         if (!preAuth.Approved)
    //             return new StartSessionResult.PreAuthFailed(preAuth.Reason ?? "Card declined");
    //
    //         session.ParkingLotId = sessionDto.ParkingLotId;
    //         session.LicensePlateNumber = licensePlate;
    //         session.Started = DateTimeOffset.UtcNow;
    //         session.Stopped = null;
    //         session.PaymentStatus = ParkingSessionStatus.PreAuthorized;
    //     }
    //
    //
    //     var persistResult = await PersistSession(session, parkingLot);
    //     if (persistResult is not PersistSessionResult.Success sPersist)
    //     {
    //         return persistResult switch
    //         {
    //             PersistSessionResult.Error err => new StartSessionResult.Error(err.Message),
    //             _ => new StartSessionResult.Error("Unknown error during session persistence.")
    //         };
    //     }
    //
    //     session = sPersist.Session;
    //
    //     try
    //     {
    //         if (!await OpenSessionGate(session, licensePlate))
    //             throw new InvalidOperationException("Failed to open gate");
    //     }
    //     catch (Exception ex)
    //     {
    //         if (session.Id > 0)
    //             await DeleteParkingSession(session.Id);
    //
    //         int rolledBackReservedCount = Math.Max(0, parkingLot.Reserved - 1);
    //         var rollback = new PatchParkingLotDto { Reserved = rolledBackReservedCount };
    //         await _parkingLots.PatchParkingLotByIdAsync(parkingLot.Id, rollback);
    //
    //         return new StartSessionResult.Error("Failed to start session: " + ex.Message);
    //     }
    //
    //     return new StartSessionResult.Success(session, parkingLot.AvailableSpots);
    // }

    public async Task<StartSessionResult> StartSession(CreateParkingSessionDto sessionDto)
    {
        var licensePlate = sessionDto.LicensePlate.Upper();
        var lot = await _parkingLots.GetParkingLotByIdAsync(sessionDto.ParkingLotId);

        if (lot.Status is not ServiceStatus.Success)
            return new StartSessionResult.LotNotFound();

        ParkingLotModel parkingLot = new()
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

        var hotelPass = await _passService
            .GetActiveHotelPassByLicensePlateAndLotIdAsync(parkingLot.Id, licensePlate);
        var activeBusinessRegistration = await _registrationService
            .GetActiveBusinessRegistrationByLicencePlateAsync(licensePlate);

        ParkingSessionModel session = new();

        if (hotelPass.Status is ServiceStatus.Success)
        {
            session.PaymentStatus = ParkingSessionStatus.HotelPass;
            session.HotelPassId = hotelPass.Data!.Id;
        }
        else if (activeBusinessRegistration.Status is ServiceStatus.Success)
        {
            session.PaymentStatus = ParkingSessionStatus.BusinessParking;
            session.BusinessParkingRegistrationId = activeBusinessRegistration.Data!.Id;
        }
        else
        {
            // User must provide payment info.
            // This will return to the controller (PaymentRequired not implemented in controller yet)
            // This signal will tell the computer at the parking lot to ask for payment info.
            // This in turn will initiate a new StartPaidSession controller call with payment info in the DTO.
            return new StartSessionResult.PaymentRequired("Please hold your payment card to the scanner");
        }

        session.ParkingLotId = sessionDto.ParkingLotId;
        session.LicensePlateNumber = licensePlate;
        session.Started = DateTimeOffset.UtcNow;
        session.Stopped = null;

        try
        {
            var persistResult = await PersistSession(session, parkingLot);
            if (persistResult is not PersistSessionResult.Success sPersist)
                throw new InvalidOperationException("Failed to persist session");

            session = sPersist.Session;
            if (!await OpenSessionGate(session, licensePlate))
                throw new InvalidOperationException("Failed to open gate");
            return new StartSessionResult.Success(session, parkingLot.AvailableSpots);
        }
        catch (Exception e)
        {
            if (session.Id > 0)
                await DeleteParkingSession(session.Id);
            int rolledBackReservedCount = Math.Max(0, parkingLot.Reserved - 1);
            var rollback = new PatchParkingLotDto { Reserved = rolledBackReservedCount };
            await _parkingLots.PatchParkingLotByIdAsync(parkingLot.Id, rollback);
            return new StartSessionResult.Error("Failed to start session: " + e.Message);
        }
    }

    public async Task<StartSessionResult> StartPaidSession(string licensePlate, long lotId, CreateCardInfoDto cardInfo)
    {
        bool moneyOnCard = cardInfo.AvailableFunds > 0;
        
        licensePlate = licensePlate.Upper();
        var lot = await _parkingLots.GetParkingLotByIdAsync(lotId);

        if (lot.Status is not ServiceStatus.Success)
            return new StartSessionResult.LotNotFound();

        ParkingLotModel parkingLot = new()
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
        
        var preAuth = await _preAuth.PreauthorizeAsync(cardInfo.Token, moneyOnCard);
        if (!preAuth.Approved)
            return new StartSessionResult.PreAuthFailed(preAuth.Reason ?? "Card declined");
        
        ParkingSessionModel session = new()
        {
            ParkingLotId = lotId,
            PaymentStatus = ParkingSessionStatus.PreAuthorized,
            LicensePlateNumber = licensePlate,
            Started = DateTimeOffset.UtcNow,
            Stopped = null,
        };
        
        try
        {
            var transaction = new TransactionModel()
            {
                Amount = 0, Method = cardInfo.Method, Token = cardInfo.Token, Bank = cardInfo.Bank
            };
            await _transactionRepo.SaveChangesAsync();

            var payment = new PaymentModel()
            {
                Amount = 0,
                LicensePlateNumber = licensePlate,
                CreatedAt = DateTimeOffset.UtcNow,
                TransactionId = transaction.Id
            };
            await _paymentRepo.SaveChangesAsync();

            session.PaymentId = payment.PaymentId;
            
            var persistResult = await PersistSession(session, parkingLot);
            if (persistResult is not PersistSessionResult.Success sPersist)
                throw new InvalidOperationException("Failed to persist session");

            session = sPersist.Session;
            await _sessions.SaveChangesAsync();
            if (!await OpenSessionGate(session, licensePlate))
                throw new InvalidOperationException("Failed to open gate");
            return new StartSessionResult.Success(session, parkingLot.AvailableSpots);
        }
        catch (Exception e)
        {
            if (session.Id > 0)
                await DeleteParkingSession(session.Id);
            await _sessions.SaveChangesAsync();
            int rolledBackReservedCount = Math.Max(0, parkingLot.Reserved - 1);
            var rollback = new PatchParkingLotDto { Reserved = rolledBackReservedCount };
            await _parkingLots.PatchParkingLotByIdAsync(parkingLot.Id, rollback);
            return new StartSessionResult.Error("Failed to start session: " + e.Message);
        }
    }
    

    // Old method, kept for reference
    public async Task<CreateCardInfoDto> GetCardFromTerminal(CreateParkingSessionDto dto)
    {
        await Task.Delay(500); // Simulate a delay for terminal interaction

        // For demonstration, return a dummy card info
        // Generate a random 8-character alphanumeric token. This is just for simulation,
        // In a real scenario, this would come from the payment terminal.
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var random = new Random();

        var token = new string(Enumerable.Repeat(chars, 8)
            .Select(s => s[random.Next(s.Length)]).ToArray());

        return new CreateCardInfoDto
        {
            Token = token,
            Method = "CreditCard",
            Bank = "DemoBank",
            AvailableFunds = 100m // Simulate sufficient funds
        };
    }

    public async Task<StopSessionResult> StopSession(long id, StopParkingSessionDto sessionDto)
    {
        var activeSessionResult = await GetParkingSessionById(id);
        if (activeSessionResult is not GetSessionResult.Success sActive)
            return new StopSessionResult.Error("Active session not found.");

        var activeSession = sActive.Session;

        if (activeSession.Stopped.HasValue)
            return new StopSessionResult.AlreadyStopped();

        var lotResult = await _parkingLots.GetParkingLotByIdAsync(activeSession.ParkingLotId);
        if (lotResult.Status != ServiceStatus.Success)
            return new StopSessionResult.Error("Failed to retrieve parking lot.");

        var parkingLot = new ParkingLotModel
        {
            Id = lotResult.Data!.Id,
            Name = lotResult.Data.Name,
            Location = lotResult.Data.Location,
            Address = lotResult.Data.Address,
            Capacity = lotResult.Data.Capacity,
            Tariff = lotResult.Data.Tariff,
            DayTariff = lotResult.Data.DayTariff
        };

        DateTimeOffset chargeFrom;
        DateTimeOffset end = DateTime.UtcNow;
        decimal totalAmount = 0m;
        bool paymentPerformed = false;
        CalculatePriceResult priceResult = new CalculatePriceResult.Error("Uninitialized");


        var anyHotelPasses = await _passService.GetHotelPassesByLicensePlateAndLotIdAsync(parkingLot.Id, activeSession.LicensePlateNumber);
        if (anyHotelPasses.Status == ServiceStatus.Success)
        {
            if (anyHotelPasses.Data is not null && anyHotelPasses.Data.Any())
            {
                var overlappingPass = anyHotelPasses.Data.FirstOrDefault(x =>
                    x.End + x.ExtraTime >= activeSession.Started && x.Start <= end);
                if (overlappingPass is not null)
                {
                    activeSession.HotelPassId = overlappingPass.Id;
                }
            }
        }
        if (activeSession.HotelPassId.HasValue)
        {
            var pass = await _passService.GetHotelPassByIdAsync(activeSession.HotelPassId.Value);
            if (pass.Status != ServiceStatus.Success)
                return new StopSessionResult.Error("Failed to retrieve hotel pass from database.");

            var passData = pass.Data!;
            var endOfFree = passData.End + passData.ExtraTime;

            decimal total = 0m;

            var beforeTo = end < passData.Start ? end : passData.Start;
            if (activeSession.Started < beforeTo)
            {
                var totalToPayBefore = _pricing.CalculateParkingCost(parkingLot, activeSession.Started, beforeTo);

                if (totalToPayBefore is CalculatePriceResult.Success priceBefore)
                {
                    total += priceBefore.Price;
                    priceResult = priceBefore;
                }
                else if (totalToPayBefore is CalculatePriceResult.Error error)
                    return new StopSessionResult.Error(error.Message);
            }

            if (end > endOfFree)
            {
                var chargeAfterPassFrom = activeSession.Started > endOfFree ? activeSession.Started : endOfFree;

                if (chargeAfterPassFrom < end)
                {
                    var totalToPayAfter = _pricing.CalculateParkingCost(parkingLot, chargeAfterPassFrom, end);

                    if (totalToPayAfter is CalculatePriceResult.Success priceAfter)
                    {
                        total += priceAfter.Price;
                        priceResult = priceAfter;
                    }
                    else if (totalToPayAfter is CalculatePriceResult.Error error)
                        return new StopSessionResult.Error(error.Message);
                }
            }

            totalAmount = total;
            if (priceResult is not CalculatePriceResult.Success)
            {
                priceResult = new CalculatePriceResult.Success(
                    Price: totalAmount,
                    BillableHours: 0,
                    BillableDays: 0);
            }
        }
        else if (activeSession.BusinessParkingRegistrationId.HasValue)
        {
            var registrationResult =
                await _registrationService.GetBusinessRegistrationByIdAsync(activeSession.BusinessParkingRegistrationId.Value);

            if (registrationResult.Status != ServiceStatus.Success)
                return new StopSessionResult.Error("Failed to retrieve business registration.");

            if (registrationResult.Data is null)
                throw new InvalidOperationException("ServiceResult data is null.");

            chargeFrom = activeSession.Started;

            priceResult = _pricing.CalculateParkingCost(parkingLot, chargeFrom, end);
            if (priceResult is not CalculatePriceResult.Success sPrice)
                return new StopSessionResult.Error("Failed to calculate parking cost.");

            totalAmount = sPrice.Price;

            activeSession.PaymentStatus = ParkingSessionStatus.PendingInvoice;
        }
        else
        {
            chargeFrom = activeSession.Started;

            priceResult = _pricing.CalculateParkingCost(parkingLot, chargeFrom, end);
            if (priceResult is not CalculatePriceResult.Success sPrice)
                return new StopSessionResult.Error("Failed to calculate parking cost.");

            totalAmount = sPrice.Price;
        }

        if (totalAmount > 0m)
        {
            if (!activeSession.BusinessParkingRegistrationId.HasValue && !activeSession.HotelPassId.HasValue)
            {
                // var paymentResult = await _preAuth.PreauthorizeAsync(sessionDto.CardToken, totalAmount);
                // if (!paymentResult.Approved)
                //     return new StopSessionResult.PaymentFailed(paymentResult.Reason ?? "Payment declined");
                if (activeSession.PaymentId is not null)
                {
                    var getPayment = await _payments.GetPaymentByIdAsync(activeSession.PaymentId.Value);
                    if (getPayment is GetPaymentResult.Success pay)
                    {
                        var payment = pay.Payment;
                        payment.Amount = totalAmount;
                        var getTransaction = await _transactions.GetTransactionById(payment.TransactionId);
                        if (getTransaction is GetTransactionResult.Success transact)
                        {
                            var transaction = transact.Transaction;
                            transaction.Amount = totalAmount;
                            payment.CompletedAt = DateTimeOffset.UtcNow;
                            activeSession.PaymentStatus = ParkingSessionStatus.Paid;
                            _paymentRepo.Update(payment);
                            await _paymentRepo.SaveChangesAsync();
                            _transactionRepo.Update(transaction);
                            await _transactionRepo.SaveChangesAsync();
                        }
                        else return new StopSessionResult.Error("Couldn't get transaction");
                    }
                    else return new StopSessionResult.Error("Payment not found");
                }
                else return new StopSessionResult.Error("No paymentId");
            }
        }
        else
        {
            if (activeSession.HotelPassId.HasValue)
                activeSession.PaymentStatus = ParkingSessionStatus.HotelPass;
            else if (activeSession.BusinessParkingRegistrationId.HasValue)
                activeSession.PaymentStatus = ParkingSessionStatus.PendingInvoice;
            else
                activeSession.PaymentStatus = ParkingSessionStatus.Paid;
        }

        activeSession.Stopped = end;
        activeSession.Cost = totalAmount;

        var invoiceStatus = activeSession.PaymentStatus switch
        {
            ParkingSessionStatus.Paid => InvoiceStatus.Paid,
            ParkingSessionStatus.HotelPass => InvoiceStatus.Paid,
            ParkingSessionStatus.PendingInvoice => InvoiceStatus.Pending,
            _ => InvoiceStatus.Pending
        };

        if (priceResult is not CalculatePriceResult.Success sPriceResult)
            return new StopSessionResult.Error("Failed to calculate parking cost for invoice generation.");

        int duration = 0;

        // For hotel pass or business parking sessions, only count billable time
        if (activeSession.HotelPassId.HasValue || activeSession.BusinessParkingRegistrationId.HasValue)
        {
            // If no cost, the entire session was covered by the pass
            if (totalAmount == 0m)
            {
                duration = 0;
            }
            else
            {
                duration = sPriceResult.BillableDays > 0 ? sPriceResult.BillableDays : sPriceResult.BillableHours;
            }
        }
        else
        {
            duration = sPriceResult.BillableDays > 0 ? sPriceResult.BillableDays : sPriceResult.BillableHours;
        }

        var createInvoiceDto = new CreateInvoiceDto
        {
            LicensePlateId = activeSession.LicensePlateNumber,
            ParkingSessionId = activeSession.Id,
            SessionDuration = duration,
            Cost = activeSession.Cost.Value,
            Status = invoiceStatus
        };

        try
        { 
            _sessions.Update(activeSession);
            await _sessions.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            return new StopSessionResult.Error("Failed to update parking session: " + ex.Message);
        }


        try
        {
            if (!await OpenSessionGate(activeSession, activeSession.LicensePlateNumber))
                throw new Exception("Failed to open gate");
        }
        catch (Exception ex)
        {
            if (paymentPerformed)
            {
                // Rollback als betaling gelukt is
                activeSession.Stopped = null;
                activeSession.Cost = null;
                activeSession.PaymentStatus = ParkingSessionStatus.PreAuthorized;

                var rollbackDto = new UpdateParkingSessionDto
                {
                    Stopped = null,
                    Cost = null,
                    PaymentStatus = ParkingSessionStatus.PreAuthorized
                };

                await _sessions.Update(activeSession, rollbackDto);
                await _sessions.SaveChangesAsync();

                return new StopSessionResult.Error($"Payment successful but gate error: {ex.Message}");
            }

            return new StopSessionResult.Error($"Gate error: {ex.Message}");
        }

        var invoiceCreateResult = await _invoiceService.CreateInvoice(createInvoiceDto);
        InvoiceModel invoice;

        if (invoiceCreateResult is CreateInvoiceResult.Success sInv)
        {
            invoice = sInv.Invoice;
        }
        else
        {
            var existingInvoice = await _invoiceRepository.GetInvoiceModelByLicensePlate(activeSession.LicensePlateNumber);
            if (existingInvoice is not null)
            {
                invoice = new InvoiceModel
                {
                    Id = existingInvoice.Id,
                    LicensePlateId = existingInvoice.LicensePlateId,
                    ParkingSessionId = existingInvoice.ParkingSessionId,
                    SessionDuration = duration,
                    CreatedAt = existingInvoice.CreatedAt,
                    Status = existingInvoice.Status,
                    Cost = existingInvoice.Cost,
                    InvoiceSummary = existingInvoice.InvoiceSummary
                };
            }
            else
            {
                invoice = new InvoiceModel
                {
                    Id = 0,
                    LicensePlateId = activeSession.LicensePlateNumber,
                    ParkingSessionId = activeSession.Id,
                    SessionDuration = duration,
                    CreatedAt = DateTimeOffset.UtcNow,
                    Status = InvoiceStatus.Paid,
                    Cost = activeSession.Cost,
                    InvoiceSummary = new List<string> { "Invoice generation failed." }
                };
            }


        }
        return new StopSessionResult.Success(activeSession, totalAmount, invoice);
    }

    private async Task<Dictionary<string, DateTimeOffset>> GetPlateOwnershipMapAsync(long userId)
    {
        var userPlatesResult = await _userPlates.GetUserPlatesByUserId(userId);

        if (userPlatesResult is GetUserPlateListResult.Success s)
        {
            return s.Plates.ToDictionary(
                uPlate => uPlate.LicensePlateNumber,
                uPlate => uPlate.CreatedAt
            );
        }

        return new Dictionary<string, DateTimeOffset>();
    }
}