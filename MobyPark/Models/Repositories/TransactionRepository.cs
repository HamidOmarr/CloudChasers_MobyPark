using Microsoft.EntityFrameworkCore;

using MobyPark.Models.DbContext;
using MobyPark.Models.Repositories.Interfaces;

namespace MobyPark.Models.Repositories;

public class TransactionRepository : Repository<TransactionModel>, ITransactionRepository
{
    public TransactionRepository(AppDbContext context) : base(context) { }

    public async Task<(bool success, Guid id)> CreateWithId(TransactionModel entity)
    {
        await DbSet.AddAsync(entity);
        await Context.SaveChangesAsync();

        return (true, entity.Id);
    }

    public async Task<TransactionModel?> GetByPaymentId(Guid paymentId)
    {
        return await Context.Payments
            .Where(payment => payment.PaymentId == paymentId)
            .Select(payment => payment.Transaction)
            .FirstOrDefaultAsync();
    }

    public async Task<TransactionModel?> GetByTransactionId(Guid transactionId) =>
        await DbSet.FirstOrDefaultAsync(transaction => transaction.Id == transactionId);

    public async Task<bool> DeleteTransaction(TransactionModel transaction)
    {
        var payment = await Context.Payments
            .FirstOrDefaultAsync(payment => payment.TransactionId == transaction.Id);

        if (payment is not null)
            Context.Payments.Remove(payment);
        DbSet.Remove(transaction);

        await Context.SaveChangesAsync();
        return true;
    }
}