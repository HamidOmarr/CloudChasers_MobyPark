using MobyPark.Models;

namespace MobyPark.Services.Results.Transaction;

public abstract record CreateTransactionResult
{
    public sealed record Success(Guid Id, TransactionModel Transaction) : CreateTransactionResult;
    public sealed record Error(string Message) : CreateTransactionResult;
}