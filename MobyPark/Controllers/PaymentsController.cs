using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MobyPark.DTOs;
using MobyPark.DTOs.Payment.Request;
using MobyPark.DTOs.Transaction.Request;
using MobyPark.Services;
using MobyPark.Services.Results.Payment;

namespace MobyPark.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PaymentsController : BaseController
{
    private readonly UserService _users;
    private readonly PaymentService _payments;

    public PaymentsController(UserService users, PaymentService payments) : base(users)
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
            PaymentCreationResult.Success success => CreatedAtAction(nameof(GetUserPayments),
                new { id = success.Payment.PaymentId },
                success.Payment),
            PaymentCreationResult.Error e => StatusCode(500, new { error = e.Message }),
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
            PaymentRefundResult.Success success => StatusCode(201, new { status = "Success", refund = success.RefundPayment }),
            PaymentRefundResult.InvalidInput e => BadRequest(new { error = e.Message }),
            PaymentRefundResult.NotFound => NotFound(new { error = "Original payment not found." }),
            PaymentRefundResult.Error e => StatusCode(500, new { error = e.Message }),
            _ => StatusCode(500, new { error = "An unknown refund error occurred." })
        };
    }

    [HttpPut("{paymentId}")]
    public async Task<IActionResult> ValidatePayment(string paymentId, [FromBody] TransactionDataDto request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        if (!Guid.TryParse(paymentId, out Guid pid))
            return BadRequest(new { error = "Invalid Payment ID format" });

        var result = await _payments.ValidatePayment(pid, request);

        return result switch
        {
            PaymentValidationResult.Success s => Ok(new { status = "Success", payment = s.Payment }),
            PaymentValidationResult.NotFound => NotFound(new { error = "Payment not found." }),
            PaymentValidationResult.InvalidData e => BadRequest(new { error = e.Message }),
            PaymentValidationResult.Error e => StatusCode(500, new { error = e.Message }),
            _ => StatusCode(500, new { error = "An unknown validation error occurred." })
        };
    }

    [HttpGet]
    public async Task<IActionResult> GetUserPayments()
    {
        var user = await GetCurrentUserAsync();
        var payments = await _payments.GetPaymentsByUser(user.Id);
        return Ok(payments);
    }

    [Authorize(Policy = "CanProcessPayments")]
    [HttpGet("{username}")]
    public async Task<IActionResult> GetPaymentsForUser(string username)
    {
        var targetUser = await _users.GetUserByUsername(username);
        if (targetUser == null)
            return NotFound(new { error = "User not found" });

        var payments = await _payments.GetPaymentsByUser(targetUser.Id);
        return Ok(payments);
    }
}
