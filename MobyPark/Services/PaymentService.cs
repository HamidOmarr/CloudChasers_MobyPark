using MobyPark.Models;
using MobyPark.Models.Repositories.Interfaces;
using MobyPark.Models.Repositories.RepositoryStack;
using MobyPark.Validation;

namespace MobyPark.Services;

public class PaymentService
{
    private readonly IPaymentRepository _payments;

    public PaymentService(IRepositoryStack repoStack)
    {
        _payments = repoStack.Payments;
    }

    public async Task<PaymentModel> CreatePayment(PaymentModel payment)
    {
        ServiceValidator.Payment(payment);

        var (createdSuccessfully, id) = await _payments.CreateWithId(payment);
        if (!createdSuccessfully)
            throw new Exception("Failed to create payment");
        payment.PaymentId = id;
        return payment;
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

    public async Task<int> CountPayments() => await _payments.Count();

    private async Task<bool> UpdatePayment(PaymentModel payment)
    {
        ServiceValidator.Payment(payment);

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

    public async Task<decimal> GetTotalAmountForTransaction(string paymentId)
    {
        var payment = await GetPaymentById(paymentId);
        return payment?.Amount ?? decimal.MinValue;
    }
}
