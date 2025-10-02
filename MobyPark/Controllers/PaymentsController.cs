using Microsoft.AspNetCore.Mvc;
using MobyPark.Models;
using MobyPark.Models.Requests;
using MobyPark.Services.Services;

namespace MobyPark.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PaymentsController : BaseController
{
    private readonly ServiceStack _services;

    public PaymentsController(ServiceStack services) : base(services.Sessions)
    {
        _services = services;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] PaymentRequest request)
    {
        var user = GetCurrentUser();

        if (string.IsNullOrEmpty(request.TransactionId) || request.Amount == null)
            return BadRequest(new { error = "Required fields missing" });

        try
        {
            var payment = await _services.Payments.CreatePayment(
                request.TransactionId,
                request.Amount.Value,
                user.Username,
                request.TransactionData
            );

            return CreatedAtAction(nameof(GetUserPayments),
                new { id = payment.TransactionId },
                payment);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("refund")]
    public async Task<IActionResult> Refund([FromBody] PaymentRefundRequest request)
    {
        var user = GetCurrentUser();

        if (user.Role != "ADMIN")
            return Forbid();

        if (request.Amount == null)
            return BadRequest(new { error = "Required field missing: amount" });

        try
        {
            var refund = await _services.Payments.RefundPayment(
                request.CoupledTo,
                request.Amount.Value,
                user.Username
            );

            return StatusCode(201, new { status = "Success", refund });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPut("{transactionId}")]
    public async Task<IActionResult> ValidatePayment(string transactionId, [FromBody] PaymentValidationRequest request)
    {
        var payment = await _services.Payments.GetPaymentByTransactionId(transactionId);

        if (payment.Hash != request.Validation)
            return Unauthorized(new { error = "Validation failed", info = "The security hash could not be validated." });

        await _services.Payments.ValidatePayment(transactionId, payment.Hash, request.TransactionData);
        return Ok(new { status = "Success", payment });
    }

    [HttpGet]
    public async Task<IActionResult> GetUserPayments()
    {
        var user = GetCurrentUser();
        var payments = await _services.Payments.GetPaymentsForUser(user.Username);
        return Ok(payments);
    }

    [HttpGet("{username}")]
    public async Task<IActionResult> GetPaymentsForUser(string username)
    {
        var user = GetCurrentUser();
        if (user.Role != "ADMIN")
            return Forbid();

        var payments = await _services.Payments.GetPaymentsForUser(username);
        return Ok(payments);
    }
}
