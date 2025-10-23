using MobyPark.DTOs.PreAuth.Response;

namespace MobyPark.Services.Interfaces;

public interface IPreAuthService
{
    public Task<PreAuthDto> PreauthorizeAsync(string cardToken, decimal estimatedAmount, bool simulateInsufficientFunds = false);
}
