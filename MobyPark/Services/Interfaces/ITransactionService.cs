using MobyPark.Models;
using MobyPark.Services.Results.Transaction;

namespace MobyPark.Services.Interfaces;

public interface ITransactionService
{
    Task<TransactionModel> CreateTransaction(TransactionModel transaction);
    Task<CreateTransactionResult> CreateTransactionConfirmation(TransactionModel transaction);
    Task<TransactionModel?> GetTransactionById(Guid id);
    Task<TransactionModel?> GetTransactionByPaymentId(Guid paymentId);
    Task<List<TransactionModel>> GetAllTransactions();
    Task<bool> TransactionExists(string checkBy, string filterValue);
    Task<int> CountTransactions();
    Task<UpdateTransactionResult> UpdateTransaction(TransactionModel transaction);
    Task<DeleteTransactionResult> DeleteTransaction(Guid transactionId);
}
