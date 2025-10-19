using MobyPark.DTOs.ParkingSession.Request;

namespace MobyPark.Validation;

public static class DtoValidator
{
    public static void ParkingSessionCreate(ParkingSessionCreateDto session)
    {
        ValHelper.ThrowIfNotPositive(session.ParkingLotId, nameof(session.ParkingLotId));
        ValHelper.ThrowIfNullOrWhiteSpace(session.LicensePlate, nameof(session.LicensePlate));
    }
}