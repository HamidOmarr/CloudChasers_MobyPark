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

        await _dataAccess.Payments.Create(payment, false);
        return payment;
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

        string uuid = SystemService.GenerateGuid(validationHash).ToString("D");

        if (payment.Hash != uuid)
            throw new UnauthorizedAccessException("Validation hash does not match the existing payment");

        payment.TransactionData = transactionData;
        if (payment.Completed.HasValue) return payment;
        payment.Completed = DateTime.UtcNow;
        await _dataAccess.Payments.Update(payment);

        return payment;
    }

    public Task<List<PaymentModel>> GetPaymentsForUser(string username) => _dataAccess.Payments.GetByUser(username);

    public async Task<PaymentModel> GetPaymentByTransactionId(string id)
    {
        PaymentModel? payment = await _dataAccess.Payments.GetByTransactionId(id);
        if (payment is null) throw new KeyNotFoundException("Payment not found");
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

