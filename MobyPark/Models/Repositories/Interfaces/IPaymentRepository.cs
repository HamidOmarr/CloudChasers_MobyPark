namespace MobyPark.Models.Repositories.Interfaces;

public interface IPaymentRepository : IRepository<PaymentModel>
{
    Task<(bool success, Guid id)> CreateWithId(PaymentModel entity);
    Task<List<PaymentModel>> GetByUserId(long userId);
    Task<PaymentModel?> GetByPaymentIdAdmin(Guid paymentId);
    Task<PaymentModel?> GetByPaymentId(Guid paymentId, long requestingUserId);
    Task<PaymentModel?> GetByTransactionId(Guid transactionId, long requestingUserId);
    Task<List<PaymentModel>> GetByLicensePlate(string licensePlateNumber, long requestingUserId);
    Task<bool> DeletePayment(Guid paymentId, long requestingUserId);
}