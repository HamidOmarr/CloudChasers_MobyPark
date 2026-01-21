using MobyPark.DTOs.Cards;
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
    string GeneratePaymentHash(string sessionId, string licensePlate);
    string GenerateTransactionValidationHash();
    Task<StartSessionResult> StartSession(CreateParkingSessionDto sessionDto);
    Task<StartSessionResult> StartPaidSession(string licensePlate, long lotId, CreateCardInfoDto cardInfo);
    Task<StopSessionResult> StopSession(long id);
    Task<List<ParkingSessionModel>> GetAuthorizedSessionsAsync(long userId, long lotId, bool canManageSessions);
    Task<GetSessionResult> GetAuthorizedSessionAsync(long userId, long lotId, long sessionId, bool canManageSessions);
}