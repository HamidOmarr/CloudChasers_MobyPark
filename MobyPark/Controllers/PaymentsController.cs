using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using MobyPark.DTOs.Payment.Request;
using MobyPark.DTOs.Payment.Response;
using MobyPark.DTOs.Shared;
using MobyPark.DTOs.Transaction.Request;
using MobyPark.Models;
using MobyPark.Services.Interfaces;
using MobyPark.Services.Results.Payment;
using MobyPark.Services.Results.User;

using Swashbuckle.AspNetCore.Annotations;

namespace MobyPark.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class PaymentsController : BaseController
{
    private readonly IUserService _users;
    private readonly IPaymentService _payments;

    public PaymentsController(IUserService users, IPaymentService payments) : base(users)
    {
        _users = users;
        _payments = payments;
    }

    [HttpPost]
    [SwaggerOperation(Summary = "Creates a pending payment and associated transaction.")]
    [SwaggerResponse(201, "Payment created", typeof(PaymentResponseDto))]
    [SwaggerResponse(500, "Internal processing error")]
    public async Task<IActionResult> Create([FromBody] CreatePaymentDto request)
    {
        var result = await _payments.CreatePaymentAndTransaction(request);

        return result switch
        {
            CreatePaymentResult.Success success => CreatedAtAction(nameof(GetPaymentById),
                new StatusResponseDto { Message = success.Payment.PaymentId.ToString() },
                new PaymentResponseDto
                {
                    Status = "Created",
                    Payment = success.Payment
                }),
            CreatePaymentResult.Error e => StatusCode(500, new ErrorResponseDto { Error = e.Message }),
            _ => StatusCode(500, new ErrorResponseDto { Error = "An unknown payment creation error occurred." })
        };
    }

    [HttpPost("refund")]
    [Authorize(Policy = "CanProcessPayments")]
    [SwaggerOperation(Summary = "Refunds an existing payment (Admin only).")]
    [SwaggerResponse(201, "Refund processed", typeof(RefundResponseDto))]
    [SwaggerResponse(400, "Invalid input or missing amount")]
    [SwaggerResponse(404, "Original payment not found")]
    public async Task<IActionResult> Refund([FromBody] PaymentRefundDto request)
    {
        var adminUser = (await GetCurrentUserAsync()).Username;
        if (request.Amount is null)
            return BadRequest(new ErrorResponseDto { Error = "Required field missing: Amount" });

        var result = await _payments.RefundPayment(
            request.PaymentId,
            request.Amount.Value,
            adminUser
        );

        return result switch
        {
            RefundPaymentResult.Success success => StatusCode(201, new RefundResponseDto
            {
                Status = "Success",
                Refund = success.RefundPayment
            }),
            RefundPaymentResult.InvalidInput e => BadRequest(new ErrorResponseDto { Error = e.Message }),
            RefundPaymentResult.NotFound => NotFound(new ErrorResponseDto { Error = "Original payment not found." }),
            RefundPaymentResult.Error e => StatusCode(500, new ErrorResponseDto { Error = e.Message }),
            _ => StatusCode(500, new ErrorResponseDto { Error = "An unknown refund error occurred." })
        };
    }

    [HttpPut("{paymentId}")]
    [Authorize]
    [SwaggerOperation(Summary = "Validates and completes a payment.")]
    [SwaggerResponse(200, "Payment validated", typeof(PaymentResponseDto))]
    [SwaggerResponse(400, "Invalid ID or data")]
    [SwaggerResponse(404, "Payment not found")]
    public async Task<IActionResult> ValidatePayment(string paymentId, [FromBody] TransactionDataDto request)
    {
        if (!Guid.TryParse(paymentId, out Guid pid))
            return BadRequest(new ErrorResponseDto { Error = "Invalid Payment ID format" });

        var user = await GetCurrentUserAsync();
        var result = await _payments.ValidatePayment(pid, request, user.Id);

        return result switch
        {
            ValidatePaymentResult.Success s => Ok(new PaymentResponseDto
            {
                Status = "Success",
                Payment = s.Payment
            }),
            ValidatePaymentResult.NotFound => NotFound(new ErrorResponseDto { Error = "Payment not found or access denied." }),
            ValidatePaymentResult.InvalidData e => BadRequest(new ErrorResponseDto { Error = e.Message }),
            ValidatePaymentResult.Error e => StatusCode(500, new ErrorResponseDto { Error = e.Message }),
            _ => StatusCode(500, new ErrorResponseDto { Error = "An unknown validation error occurred." })
        };
    }

    [HttpGet("{username}")]
    [Authorize(Policy = "CanProcessPayments")]
    [SwaggerOperation(Summary = "Retrieves all payments for a specific user (Admin only).")]
    [SwaggerResponse(200, "List retrieved", typeof(List<PaymentModel>))]
    [SwaggerResponse(404, "User or payments not found")]
    public async Task<IActionResult> GetPaymentsForUser(string username)
    {
        var targetUserResult = await _users.GetUserByUsername(username);
        if (targetUserResult is not GetUserResult.Success sUser)
            return NotFound(new ErrorResponseDto { Error = "User not found" });

        var result = await _payments.GetPaymentsByUser(sUser.User.Id);

        return result switch
        {
            GetPaymentListResult.Success s => Ok(s.Payments),
            GetPaymentListResult.NotFound => NotFound(new ErrorResponseDto { Error = "No payments found for the user." }),
            _ => StatusCode(500, new ErrorResponseDto { Error = "An unknown error occurred." })
        };
    }

    [HttpGet("id/{paymentId}")]
    [Authorize]
    [SwaggerOperation(Summary = "Retrieves a payment by its unique ID.")]
    [SwaggerResponse(200, "Payment found", typeof(PaymentModel))]
    [SwaggerResponse(400, "Invalid ID format")]
    [SwaggerResponse(404, "Payment not found")]
    public async Task<IActionResult> GetPaymentById(string paymentId)
    {
        var user = await GetCurrentUserAsync();
        var result = await _payments.GetPaymentById(paymentId, user.Id);

        return result switch
        {
            GetPaymentResult.Success s => Ok(s.Payment),
            GetPaymentResult.NotFound => NotFound(new ErrorResponseDto { Error = "Payment not found or access denied" }),
            GetPaymentResult.InvalidInput e => BadRequest(new ErrorResponseDto { Error = e.Message }),
            _ => StatusCode(500, new ErrorResponseDto { Error = "An unknown error occurred." })
        };
    }

    [HttpGet("transaction/{transactionId}")]
    [Authorize]
    [SwaggerOperation(Summary = "Retrieves a payment by its associated transaction ID.")]
    [SwaggerResponse(200, "Payment found", typeof(PaymentModel))]
    [SwaggerResponse(400, "Invalid ID format")]
    [SwaggerResponse(404, "Payment not found")]
    public async Task<IActionResult> GetPaymentByTransactionId(string transactionId)
    {
        var user = await GetCurrentUserAsync();
        var result = await _payments.GetPaymentByTransactionId(transactionId, user.Id);

        return result switch
        {
            GetPaymentResult.Success s => Ok(s.Payment),
            GetPaymentResult.NotFound => NotFound(new ErrorResponseDto { Error = "Payment not found or access denied" }),
            GetPaymentResult.InvalidInput e => BadRequest(new ErrorResponseDto { Error = e.Message }),
            _ => StatusCode(500, new ErrorResponseDto { Error = "An unknown error occurred." })
        };
    }

    [HttpGet("plate/{licensePlate}")]
    [Authorize]
    [SwaggerOperation(Summary = "Retrieves all payments associated with a license plate.")]
    [SwaggerResponse(200, "List retrieved", typeof(List<PaymentModel>))]
    [SwaggerResponse(404, "No payments found")]
    public async Task<IActionResult> GetPaymentsByLicensePlate(string licensePlate)
    {
        var user = await GetCurrentUserAsync();
        var result = await _payments.GetPaymentsByLicensePlate(licensePlate, user.Id);

        return result switch
        {
            GetPaymentListResult.Success s => Ok(s.Payments),
            GetPaymentListResult.NotFound => NotFound(new ErrorResponseDto { Error = "No payments found for the specified license plate or access denied." }),
            _ => StatusCode(500, new ErrorResponseDto { Error = "An unknown error occurred." })
        };
    }

    [HttpDelete("{paymentId}")]
    [Authorize]
    [SwaggerOperation(Summary = "Deletes a payment record.")]
    [SwaggerResponse(200, "Deleted successfully", typeof(object))]
    [SwaggerResponse(404, "Payment not found")]
    public async Task<IActionResult> DeletePayment(string paymentId)
    {
        var user = await GetCurrentUserAsync();
        var result = await _payments.DeletePayment(paymentId, user.Id);

        return result switch
        {
            DeletePaymentResult.Success => Ok(new StatusResponseDto { Status = "Deleted" }),
            DeletePaymentResult.NotFound => NotFound(new ErrorResponseDto { Error = "Payment not found or access denied" }),
            DeletePaymentResult.Error e => StatusCode(500, new ErrorResponseDto { Error = e.Message }),
            _ => StatusCode(500, new ErrorResponseDto { Error = "An unknown error occurred." })
        };
    }
}