using MobyPark.DTOs.Payment.Request;
using MobyPark.DTOs.Transaction.Request;
using MobyPark.Models;
using MobyPark.Models.Repositories.Interfaces;
using MobyPark.Services.Interfaces;
using MobyPark.Services.Results.Payment;
using MobyPark.Services.Results.Transaction;

namespace MobyPark.Services;

public class PaymentService
{
    private readonly IPaymentRepository _payments;
    private readonly ITransactionService _transactions;
    // TransactionService is better here than TransactionRepository to encapsulate transaction logic.
    // This is because payment operations always involve transactions, so using the service ensures consistency and proper handling.

    public PaymentService(IPaymentRepository payments, ITransactionService transactions)
    {
        _payments = payments;
        _transactions = transactions;
    }

    public async Task<PaymentCreationResult> CreatePaymentAndTransaction(PaymentCreateDto request)
    {
        var transaction = new TransactionModel
        {
            Id = Guid.NewGuid(),
            Amount = request.Amount
            // Rest are empty strings until payment processing is done
        };

        var transactionResult = await _transactions.CreateTransactionConfirmation(transaction);

        if (transactionResult is not TransactionCreationResult.Success success)
            return new PaymentCreationResult.Error("Failed to create transaction record.");

        var payment = new PaymentModel
        {
            PaymentId = Guid.NewGuid(),
            Amount = request.Amount,
            LicensePlateNumber = request.LicensePlateNumber,
            CreatedAt = DateTime.UtcNow,
            TransactionId = success.Id,
        };

        try
        {
            var (createdPaymentSuccessfully, paymentId) = await _payments.CreateWithId(payment);
            if (!createdPaymentSuccessfully)
            {
                await _transactions.DeleteTransaction(success.Id);
                return new PaymentCreationResult.Error("Failed to create payment record after transaction.");
            }

            payment.PaymentId = paymentId;
            payment.Transaction = success.Transaction;
            return new PaymentCreationResult.Success(payment);
        }
        catch (Exception)
        {
            await _transactions.DeleteTransaction(success.Id);
            return new PaymentCreationResult.Error("An error occurred while saving the payment.");
        }
    }

    public async Task<PaymentModel?> GetPaymentById(string paymentId)
    {
        var payment = await _payments.GetByPaymentId(Guid.Parse(paymentId));
        if (payment is null)
            throw new KeyNotFoundException("Payment not found");
        return payment;
    }

    public async Task<PaymentModel?> GetPaymentByTransactionId(string transactionId)
    {
        var payment = await _payments.GetByTransactionId(Guid.Parse(transactionId));
        if (payment is null)
            throw new KeyNotFoundException("Payment not found");
        return payment;

    }

    public async Task<List<PaymentModel>> GetPaymentsByLicensePlate(string licensePlate)
    {
        var payments = await _payments.GetByLicensePlate(licensePlate);
        if (payments.Count == 0)
            throw new KeyNotFoundException("No payments found for the given license plate");
        return payments;
    }

    public async Task<List<PaymentModel>> GetAllPayments() => await _payments.GetAll();

    public async Task<List<PaymentModel>> GetPaymentsByUser(long userId) => await _payments.GetByUserId(userId);

    public async Task<int> CountPayments() => await _payments.Count();

    private async Task<bool> UpdatePayment(PaymentModel payment)
    {
        var existingPayment = await GetPaymentById(payment.PaymentId.ToString());
        if (existingPayment is null) throw new KeyNotFoundException("Payment not found");

        bool updatedSuccessfully = await _payments.Update(payment);
        return updatedSuccessfully;
    }

    public async Task<bool> DeletePayment(string paymentId)
    {
        var payment = await GetPaymentById(paymentId);
        if (payment is null) throw new KeyNotFoundException("Payment not found");

        bool deletedSuccessfully = await _payments.DeletePayment(Guid.Parse(paymentId));
        return deletedSuccessfully;
    }

    public async Task<PaymentValidationResult> ValidatePayment(Guid paymentId, TransactionDataDto dto)
    {
        var payment = await _payments.GetByPaymentId(paymentId);
        if (payment is null)
            return new PaymentValidationResult.NotFound();

        var transaction = await _transactions.GetTransactionById(payment.TransactionId);
        if (transaction is null)
            return new PaymentValidationResult.InvalidData("Payment exists but its Transaction is missing.");

        payment.CompletedAt = DateTime.UtcNow;
        transaction.Method = dto.Method;
        transaction.Issuer = dto.Issuer;
        transaction.Bank = dto.Bank;

        try
        {
            await _payments.Update(payment);
            var updateTxResult = await _transactions.UpdateTransaction(transaction);

            if (updateTxResult is not TransactionUpdateResult.Success)
                return new PaymentValidationResult.Error("Failed to update transaction details.");

            payment.Transaction = transaction;
            return new PaymentValidationResult.Success(payment);
        }
        catch (Exception ex)
        { return new PaymentValidationResult.Error(ex.Message); }
    }

    public async Task<PaymentRefundResult> RefundPayment(string originalPaymentId, decimal refundAmount, string adminUsername)
    {
        if (!Guid.TryParse(originalPaymentId, out Guid paymentId))
            return new PaymentRefundResult.InvalidInput("Invalid original payment ID format.");

        var originalPayment = await _payments.GetByPaymentId(paymentId);
        if (originalPayment is null)
            return new PaymentRefundResult.NotFound();

        if (refundAmount <= 0 || refundAmount > originalPayment.Amount)
            return new PaymentRefundResult.InvalidInput("Invalid refund amount.");

        var refundTransaction = new TransactionModel { /* ... */ };

        var transactionResult = await _transactions.CreateTransactionConfirmation(refundTransaction);
        if (transactionResult is not TransactionCreationResult.Success success)
            return new PaymentRefundResult.Error("Failed to create refund transaction.");

        var refundPayment = new PaymentModel { /* ... */ };

        try
        {
            var (createdPaymentSuccessfully, confirmedPaymentId) = await _payments.CreateWithId(refundPayment);
            if (!createdPaymentSuccessfully)
            {
                await _transactions.DeleteTransaction(success.Id);
                return new PaymentRefundResult.Error("Failed to create refund payment record.");
            }

            refundPayment.PaymentId = confirmedPaymentId;
            refundPayment.Transaction = success.Transaction;
            return new PaymentRefundResult.Success(refundPayment);
        }
        catch (Exception ex)
        {
            await _transactions.DeleteTransaction(success.Id);
            return new PaymentRefundResult.Error(ex.Message);
        }
    }

    public async Task<decimal> GetTotalAmountForTransaction(string paymentId)
    {
        var payment = await GetPaymentById(paymentId);
        return payment?.Amount ?? decimal.MinValue;
    }
}
