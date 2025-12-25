using MobyPark.DTOs.Reservation.Request;
using MobyPark.Services.Results.Reservation;

namespace MobyPark.Services.Interfaces;

public interface IReservationService
{
    Task<CreateReservationResult> CreateReservation(CreateReservationDto dto, long requestingUserId, bool isAdminRequest = false);
    Task<GetReservationCostEstimateResult> GetReservationCostEstimate(ReservationCostEstimateRequest dto, long userId);
    Task<GetReservationResult> GetReservationById(long id, long requestingUserId);
    Task<GetReservationListResult> GetReservationsByParkingLotId(long parkingLotId, long requestingUserId);
    Task<GetReservationListResult> GetReservationsByLicensePlate(string licensePlate, long requestingUserId);
    Task<GetReservationListResult> GetReservationsByStatus(string status, long requestingUserId);
    Task<int> CountReservations();
    Task<GetReservationListResult> GetAllReservations();
    Task<UpdateReservationResult> UpdateReservation(long reservationId, long requestingUserId, UpdateReservationDto dto);
    Task<DeleteReservationResult> DeleteReservation(long id, long requestingUserId);
}
