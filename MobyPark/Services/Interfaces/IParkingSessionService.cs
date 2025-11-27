using MobyPark.DTOs.ParkingSession.Request;
using MobyPark.Models;
using MobyPark.Services.Results.ParkingSession;

namespace MobyPark.Services.Interfaces;

public interface IParkingSessionService
{
    Task<CreateSessionResult> CreateParkingSession(CreateParkingSessionDto dto);
    Task<GetSessionResult> GetParkingSessionById(long id);
    Task<GetSessionListResult> GetParkingSessionsByParkingLotId(long lotId);
    Task<GetSessionListResult> GetParkingSessionsByLicensePlate(string licensePlate);
    Task<GetSessionListResult> GetParkingSessionsByPaymentStatus(string status);
    Task<GetSessionListResult> GetActiveParkingSessions();
    Task<GetSessionResult> GetActiveParkingSessionByLicensePlate(string licensePlate);
    Task<GetSessionListResult> GetAllRecentParkingSessionsByLicensePlate(string licensePlate, TimeSpan recentDuration);
    Task<GetSessionListResult> GetAllParkingSessions();
    Task<int> CountParkingSessions();
    Task<UpdateSessionResult> UpdateParkingSession(long id, UpdateParkingSessionDto dto);
    Task<DeleteSessionResult> DeleteParkingSession(long id);
    // (decimal Price, int Hours, int Days) CalculatePrice(ParkingLotModel parkingLot, ParkingSessionModel session);
    string GeneratePaymentHash(string sessionId, string licensePlate);
    string GenerateTransactionValidationHash();
    Task<StartSessionResult> StartSession(CreateParkingSessionDto sessionDto, string cardToken, decimal estimatedAmount, string? username, bool simulateInsufficientFunds = false);
    Task<StopSessionResult> StopSession(StopParkingSessionDto dto);
    Task<List<ParkingSessionModel>> GetAuthorizedSessionsAsync(long userId, int lotId, bool canManageSessions);
    Task<GetSessionResult> GetAuthorizedSessionAsync(long userId, int lotId, int sessionId, bool canManageSessions);
}
