using MobyPark.Services.Interfaces;

namespace MobyPark.Services;

public class 
    GateService : IGateService
{
    // Placeholder. When integrated with actual gate hardware, implement here and add tests.
    public Task<bool> OpenGateAsync(long parkingLotId, string licensePlate)
    {
        return Task.FromResult(true);
    }
}
