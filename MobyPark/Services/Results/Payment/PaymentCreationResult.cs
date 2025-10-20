using MobyPark.Models;

namespace MobyPark.Services.Results.Payment;

public abstract record PaymentCreationResult
{
    public sealed record Success(PaymentModel Payment) : PaymentCreationResult;
    public sealed record Error(string Message) : PaymentCreationResult;
}