using System.Transactions;

using MobyPark.DTOs.Payment.Request;
using MobyPark.DTOs.Transaction.Request;
using MobyPark.Models;
using MobyPark.Models.Repositories.Interfaces;
using MobyPark.Services.Interfaces;
using MobyPark.Services.Results.Payment;
using MobyPark.Services.Results.Transaction;
using MobyPark.Validation;

namespace MobyPark.Services;

public class PaymentService : IPaymentService
{
    private readonly IPaymentRepository _payments;
    private readonly ITransactionService _transactions;

    public PaymentService(IPaymentRepository payments, ITransactionService transactions)
    {
        _payments = payments;
        _transactions = transactions;
    }

    public async Task<CreatePaymentResult> CreatePaymentAndTransaction(CreatePaymentDto request)
    {
        var transaction = new TransactionModel
        {
            Id = Guid.NewGuid(),
            Amount = request.Amount
            // Rest are empty strings until payment processing is done
        };

        var transactionResult = await _transactions.CreateTransaction(transaction);

        if (transactionResult is not CreateTransactionResult.Success success)
            return new CreatePaymentResult.Error("Failed to create transaction record.");

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
                return new CreatePaymentResult.Error("Failed to create payment record after transaction.");
            }

            payment.PaymentId = paymentId;
            payment.Transaction = success.Transaction;
            return new CreatePaymentResult.Success(payment);
        }
        catch (Exception)
        {
            await _transactions.DeleteTransaction(success.Id);
            return new CreatePaymentResult.Error("An error occurred while saving the payment.");
        }
    }

    public async Task<GetPaymentResult> GetPaymentById(string pId, long requestingUserId)
    {
        if (!Guid.TryParse(pId, out Guid paymentId))
            return new GetPaymentResult.InvalidInput("Invalid Payment ID format.");

        var payment = await _payments.GetByPaymentId(paymentId, requestingUserId);
        if (payment is null)
            return new GetPaymentResult.NotFound();

        return new GetPaymentResult.Success(payment);
    }
    
    public async Task<GetPaymentResult> GetPaymentByIdAsync(Guid pId)
    {
        var payment = await _payments.FindByIdAsync(pId);
        if (payment is null)
            return new GetPaymentResult.NotFound();

        return new GetPaymentResult.Success(payment);
    }

    public async Task<DeletePaymentResult> DeletePayment(string paymentId, long requestingUserId)
    {
        if (!Guid.TryParse(paymentId, out Guid pid))
            return new DeletePaymentResult.Error("Invalid Payment ID format.");

        try
        {
            if (!await _payments.DeletePayment(pid, requestingUserId))
                return new DeletePaymentResult.NotFound();

            return new DeletePaymentResult.Success();
        }
        catch (Exception ex)
        { return new DeletePaymentResult.Error(ex.Message); }
    }

    public async Task<GetPaymentResult> GetPaymentByTransactionId(string tId, long requestingUserId)
    {
        if (!Guid.TryParse(tId, out Guid transactionId))
            return new GetPaymentResult.InvalidInput("Invalid Transaction ID format.");

        var payment = await _payments.GetByTransactionId(transactionId, requestingUserId);
        if (payment is null)
            return new GetPaymentResult.NotFound();

        return new GetPaymentResult.Success(payment);
    }

    public async Task<GetPaymentListResult> GetPaymentsByLicensePlate(string licensePlate, long requestingUserId)
    {
        licensePlate = licensePlate.Upper();
        var payments = await _payments.GetByLicensePlate(licensePlate, requestingUserId);
        if (payments.Count == 0)
            return new GetPaymentListResult.NotFound();

        return new GetPaymentListResult.Success(payments);
    }

    public async Task<GetPaymentListResult> GetAllPayments()
    {
        var payments = await _payments.GetAll();
        if (payments.Count == 0)
            return new GetPaymentListResult.NotFound();

        return new GetPaymentListResult.Success(payments);
    }

    public async Task<GetPaymentListResult> GetPaymentsByUser(long userId)
    {
        var payments = await _payments.GetByUserId(userId);
        if (payments.Count == 0)
            return new GetPaymentListResult.NotFound();

        return new GetPaymentListResult.Success(payments);
    }

    public async Task<int> CountPayments() => await _payments.Count();

    private async Task<UpdatePaymentResult> SetPaymentCompleted(Guid paymentId, long requestingUserId)
    {
        var getResult = await GetPaymentById(paymentId.ToString(), requestingUserId);
        if (getResult is not GetPaymentResult.Success successResult)
        {
            return getResult switch
            {
                GetPaymentResult.NotFound => new UpdatePaymentResult.NotFound(),
                GetPaymentResult.InvalidInput err => new UpdatePaymentResult.Error(err.Message),
                _ => new UpdatePaymentResult.Error("Failed to retrieve payment.")
            };
        }
        var existingPayment = successResult.Payment;

        if (existingPayment.CompletedAt.HasValue)
            return new UpdatePaymentResult.AlreadyCompleted();

        var dto = new CompletePaymentDto { CompletedAt = DateTime.UtcNow };

        try
        {
            bool updated = await _payments.Update(existingPayment, dto);
            if (!updated)
                return new UpdatePaymentResult.Error("Database update failed or reported no changes for payment completion.");

            return new UpdatePaymentResult.Success(existingPayment);
        }
        catch (Exception ex)
        { return new UpdatePaymentResult.Error($"Error saving payment completion: {ex.Message}"); }
    }

    public async Task<ValidatePaymentResult> ValidatePayment(Guid paymentId, TransactionDataDto dto, long requestingUserId)
    {
        var getResult = await GetPaymentById(paymentId.ToString(), requestingUserId);
        if (getResult is GetPaymentResult.NotFound)
            return new ValidatePaymentResult.NotFound();
        if (getResult is GetPaymentResult.InvalidInput)
            return new ValidatePaymentResult.Error("Invalid Payment ID format.");

        var payment = ((GetPaymentResult.Success)getResult).Payment;

        if (payment.CompletedAt.HasValue)
            return new ValidatePaymentResult.InvalidData("Payment has already been completed.");

        var transactionResult = await _transactions.GetTransactionById(payment.TransactionId);

        if (transactionResult is not GetTransactionResult.Success tSuccess)
            return transactionResult switch
            {
                GetTransactionResult.NotFound => new ValidatePaymentResult.InvalidData("Payment exists but its Transaction not found."),
                GetTransactionResult.InvalidInput invalidInput => new ValidatePaymentResult.Error(invalidInput.Message),
                _ => new ValidatePaymentResult.Error("Failed to retrieve associated Transaction.")
            };

        var transaction = tSuccess.Transaction;

        payment.CompletedAt = DateTime.UtcNow;
        transaction.Method = dto.Method;
        transaction.Token = dto.Issuer;
        transaction.Bank = dto.Bank;

        // Transaction scope ensures both payment and transaction updates succeed or fail together, as they are 1:1 linked.
        using var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);
        try
        {
            var setCompletedResult = await SetPaymentCompleted(payment.PaymentId, requestingUserId);
            if (setCompletedResult is not UpdatePaymentResult.Success paymentSuccess)
                return setCompletedResult switch
                {
                    UpdatePaymentResult.NotFound => new ValidatePaymentResult.NotFound(),
                    UpdatePaymentResult.AlreadyCompleted => new ValidatePaymentResult.InvalidData(
                        "Payment has already been completed."),
                    UpdatePaymentResult.Error err => new ValidatePaymentResult.Error(err.Message),
                    _ => new ValidatePaymentResult.Error("Failed to set payment as completed.")
                };

            var updateTransactionResult = await _transactions.UpdateTransaction(transaction.Id, dto);
            if (updateTransactionResult is not UpdateTransactionResult.Success transactionSuccess)
                return updateTransactionResult switch
                {
                    UpdateTransactionResult.NotFound => new ValidatePaymentResult.InvalidData(
                        "Associated Transaction not found during update."),
                    UpdateTransactionResult.Error err => new ValidatePaymentResult.Error(err.Message),
                    _ => new ValidatePaymentResult.Error("Failed to update transaction details.")
                };

            scope.Complete();

            payment = paymentSuccess.Payment;
            payment.Transaction = transactionSuccess.Transaction;
            return new ValidatePaymentResult.Success(payment);
        }

        catch (Exception ex)
        { return new ValidatePaymentResult.Error($"An error occurred during validation: {ex.Message}"); }
    }

    public async Task<RefundPaymentResult> RefundPayment(string originalPaymentId, decimal refundAmount, string adminUsername)
    {
        if (!Guid.TryParse(originalPaymentId, out Guid paymentId))
            return new RefundPaymentResult.InvalidInput("Invalid original payment ID format.");

        var originalPayment = await _payments.GetByPaymentIdAdmin(paymentId);
        if (originalPayment is null)
            return new RefundPaymentResult.NotFound();

        if (refundAmount <= 0 || refundAmount > originalPayment.Amount)
            return new RefundPaymentResult.InvalidInput("Invalid refund amount.");

        var refundTransaction = new TransactionModel
        {
            Id = Guid.NewGuid(),
            Amount = -Math.Abs(refundAmount),
            Method = "REFUND",
            Token = "ADMIN",
            Bank = adminUsername
        };

        var transactionResult = await _transactions.CreateTransaction(refundTransaction);
        if (transactionResult is not CreateTransactionResult.Success success)
            return new RefundPaymentResult.Error("Failed to create refund transaction.");

        var refundPayment = new PaymentModel
        {
            PaymentId = Guid.NewGuid(),
            Amount = -Math.Abs(refundAmount),
            LicensePlateNumber = originalPayment.LicensePlateNumber,
            CreatedAt = DateTime.UtcNow,
            CompletedAt = DateTime.UtcNow,
            TransactionId = success.Id
        };

        try
        {
            var (createdPaymentSuccessfully, confirmedPaymentId) = await _payments.CreateWithId(refundPayment);
            if (!createdPaymentSuccessfully)
            {
                await _transactions.DeleteTransaction(success.Id);
                return new RefundPaymentResult.Error("Failed to create refund payment record.");
            }

            refundPayment.PaymentId = confirmedPaymentId;
            refundPayment.Transaction = success.Transaction;
            return new RefundPaymentResult.Success(refundPayment);
        }
        catch (Exception ex)
        {
            await _transactions.DeleteTransaction(success.Id);
            return new RefundPaymentResult.Error(ex.Message);
        }
    }

    public async Task<decimal?> GetTotalAmountForPayment(string paymentId, long requestingUserId)
    {
        var result = await GetPaymentById(paymentId, requestingUserId);
        if (result is GetPaymentResult.Success success)
            return success.Payment.Amount;
        return null;
    }
}