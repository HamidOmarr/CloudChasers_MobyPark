using MobyPark.Models;

namespace MobyPark.Services.Results.Transaction;

public abstract record TransactionCreationResult
{
    public sealed record Success(Guid Id, TransactionModel Transaction) : TransactionCreationResult;
    public sealed record Error(string Message) : TransactionCreationResult;
}
