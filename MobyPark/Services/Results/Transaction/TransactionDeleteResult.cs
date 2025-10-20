namespace MobyPark.Services.Results.Transaction;

public abstract record TransactionDeleteResult
{
    public sealed record Success() : TransactionDeleteResult;
    public sealed record NotFound() : TransactionDeleteResult;
    public sealed record Error(string Message) : TransactionDeleteResult;
}
