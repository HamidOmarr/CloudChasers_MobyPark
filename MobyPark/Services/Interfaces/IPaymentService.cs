using MobyPark.DTOs.Payment.Request;
using MobyPark.DTOs.Transaction.Request;
using MobyPark.Models;
using MobyPark.Services.Results.Payment;

namespace MobyPark.Services.Interfaces;

public interface IPaymentService
{
    Task<CreatePaymentResult> CreatePaymentAndTransaction(PaymentCreateDto request);
    Task<GetPaymentResult> GetPaymentById(string paymentId, long requestingUserId);
    Task<GetPaymentResult> GetPaymentByTransactionId(string transactionId, long requestingUserId);
    Task<GetPaymentListResult> GetPaymentsByLicensePlate(string licensePlate, long requestingUserId);
    Task<GetPaymentListResult> GetAllPayments();
    Task<GetPaymentListResult> GetPaymentsByUser(long userId);
    Task<int> CountPayments();
    Task<UpdatePaymentResult> UpdatePayment(PaymentModel payment, long requestingUserId);
    Task<DeletePaymentResult> DeletePayment(string paymentId, long requestingUserId);
    Task<ValidatePaymentResult> ValidatePayment(Guid paymentId, TransactionDataDto dto, long requestingUserId);
    Task<RefundPaymentResult> RefundPayment(string originalPaymentId, decimal refundAmount, string adminUsername);
    Task<decimal?> GetTotalAmountForPayment(string paymentId, long requestingUserId);

}