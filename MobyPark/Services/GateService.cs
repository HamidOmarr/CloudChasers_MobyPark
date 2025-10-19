namespace MobyPark.Services;

public static class GateService
{
    // Placeholder. Not yet in DiContainer. Add if class receives dependencies (i.e. IRepositoryStack).
    public static Task<bool> OpenGateAsync(long parkingLotId, string licensePlate)
    {
        return Task.FromResult(true);
    }
}
