using MobyPark.Models;

namespace MobyPark.Services.Results.Transaction;

public abstract record GetTransactionListResult
{
    public sealed record Success(List<TransactionModel> Transactions) : GetTransactionListResult;
    public sealed record NotFound : GetTransactionListResult;
}