using MobyPark.DTOs.Transaction.Request;
using MobyPark.Models;
using MobyPark.Models.Repositories.Interfaces;
using MobyPark.Services.Interfaces;
using MobyPark.Services.Results.Transaction;
using MobyPark.Validation;

namespace MobyPark.Services;

public class TransactionService : ITransactionService
{
    private readonly ITransactionRepository _transactions;

    public TransactionService(ITransactionRepository transactions)
    {
        _transactions = transactions;
    }

    public async Task<CreateTransactionResult> CreateTransaction(TransactionModel transaction)
    {
        try
        {
            (bool success, Guid id) = await _transactions.CreateWithId(transaction);
            if (!success)
                return new CreateTransactionResult.Error("Database insertion failed.");

            transaction.Id = id;
            return new CreateTransactionResult.Success(id, transaction);
        }
        catch (Exception)
        { return new CreateTransactionResult.Error("An error occurred while creating the transaction."); }
    }

    public async Task<GetTransactionResult> GetTransactionById(Guid id)
    {
        var transaction = await _transactions.GetByTransactionId(id);
        if (transaction is null)
            return new GetTransactionResult.NotFound();

        return new GetTransactionResult.Success(transaction);
    }

    public async Task<GetTransactionResult> GetTransactionByPaymentId(Guid paymentId)
    {
        var transaction = await _transactions.GetByPaymentId(paymentId);
        if (transaction is null)
            return new GetTransactionResult.NotFound();

        return new GetTransactionResult.Success(transaction);
    }

    public async Task<GetTransactionListResult> GetAllTransactions()
    {
        var transactions = await _transactions.GetAll();
        if (transactions.Count == 0)
            return new GetTransactionListResult.NotFound();
        return new GetTransactionListResult.Success(transactions);
    }

    public async Task<TransactionExistsResult> TransactionExists(string checkBy, string filterValue)
    {
        checkBy = checkBy.Lower();
        filterValue = filterValue.TrimSafe();

        if (string.IsNullOrEmpty(filterValue))
            return new TransactionExistsResult.InvalidInput("Filter value cannot be empty or whitespace.");

        Guid id = Guid.Empty;
        if (checkBy == "id" && !Guid.TryParse(filterValue, out id))
            return new TransactionExistsResult.InvalidInput("ID must be a valid GUID when checking by 'id'.");
        if (checkBy != "id")
            return new TransactionExistsResult.InvalidInput("Invalid checkBy parameter. Must be 'id'.");

        bool exists = checkBy switch
        {
            "id" => await _transactions.Exists(transaction => transaction.Id == id),
            _ => false
        };

        return exists ? new TransactionExistsResult.Exists() : new TransactionExistsResult.NotExists();
    }

    public async Task<int> CountTransactions() => await _transactions.Count();

    public async Task<UpdateTransactionResult> UpdateTransaction(Guid transactionId, TransactionDataDto dto)
    {
        var getResult = await GetTransactionById(transactionId);
        if (getResult is not GetTransactionResult.Success success)
        {
            return getResult switch
            {
                GetTransactionResult.NotFound => new UpdateTransactionResult.NotFound(),
                _ => new UpdateTransactionResult.Error("Failed to retrieve transaction for update.")
            };
        }

        var existingTransaction = success.Transaction;

        bool changed = dto.Method != existingTransaction.Method
                       || dto.Issuer != existingTransaction.Token
                       || dto.Bank != existingTransaction.Bank;

        if (!changed)
            return new UpdateTransactionResult.NoChangesMade();

        try
        {
            bool updated = await _transactions.Update(existingTransaction, dto);
            if (!updated)
                return new UpdateTransactionResult.Error("Database update failed or reported no changes.");
            return new UpdateTransactionResult.Success(existingTransaction);
        }
        catch (Exception ex)
        { return new UpdateTransactionResult.Error(ex.Message); }
    }

    public async Task<DeleteTransactionResult> DeleteTransaction(Guid transactionId)
    {
        try
        {
            var transaction = await _transactions.GetByTransactionId(transactionId);
            if (transaction is null)
                return new DeleteTransactionResult.NotFound();

            if (!await _transactions.DeleteTransaction(transaction))
                return new DeleteTransactionResult.Error("Failed to delete transaction from database.");
            return new DeleteTransactionResult.Success();
        }
        catch (Exception ex)
        { return new DeleteTransactionResult.Error(ex.Message); }
    }
}