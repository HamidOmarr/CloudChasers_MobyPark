using Microsoft.AspNetCore.Mvc;
using MobyPark.DTOs;
using MobyPark.Models;
using MobyPark.Services;
using MobyPark.Services.Services;

namespace MobyPark.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PaymentsController : BaseController
{
    private readonly UserService _users;
    private readonly PaymentService _payments;

    public PaymentsController(UserService users, PaymentService payments) : base(users)
    {
        _payments = payments;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] PaymentRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var user = GetCurrentUserAsync();

        if (request.Amount == null)
            return BadRequest(new { error = "Required fields missing" });

        var createTransactionDataId = Guid.NewGuid(); // Placeholder for transaction data ID generation, has to be done in payment service later, whilst creating the transaction alongside the payment

        var payment = new PaymentModel
        {
            PaymentId = Guid.NewGuid(),
            Amount = request.Amount.Value,
            LicensePlateNumber = request.LicensePlateNumber,
            CreatedAt = DateTime.UtcNow,
            CompletedAt = null,
            TransactionId = createTransactionDataId
        };

        try
        {
            var makePayment = await _payments.CreatePayment(payment);

            return CreatedAtAction(nameof(GetUserPayments),
                new { id = makePayment.TransactionId },
                payment);
        }
        catch (ArgumentException ex)
        { return BadRequest(new { error = ex.Message }); }
    }

    [HttpPost("refund")]
    public async Task<IActionResult> Refund([FromBody] PaymentRefundRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var user = GetCurrentUserAsync();

        if (user.Role != "ADMIN")
            return Forbid();

        if (request.Amount == null)
            return BadRequest(new { error = "Required field missing: amount" });

        try
        {
            var refund = await _payments.RefundPayment(
                request.CoupledTo,
                request.Amount.Value,
                user.Username
            );

            return StatusCode(201, new { status = "Success", refund });
        }
        catch (ArgumentException ex)
        { return BadRequest(new { error = ex.Message }); }
    }

    [HttpPut("{transactionId}")]
    public async Task<IActionResult> ValidatePayment(string transactionId, [FromBody] PaymentValidationRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var payment = await _payments.GetPaymentByTransactionId(transactionId);

        if (payment.Hash != request.Validation)
            return Unauthorized(new { error = "Validation failed", info = "The security hash could not be validated." });

        await _payments.ValidatePayment(transactionId, payment.Hash, request.TransactionData);
        return Ok(new { status = "Success", payment });
    }

    [HttpGet]
    public async Task<IActionResult> GetUserPayments()
    {
        var user = GetCurrentUserAsync();
        var payments = await _payments.GetPaymentsByUser(user.Username);
        return Ok(payments);
    }

    [HttpGet("{username}")]
    public async Task<IActionResult> GetPaymentsForUser(string username)
    {
        var user = GetCurrentUserAsync();
        if (user.Role != "ADMIN")
            return Forbid();

        var payments = await _payments.GetPaymentsByUser(username);
        return Ok(payments);
    }
}
