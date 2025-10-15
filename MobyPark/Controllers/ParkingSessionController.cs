using Microsoft.AspNetCore.Mvc;
using MobyPark.Models;
using MobyPark.Services.Services;
using MobyPark.Models.Requests;
using MobyPark.Models.Requests.Session;

namespace MobyPark.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ParkingSessionController : BaseController
{
    private readonly ServiceStack _services;

    public ParkingSessionController(ServiceStack services) : base(services.Sessions)
    {
        _services = services;
    }

    [HttpDelete("{lotId}/sessions/{sessionId}")]
    public async Task<IActionResult> DeleteSession(int lotId, int sessionId)
    {
        var user = GetCurrentUser();
        if (user.Role != "ADMIN") return Forbid();

        var session = await _services.ParkingSessions.GetParkingSessionById(sessionId);
        if (session.ParkingLotId != lotId)
            return NotFound(new { error = "Session not found" });

        await _services.ParkingSessions.DeleteParkingSession(sessionId);
        return Ok(new { status = "Deleted" });
    }

    [HttpGet("{lotId}/sessions")]
    public async Task<IActionResult> GetSessions(int lotId)
    {
        var user = GetCurrentUser();
        var sessions = await _services.ParkingSessions.GetParkingSessionsByParkingLotId(lotId);

        // Users can only view their own sessions
        if (user.Role != "ADMIN")
            sessions = sessions.Where(session => session.User == user.Username).ToList();

        return Ok(sessions);
    }

    [HttpGet("{lotId}/sessions/{sessionId}")]
    public async Task<IActionResult> GetSession(int lotId, int sessionId)
    {
        var user = GetCurrentUser();
        var session = await _services.ParkingSessions.GetParkingSessionById(sessionId);

        if (session.ParkingLotId != lotId)
            return NotFound(new { error = "Session not found" });

        if (user.Role != "ADMIN" && session.User != user.Username)
            return Forbid();

        return Ok(session);
    }

    [HttpPost("{lotID}/sessions/{sessionId}/stop")]
    public async Task<IActionResult> StopSession(int lotID, [FromBody] PaymentValidationRequest paymentValidationRequest, [FromBody] StopSessionRequest stopSessionRequest)
    {
        var user = GetCurrentUser();
        var payment = await _services.Payments.GetPaymentsByUser(user.Username);
        var validatedPayment = await _services.Payments.ValidatePayment(payment.Last().TransactionId, paymentValidationRequest.Validation, paymentValidationRequest.TransactionData);
        var session = await _services.ParkingSessions.GetParkingLotSessionByLicensePlateAndParkingLotId(lotID, stopSessionRequest);
        decimal TotalPaymentAmount = await _services.Payments.GetTotalAmountForTransaction(validatedPayment.TransactionId);

        if (session.ParkingLotId != lotID)
            return NotFound(new { error = "Session not found" });

        if (user.Role != "ADMIN" && session.User != user.Username)
            return Forbid();

        if (session.Stopped is not null)
            return BadRequest(new { error = "Session already stopped" });

        if (payment.Count == 0)
            return BadRequest(new { error = "No payment found for this user" });

        if (!paymentValidationRequest.Confirmed)
            return BadRequest(new { error = "Payment not confirmed. Restart process." });

        if (payment.Last().Amount == 0)
        {
            return BadRequest(new { error = "No payment required for this session" });
        }
        if (validatedPayment is null)
            return BadRequest(new { error = "Payment validation failed" });



        session.Stopped = DateTime.UtcNow;
        await _services.ParkingSessions.UpdateParkingSession(session);
        return Ok(new { status = "Stopped", session, totalAmount = TotalPaymentAmount });
    }

}
