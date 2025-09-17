using Microsoft.AspNetCore.Mvc;
using MobyPark.Models;
using MobyPark.Models.Access;
using MobyPark.Models.Requests;
using MobyPark.Services;

namespace MobyPark.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PaymentsController : BaseController
{
    private readonly PaymentAccess _paymentAccess;
    private readonly ParkingSessionService _sessionServiceCalculator;

    public PaymentsController(SessionService sessionService, PaymentAccess paymentAccess, ParkingSessionService sessionServiceCalculator)
        : base(sessionService)
    {
        _paymentAccess = paymentAccess;
        _sessionServiceCalculator = sessionServiceCalculator;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] PaymentRequest request)
    {
        var user = GetCurrentUser();

        if (string.IsNullOrEmpty(request.TransactionId) || request.Amount == null)
            return BadRequest(new { error = "Required fields missing" });

        var payment = new PaymentModel
        {
            TransactionId = request.TransactionId,
            Amount = request.Amount.Value,
            Initiator = user.Username,
            CreatedAt = DateTime.UtcNow,
            Completed = DateTime.UtcNow,
            Hash = _sessionServiceCalculator.GenerateTransactionValidationHash()
        };

        await _paymentAccess.Create(payment);
        return StatusCode(201, new { status = "Success", payment });
    }

    [HttpPost("refund")]
    public async Task<IActionResult> Refund([FromBody] PaymentRefundRequest request)
    {
        var user = GetCurrentUser();

        if (user.Role != "ADMIN")
            return Forbid();

        if (request.Amount == null)
            return BadRequest(new { error = "Required field missing: amount" });

        var payment = new PaymentModel
        {
            TransactionId = string.IsNullOrEmpty(request.TransactionId) ? Guid.NewGuid().ToString("N") : request.TransactionId,
            Amount = -Math.Abs(request.Amount.Value),
            CoupledTo = request.CoupledTo,
            CreatedAt = DateTime.UtcNow,
            Completed = DateTime.UtcNow,
            Hash = _sessionServiceCalculator.GenerateTransactionValidationHash()
        };

        await _paymentAccess.Create(payment);
        return StatusCode(201, new { status = "Success", payment });
    }

    [HttpPut("{transactionId}")]
    public async Task<IActionResult> ValidatePayment(string transactionId, [FromBody] PaymentValidationRequest request)
    {
        var payment = await _paymentAccess.GetByTransactionId(transactionId);
        if (payment == null)
            return NotFound(new { error = "Payment not found" });

        if (payment.Hash != request.Validation)
            return Unauthorized(new { error = "Validation failed", info = "The security hash could not be validated." });

        payment.Completed = DateTime.UtcNow;
        payment.TransactionData = request.TransactionData;

        await _paymentAccess.Update(payment);
        return Ok(new { status = "Success", payment });
    }

    [HttpGet]
    public async Task<IActionResult> GetUserPayments()
    {
        var user = GetCurrentUser();
        var payments = await _paymentAccess.GetByUser(user.Username);
        return Ok(payments);
    }

    [HttpGet("{username}")]
    public async Task<IActionResult> GetPaymentsForUser(string username)
    {
        var user = GetCurrentUser();
        if (user.Role != "ADMIN")
            return Forbid();

        var payments = await _paymentAccess.GetByUser(username);
        return Ok(payments);
    }
}
