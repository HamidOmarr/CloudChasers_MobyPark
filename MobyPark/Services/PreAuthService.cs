using MobyPark.DTOs.PreAuth.Response;
using MobyPark.Services.Interfaces;

namespace MobyPark.Services;

public class PreAuthService : IPreAuthService
{
    // Placeholder: approve unless amount <= 0
    public Task<PreAuthDto> PreauthorizeAsync(string cardToken, decimal estimatedAmount, bool simulateInsufficientFunds = false)
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