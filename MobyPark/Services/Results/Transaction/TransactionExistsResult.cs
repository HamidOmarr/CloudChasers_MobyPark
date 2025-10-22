namespace MobyPark.Services.Results.Transaction;

public abstract record TransactionExistsResult
{
    public sealed record Exists : TransactionExistsResult;
    public sealed record NotExists : TransactionExistsResult;
    public sealed record InvalidInput(string Message) : TransactionExistsResult;
    public sealed record Error(string Message) : TransactionExistsResult;
}