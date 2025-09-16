using MobyPark.Models;
using MobyPark.Models.DataService;

namespace MobyPark.Services;

public class PaymentService
{
    private readonly IDataService _dataService;

    public PaymentService(IDataService dataService)
    {
        _dataService = dataService;
    }

    public async Task<PaymentModel> CreatePayment(string transaction, decimal amount, string initiator,
                                                       TransactionDataModel transactionData)
    {
        PaymentModel payment = new()
        {
            TransactionId = transaction,
            Amount = amount,
            Initiator = initiator,
            CreatedAt = DateTime.UtcNow,
            Completed = default,
            Hash = Guid.NewGuid().ToString("N"),
            TransactionData = transactionData
        };

        if (string.IsNullOrWhiteSpace(transaction) || string.IsNullOrWhiteSpace(initiator) ||
            transaction is null)
            throw new ArgumentException("Required fields not filled!");

        await _dataService.Payments.Create(payment);
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
            Date = DateTime.UtcNow,
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
            Completed = default,
            Hash = Guid.NewGuid().ToString("N"),
            TransactionData = transactionData,
            CoupledTo = originalTransaction
        };

        if (string.IsNullOrWhiteSpace(originalTransaction) || string.IsNullOrWhiteSpace(adminUser))
            throw new ArgumentException("Required fields not filled!");

        await _dataService.Payments.Create(refundPayment);
        return refundPayment;
    }

    public async Task<PaymentModel> ValidatePayment(string transactionId, string validationHash, TransactionDataModel transactionData)
    {
        PaymentModel? payment = await _dataService.Payments.GetByTransactionId(transactionId);

        if (payment == null)
            throw new KeyNotFoundException("Payment not found");

        string uuid = SystemService.GenerateGuid(validationHash).ToString("D");

        if (payment.Hash != uuid)
            throw new UnauthorizedAccessException("Validation hash does not match the existing payment");

        payment.TransactionData = transactionData;
        if (payment.Completed.HasValue) return payment;
        payment.Completed = DateTime.UtcNow;
        await _dataService.Payments.Update(payment);

        return payment;
    }


    public Task<List<PaymentModel>> GetPaymentsForUser(string username) => _dataService.Payments.GetByUser(username);

    public async Task<decimal> GetTotalAmountForTransaction(string transaction)
    {
        var payment = await _dataService.Payments.GetByTransactionId(transaction);
        if (payment is null)
            return Decimal.MinValue; // TODO: Make a bit clearer, either here or where called.
        return payment.Amount;
    }
}

