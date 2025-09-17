using MobyPark.Services;

namespace MobyPark.Models.Access;

public interface IPaymentAccess : IRepository<PaymentModel>
{
    Task<List<PaymentModel>> GetByUser(string user);
    Task<PaymentModel?> GetByTransactionId(string transaction);
}
