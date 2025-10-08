using MobyPark.Models;
using MobyPark.Models.DataService;

namespace MobyPark.Services;

public class PaymentService
{
    private readonly IDataAccess _dataAccess;

    public PaymentService(IDataAccess dataAccess)
    {
        _dataAccess = dataAccess;
    }

    public async Task<PaymentModel> CreatePayment(PaymentModel payment)
    {
        if (string.IsNullOrWhiteSpace(payment.TransactionId) ||
            string.IsNullOrWhiteSpace(payment.Initiator) ||
            payment.Amount == 0 ||
            payment.TransactionData == null)
            throw new ArgumentException("Required fields not filled!");

        await _dataAccess.Payments.Create(payment);
        return payment;
    }

    public async Task<PaymentModel?> GetPaymentByTransactionId(string id) => await _dataAccess.Payments.GetByTransactionId(id);

    public Task<List<PaymentModel>> GetPaymentsByUser(string username) => _dataAccess.Payments.GetByUser(username);

    public async Task<List<PaymentModel>> GetAllPayments() => await _dataAccess.Payments.GetAll();

    public async Task<int> CountPayments() => await _dataAccess.Payments.Count();

    private async Task<bool> UpdatePayment(PaymentModel payment)
    {
        var existingPayment = await GetPaymentByTransactionId(payment.TransactionId);
        if (existingPayment is null) throw new KeyNotFoundException("Payment not found");

        bool success = await _dataAccess.Payments.Update(payment);
        return success;
    }

    public async Task<bool> DeletePayment(string transactionId)
    {
        var payment = await GetPaymentByTransactionId(transactionId);
        if (payment is null) throw new KeyNotFoundException("Payment not found");

        bool success = await _dataAccess.Payments.DeletePayment(transactionId);
        return success;
    }

    public async Task<PaymentModel> RefundPayment(string originalTransaction, decimal amount, string adminUser)
    {
        string refundTransaction = Guid.NewGuid().ToString("N");

        if (decimal.IsNegative(amount))
            throw new ArgumentException("Amount cannot be negative");

        // TODO: Add extra adminUser injection security

        TransactionDataModel transactionData = new()
        {
            Amount = -Math.Abs(amount),
            Date = DateOnly.FromDateTime(DateTime.UtcNow),
            Method = "Refund",
            Issuer = adminUser,
            Bank = "N/A"
        };

        PaymentModel refundPayment = new()
        {
            TransactionId = refundTransaction,
            Amount = -Math.Abs(amount),
            Initiator = adminUser,
            CreatedAt = DateTime.UtcNow,
            Completed = null,
            Hash = Guid.NewGuid().ToString("N"),
            TransactionData = transactionData,
            CoupledTo = originalTransaction
        };

        if (string.IsNullOrWhiteSpace(originalTransaction) || string.IsNullOrWhiteSpace(adminUser))
            throw new ArgumentException("Required fields not filled!");

        await _dataAccess.Payments.Create(refundPayment);
        return refundPayment;
    }

    public async Task<PaymentModel> ValidatePayment(string transactionId, string validationHash, TransactionDataModel transactionData)
    {
        PaymentModel? payment = await _dataAccess.Payments.GetByTransactionId(transactionId);

        if (payment == null)
            throw new KeyNotFoundException("Payment not found");

        if (payment.Hash != validationHash)
            throw new UnauthorizedAccessException("Validation hash does not match the existing payment");

        payment.TransactionData = transactionData;
        if (payment.Completed.HasValue) return payment;
        payment.Completed = DateTime.UtcNow;
        await UpdatePayment(payment);

        return payment;
    }

    public async Task<decimal> GetTotalAmountForTransaction(string transaction)
    {
        var payment = await _dataAccess.Payments.GetByTransactionId(transaction);
        if (payment is null)
            return Decimal.MinValue; // TODO: Make a bit clearer, either here or where called.
        return payment.Amount;
    }
}
