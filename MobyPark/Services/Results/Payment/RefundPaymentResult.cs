using MobyPark.Models;

namespace MobyPark.Services.Results.Payment;

public abstract record RefundPaymentResult
{
    public sealed record Success(PaymentModel RefundPayment) : RefundPaymentResult;
    public sealed record InvalidInput(string Message) : RefundPaymentResult;
    public sealed record NotFound() : RefundPaymentResult;
    public sealed record Error(string Message) : RefundPaymentResult;
}
