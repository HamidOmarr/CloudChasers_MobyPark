using MobyPark.Models;

namespace MobyPark.Services.Results.Payment;

public abstract record CreatePaymentResult
{
    public sealed record Success(PaymentModel Payment) : CreatePaymentResult;
    public sealed record Error(string Message) : CreatePaymentResult;
}