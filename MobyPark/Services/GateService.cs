using MobyPark.Services.Interfaces;

namespace MobyPark.Services;

public class GateService : IGateService
{
    public Task<bool> OpenGateAsync(long parkingLotId, string licensePlate)
    {
        return Task.FromResult(true);
    }
}
