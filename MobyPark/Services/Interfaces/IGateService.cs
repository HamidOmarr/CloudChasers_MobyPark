namespace MobyPark.Services.Interfaces;

public interface IGateService
{
    Task<bool> OpenGateAsync(long parkingLotId, string licensePlate);
}