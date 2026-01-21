using Microsoft.EntityFrameworkCore;

using MobyPark.Models.DbContext;
using MobyPark.Models.Repositories.Interfaces;

namespace MobyPark.Models.Repositories;

public class PaymentRepository : Repository<PaymentModel>, IPaymentRepository
{
    public PaymentRepository(AppDbContext context) : base(context) { }

    public async Task<(bool success, Guid id)> CreateWithId(PaymentModel entity)
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
            .Where(joined => joined.payment.CreatedAt >= joined.userPlate.CreatedAt)
            .Select(joined => joined.payment)
            .Include(payment => payment.LicensePlate)
            .Include(payment => payment.Transaction)
            .ToListAsync();

        return payments;
    }

    public async Task<PaymentModel?> GetByPaymentIdAdmin(Guid paymentId)
    {
        return await DbSet
            .Include(payment => payment.LicensePlate)
            .Include(payment => payment.Transaction)
            .FirstOrDefaultAsync(payment => payment.PaymentId == paymentId);
    }

    public async Task<PaymentModel?> GetByPaymentId(Guid paymentId, long requestingUserId)
    {
        var payment = await DbSet
            .Include(payment => payment.LicensePlate)
            .FirstOrDefaultAsync(payment => payment.PaymentId == paymentId);

        if (payment?.LicensePlate is null) return null;

        bool isAuthorized = await (
          from uPlate in Context.UserPlates
          where uPlate.UserId == requestingUserId
                && uPlate.LicensePlateNumber == payment.LicensePlateNumber
              && uPlate.CreatedAt <= payment.CreatedAt
          select uPlate).AnyAsync();

        return isAuthorized ? payment : null;
    }

    public async Task<PaymentModel?> GetByTransactionId(Guid transactionId, long requestingUserId)
    {
        var payment = await DbSet
            .Include(payment => payment.LicensePlate)
            .Include(payment => payment.Transaction)
            .FirstOrDefaultAsync(payment => payment.TransactionId == transactionId);

        if (payment?.LicensePlate is null) return null;

        bool isAuthorized = await
            Context.UserPlates.AnyAsync(uPlate =>
                uPlate.UserId == requestingUserId
                && uPlate.LicensePlateNumber == payment.LicensePlateNumber
            && uPlate.CreatedAt <= payment.CreatedAt);

        return isAuthorized ? payment : null;
    }

    public async Task<List<PaymentModel>> GetByLicensePlate(string licensePlateNumber, long requestingUserId)
    {
        var paymentsQuery = DbSet
            .Where(payment => payment.LicensePlateNumber == licensePlateNumber)
            .Include(payment => payment.LicensePlate)
            .Include(payment => payment.Transaction);

        var userPlateOwnershipQuery = Context.UserPlates
            .Where(uPlate => uPlate.UserId == requestingUserId && uPlate.LicensePlateNumber == licensePlateNumber);

        var authorizedPayments = await paymentsQuery
            .Join(
                userPlateOwnershipQuery,
                payment => payment.LicensePlateNumber,
                userPlate => userPlate.LicensePlateNumber,
                (payment, userPlate) => new { payment, userPlate }
            )
            .Where(joined => joined.payment.CreatedAt >= joined.userPlate.CreatedAt)
            .Select(joined => joined.payment)
            .ToListAsync();

        return authorizedPayments;
    }

    public async Task<bool> DeletePayment(Guid paymentId, long requestingUserId)
    {
        var payment = await GetByPaymentId(paymentId, requestingUserId);

        if (payment is null) return false;

        var transaction = await Context.Transactions.FindAsync(payment.TransactionId);

        if (transaction != null)
            Context.Transactions.Remove(transaction);
        DbSet.Remove(payment);

        await Context.SaveChangesAsync();
        return true;
    }


}