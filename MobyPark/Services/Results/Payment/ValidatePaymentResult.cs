using MobyPark.Models;

namespace MobyPark.Services.Results.Payment;

public abstract record ValidatePaymentResult
{
    public sealed record Success(PaymentModel Payment) : ValidatePaymentResult;
    public sealed record InvalidData(string Message) : ValidatePaymentResult;
    public sealed record NotFound : ValidatePaymentResult;
    public sealed record Error(string Message) : ValidatePaymentResult;
}