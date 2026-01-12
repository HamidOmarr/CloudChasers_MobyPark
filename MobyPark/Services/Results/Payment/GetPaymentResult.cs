using MobyPark.Models;

namespace MobyPark.Services.Results.Payment;

public abstract record GetPaymentResult
{
    public sealed record Success(PaymentModel Payment) : GetPaymentResult;
    public sealed record NotFound : GetPaymentResult;
    public sealed record InvalidInput(string Message) : GetPaymentResult; // For bad Guids
}