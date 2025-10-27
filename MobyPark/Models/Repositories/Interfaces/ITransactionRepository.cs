namespace MobyPark.Models.Repositories.Interfaces;

public interface ITransactionRepository : IRepository<TransactionModel>
{
    new Task<(bool success, Guid id)> CreateWithId(TransactionModel entity);
    Task<TransactionModel?> GetByPaymentId(Guid paymentId);
    Task<TransactionModel?> GetByTransactionId(Guid transactionId);
    Task<bool> DeleteTransaction(TransactionModel transaction);
}
