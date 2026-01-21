using MobyPark.DTOs.PreAuth.Response;
using MobyPark.Services.Interfaces;

namespace MobyPark.Services;

public class PreAuthService : IPreAuthService
{
    // Placeholder: approve unless amount <= 0
    public PreAuthDto PreauthorizeAsync(string cardToken, bool isSufficientFunds)
    {
        if (!isSufficientFunds)
            return new PreAuthDto { Approved = false, Reason = "Insufficient funds" };

        return
    }
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