using MobyPark.Models;

namespace MobyPark.Services.Results.Payment;

public abstract record GetPaymentListResult
{
    public sealed record Success(List<PaymentModel> Payments) : GetPaymentListResult;
    public sealed record NotFound : GetPaymentListResult;
}