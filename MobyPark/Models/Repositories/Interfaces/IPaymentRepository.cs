namespace MobyPark.Models.Repositories.Interfaces;

public interface IPaymentRepository : IRepository<PaymentModel>
{
    new Task<(bool success, Guid id)> CreateWithId(PaymentModel entity);
    Task<List<PaymentModel>> GetByUserId(long userId);
    Task<PaymentModel?> GetByPaymentId(Guid paymentId);
    Task<PaymentModel?> GetByTransactionId(Guid transactionId);
    Task<List<PaymentModel>> GetByLicensePlate(string licensePlateNumber);
    Task<bool> DeletePayment(Guid paymentId);
}
