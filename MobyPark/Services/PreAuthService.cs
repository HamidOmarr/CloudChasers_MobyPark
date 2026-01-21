using MobyPark.DTOs.PreAuth.Response;
using MobyPark.Services.Interfaces;

namespace MobyPark.Services;

public class PreAuthService : IPreAuthService
{
    // Placeholder: approve unless amount <= 0
    public Task<PreAuthDto> PreauthorizeAsync(string cardToken, bool isSufficientFunds)
    {
        if (!isSufficientFunds)
            return Task.FromResult(new PreAuthDto { Approved = false, Reason = "Insufficient funds" });
        return Task.FromResult(new PreAuthDto { Approved = true, Reason = "Sufficient funds" });
    }
}