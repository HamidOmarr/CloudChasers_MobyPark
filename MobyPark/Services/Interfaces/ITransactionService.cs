using MobyPark.DTOs.Transaction.Request;
using MobyPark.Models;
using MobyPark.Services.Results.Transaction;

namespace MobyPark.Services.Interfaces;

public interface ITransactionService
{
    Task<CreateTransactionResult> CreateTransaction(TransactionModel transaction);
    Task<GetTransactionResult> GetTransactionById(Guid id);
    Task<GetTransactionResult> GetTransactionByPaymentId(Guid paymentId);
    Task<GetTransactionListResult> GetAllTransactions();
    Task<TransactionExistsResult> TransactionExists(string checkBy, string filterValue);
    Task<int> CountTransactions();
    Task<UpdateTransactionResult> UpdateTransaction(Guid transactionId, TransactionDataDto dto);
    Task<DeleteTransactionResult> DeleteTransaction(Guid transactionId);
}
