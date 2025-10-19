using MobyPark.Models;
using MobyPark.Models.Repositories.Interfaces;
using MobyPark.Models.Repositories.RepositoryStack;
using MobyPark.Services.Services;

namespace MobyPark.Services;

public class TransactionService
{
    private readonly ITransactionRepository _transactions;

    public TransactionService(IRepositoryStack repoStack)
    {
        _transactions = repoStack.Transactions;
    }

    public async Task<TransactionModel> CreateTransaction(TransactionModel transaction)
    {
        Validator.Transaction(transaction);

        (bool createdSuccessfully, Guid id) = await _transactions.CreateWithId(transaction);
        if (createdSuccessfully) transaction.Id = id;
        return transaction;
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

    public async Task<bool> UpdateTransaction(TransactionModel transaction)
    {
        Validator.Transaction(transaction);
        return await _transactions.Update(transaction);
    }

    public async Task<bool> DeleteTransaction(Guid transactionId)
    {
        var transactionExists = await TransactionExists("id", transactionId.ToString());
        if (!transactionExists) throw new KeyNotFoundException("Transaction not found");  // Check existence. Make custom return type.

        var transaction = (await GetTransactionById(transactionId))!;

        return await _transactions.DeleteTransaction(transaction);
    }
}
