namespace MobyPark.Services.Results.Payment;

public abstract record DeletePaymentResult
{
    public sealed record Success() : DeletePaymentResult;
    public sealed record NotFound() : DeletePaymentResult;
    public sealed record Error(string Message) : DeletePaymentResult;
}
