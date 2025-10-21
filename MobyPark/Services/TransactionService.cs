using MobyPark.Models;
using MobyPark.Models.Repositories.Interfaces;
using MobyPark.Services.Interfaces;
using MobyPark.Services.Results.Transaction;

namespace MobyPark.Services;

public class TransactionService : ITransactionService
{
    private readonly ITransactionRepository _transactions;

    public TransactionService(ITransactionRepository transactions)
    {
        _transactions = transactions;
    }

    public async Task<TransactionModel> CreateTransaction(TransactionModel transaction)
    {
        (bool createdSuccessfully, Guid id) = await _transactions.CreateWithId(transaction);
        if (createdSuccessfully) transaction.Id = id;
        return transaction;
    }

    public async Task<CreateTransactionResult> CreateTransactionConfirmation(TransactionModel transaction)
    {
        try
        {
            (bool success, Guid id) = await _transactions.CreateWithId(transaction);
            if (!success)
                return new CreateTransactionResult.Error("Database insertion failed.");

            transaction.Id = id;
            return new CreateTransactionResult.Success(id, transaction);
        }
        catch (Exception )
        { return new CreateTransactionResult.Error("An error occurred while creating the transaction."); }
    }

    public async Task<TransactionModel?> GetTransactionById(Guid id) => await _transactions.GetByTransactionId(id);

    public async Task<TransactionModel?> GetTransactionByPaymentId(Guid paymentId) => await _transactions.GetByPaymentId(paymentId);

    public async Task<List<TransactionModel>> GetAllTransactions() => await _transactions.GetAll();

    public async Task<bool> TransactionExists(string checkBy, string filterValue)
    {
        if (string.IsNullOrWhiteSpace(filterValue))
            throw new ArgumentException("Filter value cannot be empty or whitespace.", nameof(filterValue));

        bool exists = checkBy.ToLower() switch
        {
            "id" => Guid.TryParse(filterValue, out Guid id) && await _transactions.Exists(transaction => transaction.Id == id),
            _ => throw new ArgumentException("Invalid checkBy parameter. Must be 'id'.",
                nameof(checkBy))
        };

        return exists;
    }

    public async Task<int> CountTransactions() => await _transactions.Count();

    public async Task<UpdateTransactionResult> UpdateTransaction(TransactionModel transaction)
    {
        try
        {
            var exists = await _transactions.GetByTransactionId(transaction.Id);
            if (exists is null)
                return new UpdateTransactionResult.NotFound();

            if (!await _transactions.Update(transaction))
                return new UpdateTransactionResult.Error("Database update failed.");
            return new UpdateTransactionResult.Success(transaction);
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
