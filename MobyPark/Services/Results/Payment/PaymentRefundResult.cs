using MobyPark.Models;

namespace MobyPark.Services.Results.Payment;

public abstract record PaymentRefundResult
{
    public sealed record Success(PaymentModel RefundPayment) : PaymentRefundResult;
    public sealed record InvalidInput(string Message) : PaymentRefundResult;
    public sealed record NotFound() : PaymentRefundResult;
    public sealed record Error(string Message) : PaymentRefundResult;
}
