using MobyPark.DTOs.PreAuth.Response;

namespace MobyPark.Services.Helpers;

// Placeholder. Not yet in DiContainer. Add if class receives dependencies (i.e. IRepositoryStack).
public static class PreAuth
{
    // Placeholder: approve unless amount <= 0
    public static Task<PreAuthDto> PreauthorizeAsync(string cardToken, decimal estimatedAmount, bool simulateInsufficientFunds = false)
    {
        if (simulateInsufficientFunds)
            return Task.FromResult(new PreAuthDto { Approved = false, Reason = "Insufficient funds" });

        return Task.FromResult(
            estimatedAmount <= 0
            ? new PreAuthDto { Approved = false, Reason = "Invalid amount" }
            : new PreAuthDto { Approved = true }
        );
    }
}
