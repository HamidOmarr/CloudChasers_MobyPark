namespace MobyPark.Services.Results.Transaction;

public abstract record DeleteTransactionResult
{
    public sealed record Success() : DeleteTransactionResult;
    public sealed record NotFound() : DeleteTransactionResult;
    public sealed record Error(string Message) : DeleteTransactionResult;
}
