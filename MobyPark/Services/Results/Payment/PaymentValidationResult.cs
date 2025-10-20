using MobyPark.Models;

namespace MobyPark.Services.Results.Payment;

public abstract record PaymentValidationResult
{
    public sealed record Success(PaymentModel Payment) : PaymentValidationResult;
    public sealed record InvalidData(string Message) : PaymentValidationResult;
    public sealed record NotFound() : PaymentValidationResult;
    public sealed record Error(string Message) : PaymentValidationResult;
}
