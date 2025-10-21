using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MobyPark.DTOs;
using MobyPark.DTOs.Payment.Request;
using MobyPark.DTOs.Transaction.Request;
using MobyPark.Services.Interfaces;
using MobyPark.Services.Results.Payment;
using MobyPark.Services.Results.User;

namespace MobyPark.Controllers;

[ApiController]
[Route("api/[controller]")]
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
    public async Task<IActionResult> Create([FromBody] PaymentCreateDto request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        var result = await _payments.CreatePaymentAndTransaction(request);

        return result switch
        {
            CreatePaymentResult.Success success => CreatedAtAction(nameof(GetPaymentById),
                new { paymentId = success.Payment.PaymentId },
                success.Payment),
            CreatePaymentResult.Error e => StatusCode(500, new { error = e.Message }),
            _ => StatusCode(500, new { error = "An unknown payment creation error occurred." })
        };
    }

    [Authorize(Policy = "CanProcessPayments")]
    [HttpPost("refund")]
    public async Task<IActionResult> Refund([FromBody] PaymentRefundRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var adminUser = (await GetCurrentUserAsync()).Username;

        if (request.Amount == null)
            return BadRequest(new { error = "Required field missing: Amount" });

        // No more try...catch!
        var result = await _payments.RefundPayment(
            request.PaymentId,
            request.Amount.Value,
            adminUser
        );

        return result switch
        {
            RefundPaymentResult.Success success => StatusCode(201, new { status = "Success", refund = success.RefundPayment }),
            RefundPaymentResult.InvalidInput e => BadRequest(new { error = e.Message }),
            RefundPaymentResult.NotFound => NotFound(new { error = "Original payment not found." }),
            RefundPaymentResult.Error e => StatusCode(500, new { error = e.Message }),
            _ => StatusCode(500, new { error = "An unknown refund error occurred." })
        };
    }

    [Authorize]
    [HttpPut("{paymentId}")]
    public async Task<IActionResult> ValidatePayment(string paymentId, [FromBody] TransactionDataDto request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        if (!Guid.TryParse(paymentId, out Guid pid))
            return BadRequest(new { error = "Invalid Payment ID format" });

        var user = await GetCurrentUserAsync();
        var result = await _payments.ValidatePayment(pid, request, user.Id);

        return result switch
        {
            ValidatePaymentResult.Success s => Ok(new { status = "Success", payment = s.Payment }),
            ValidatePaymentResult.NotFound => NotFound(new { error = "Payment not found or access denied." }),
            ValidatePaymentResult.InvalidData e => BadRequest(new { error = e.Message }),
            ValidatePaymentResult.Error e => StatusCode(500, new { error = e.Message }),
            _ => StatusCode(500, new { error = "An unknown validation error occurred." })
        };
    }

    [Authorize(Policy = "CanProcessPayments")]
    [HttpGet("{username}")]
    public async Task<IActionResult> GetPaymentsForUser(string username)
    {
        var targetUserResult = await _users.GetUserByUsername(username);
        if (targetUserResult is not GetUserResult.Success sUser)
            return NotFound(new { error = "User not found" });

        var result = await _payments.GetPaymentsByUser(sUser.User.Id);

        return result switch
        {
            GetPaymentListResult.Success s => Ok(s.Payments),
            GetPaymentListResult.NotFound => NotFound(new { error = "No payments found for the user." }),
            _ => StatusCode(500, new { error = "An unknown error occurred." })
        };
    }

    [Authorize]
    [HttpGet("id/{paymentId}")]
    public async Task<IActionResult> GetPaymentById(string paymentId)
    {
        var user = await GetCurrentUserAsync();
        var result = await _payments.GetPaymentById(paymentId, user.Id);

        return result switch
        {
            GetPaymentResult.Success s => Ok(s.Payment),
            GetPaymentResult.NotFound => NotFound(new { error = "Payment not found or access denied" }),
            GetPaymentResult.InvalidInput e => BadRequest(new { error = e.Message }),
            _ => StatusCode(500, new { error = "An unknown error occurred." })
        };
    }

    [Authorize]
    [HttpGet("transaction/{transactionId}")]
    public async Task<IActionResult> GetPaymentByTransactionId(string transactionId)
    {
        var user = await GetCurrentUserAsync();
        var result = await _payments.GetPaymentByTransactionId(transactionId, user.Id);

        return result switch
        {
            GetPaymentResult.Success s => Ok(s.Payment),
            GetPaymentResult.NotFound => NotFound(new { error = "Payment not found or access denied" }),
            GetPaymentResult.InvalidInput e => BadRequest(new { error = e.Message }),
            _ => StatusCode(500, new { error = "An unknown error occurred." })
        };
    }

    [Authorize]
    [HttpGet("plate/{licensePlate}")]
    public async Task<IActionResult> GetPaymentsByLicensePlate(string licensePlate)
    {
        var user = await GetCurrentUserAsync();
        var result = await _payments.GetPaymentsByLicensePlate(licensePlate, user.Id);

        return result switch
        {
            GetPaymentListResult.Success s => Ok(s.Payments),
            GetPaymentListResult.NotFound => NotFound(new { error = "No payments found for the specified license plate or access denied." }),

            _ => StatusCode(500, new { error = "An unknown error occurred." })
        };
    }

    [Authorize]
    [HttpDelete("{paymentId}")]
    public async Task<IActionResult> DeletePayment(string paymentId)
    {
         var user = await GetCurrentUserAsync();
        var result = await _payments.DeletePayment(paymentId, user.Id);

        return result switch
        {
            DeletePaymentResult.Success => Ok(new { status = "Deleted" }),
            DeletePaymentResult.NotFound => NotFound(new { error = "Payment not found or access denied" }),
            DeletePaymentResult.Error e => StatusCode(500, new { error = e.Message }),
            _ => StatusCode(500, new { error = "An unknown error occurred." })
        };
    }
}
