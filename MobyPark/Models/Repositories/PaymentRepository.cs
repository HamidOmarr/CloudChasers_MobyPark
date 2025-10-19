using Microsoft.EntityFrameworkCore;
using MobyPark.Models.DbContext;
using MobyPark.Models.Repositories.Interfaces;

namespace MobyPark.Models.Repositories;

public class PaymentRepository : Repository<PaymentModel>, IPaymentRepository
{
    public PaymentRepository(AppDbContext context) : base(context) { }

    public new async Task<(bool success, Guid id)> CreateWithId(PaymentModel entity)
    {
        await DbSet.AddAsync(entity);
        await Context.SaveChangesAsync();

        return (true, entity.PaymentId);
    }

    public async Task<List<PaymentModel>> GetByUserId(long userId)
    {
        var payments = await Context.Payments
            .Join(
                Context.UserPlates.Where(userPlate => userPlate.UserId == userId),
                payment => payment.LicensePlateNumber,
                userPlate => userPlate.LicensePlateNumber,
                (payment, userPlate) => new { payment, userPlate }
            )
            .Where(joined => joined.payment.CreatedAt >= joined.userPlate.CreatedAt.ToDateTime(TimeOnly.MinValue))
            .Select(joined => joined.payment)
            .ToListAsync();

        return payments;
    }

    public async Task<PaymentModel?> GetByPaymentId(Guid paymentId) =>
        await DbSet.FirstOrDefaultAsync(payment => payment.PaymentId == paymentId);

    public async Task<PaymentModel?> GetByTransactionId(Guid transactionId) =>
        await DbSet.FirstOrDefaultAsync(payment => payment.TransactionDataId == transactionId);

    public async Task<List<PaymentModel>> GetByLicensePlate(string licensePlateNumber) =>
        await DbSet.Where(payment => payment.LicensePlateNumber == licensePlateNumber).ToListAsync();

    public async Task<bool> DeletePayment(Guid paymentId)
    {
        var payment = await GetByPaymentId(paymentId);
        if (payment is null) return false;

        var transaction = await Context.Transactions.FindAsync(payment.TransactionDataId);

        if (transaction is not null)
            Context.Transactions.Remove(transaction);
        DbSet.Remove(payment);

        await Context.SaveChangesAsync();
        return true;
    }
}
