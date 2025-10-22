using MobyPark.Models;

namespace MobyPark.Services.Results.Transaction;

public abstract record GetTransactionResult
{
    public sealed record Success(TransactionModel Transaction) : GetTransactionResult;
    public sealed record NotFound : GetTransactionResult;
    public sealed record InvalidInput(string Message) : GetTransactionResult;
    public sealed record Error(string Message) : GetTransactionResult;
}
