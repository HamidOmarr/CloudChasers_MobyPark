namespace MobyPark.Services;

public class PaymentPreauthService
{
    public class PreauthResult
    {
        public bool Approved { get; init; }
        public string? Reason { get; init; }
    }

    // Placeholder: approve unless amount <= 0
    public virtual Task<PreauthResult> PreauthorizeAsync(string cardToken, decimal estimatedAmount, bool simulateInsufficientFunds = false)
    {
        if (simulateInsufficientFunds)
            return Task.FromResult(new PreauthResult { Approved = false, Reason = "Insufficient funds" });

        if (estimatedAmount <= 0)
            return Task.FromResult(new PreauthResult { Approved = false, Reason = "Invalid amount" });

        return Task.FromResult(new PreauthResult { Approved = true });
    }
}
