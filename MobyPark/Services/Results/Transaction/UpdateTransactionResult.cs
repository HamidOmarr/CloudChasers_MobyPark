using MobyPark.Models;

namespace MobyPark.Services.Results.Transaction;

public abstract record UpdateTransactionResult
{
    public sealed record Success(TransactionModel Transaction) : UpdateTransactionResult;
    public sealed record NoChangesMade : UpdateTransactionResult;
    public sealed record NotFound : UpdateTransactionResult;
    public sealed record Error(string Message) : UpdateTransactionResult;
}
