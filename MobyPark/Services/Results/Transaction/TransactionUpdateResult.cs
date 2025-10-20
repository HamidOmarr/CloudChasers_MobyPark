using MobyPark.Models;

namespace MobyPark.Services.Results.Transaction;

public abstract record TransactionUpdateResult
{
    public sealed record Success(TransactionModel Transaction) : TransactionUpdateResult;
    public sealed record NotFound() : TransactionUpdateResult;
    public sealed record Error(string Message) : TransactionUpdateResult;
}
