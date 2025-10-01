namespace MobyPark.Services;

public class GateService
{
    // placeholder
    public virtual Task<bool> OpenGateAsync(int parkingLotId, string licensePlate)
    {
        return Task.FromResult(true);
    }
}
