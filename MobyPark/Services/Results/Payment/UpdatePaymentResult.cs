using MobyPark.Models;

namespace MobyPark.Services.Results.Payment;

public abstract record UpdatePaymentResult
{
    public sealed record Success(PaymentModel Payment) : UpdatePaymentResult;
    public sealed record AlreadyCompleted : UpdatePaymentResult;
    public sealed record NotFound : UpdatePaymentResult;
    public sealed record Error(string Message) : UpdatePaymentResult;
}