using System.Security.Claims;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using MobyPark.DTOs.Business;
using MobyPark.Services.Interfaces;

using Swashbuckle.AspNetCore.Annotations;

namespace MobyPark.Controllers;

[ApiController]
[Route("api/BusinessParking")]
[Produces("application/json")]
public class BusinessParkingRegistrationsController : BaseController
{
    private readonly IBusinessParkingRegistrationService _registrationService;

    public BusinessParkingRegistrationsController(IUserService users,
        IBusinessParkingRegistrationService registrationService) : base(users)
    {
        _registrationService = registrationService;
    }

    [HttpPost]
    [Authorize("CanManageBusinesses")]
    [SwaggerOperation(Summary = "Creates a new business registration (Admin only).")]
    [SwaggerResponse(200, "Registration created", typeof(ReadBusinessRegDto))]
    [SwaggerResponse(404, "Business ID not found")]
    [SwaggerResponse(409, "License plate already registered or active")]
    public async Task<IActionResult> CreateBusinessRegistrationAdmin([FromBody] CreateBusinessRegAdminDto bReg)
    {
        var result = await _registrationService.CreateBusinessRegistrationAdminAsync(bReg);
        return FromServiceResult(result);
    }

    [HttpPost("self")]
    [SwaggerOperation(Summary = "Creates a registration for the currently logged-in user.")]
    [SwaggerResponse(200, "Registration created", typeof(ReadBusinessRegDto))]
    [SwaggerResponse(404, "User or Business profile not found")]
    [SwaggerResponse(409, "User not authorized OR License plate already active")]
    public async Task<IActionResult> CreateBusinessRegistration(CreateBusinessRegDto bReg)
    {
        long currentUserId = GetCurrentUserId();
        var result = await _registrationService.CreateBusinessRegistrationAsync(bReg, currentUserId);
        return FromServiceResult(result);
    }

    [HttpPatch]
    [Authorize("CanManageBusinesses")]
    [SwaggerOperation(Summary = "Updates the active status of a registration (Admin only).")]
    [SwaggerResponse(200, "Status updated", typeof(ReadBusinessRegDto))]
    [SwaggerResponse(404, "Registration not found")]
    public async Task<IActionResult> SetBusinessRegistrationActiveAdmin(PatchBusinessRegDto bReg)
    {
        var result = await _registrationService.SetBusinessRegistrationActiveAdminAsync(bReg);
        return FromServiceResult(result);
    }

    [HttpPatch("self")]
    [SwaggerOperation(Summary = "Updates the active status of the user's own registration.")]
    [SwaggerResponse(200, "Status updated", typeof(ReadBusinessRegDto))]
    [SwaggerResponse(404, "Registration not found")]
    [SwaggerResponse(409, "User not authorized to modify this registration")]
    public async Task<IActionResult> SetBusinessRegistrationActive(
        PatchBusinessRegDto bReg)
    {
        long currentUserId = long.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var result = await _registrationService.SetBusinessRegistrationActiveAsync(bReg, currentUserId);
        return FromServiceResult(result);
    }

    [HttpDelete("{id:long}")]
    [Authorize("CanManageBusinesses")]
    [SwaggerOperation(Summary = "Deletes a registration by ID (Admin only).")]
    [SwaggerResponse(200, "Deleted successfully", typeof(bool))]
    [SwaggerResponse(404, "Registration not found")]
    public async Task<IActionResult> AdminDeleteBusinessRegistration(
        long id)
    {
        var result = await _registrationService.AdminDeleteBusinessRegistrationAsync(id);
        return FromServiceResult(result);
    }

    [HttpGet("{id:long}")]
    [SwaggerOperation(Summary = "Retrieves a registration by its ID.")]
    [SwaggerResponse(200, "Found registration", typeof(ReadBusinessRegDto))]
    [SwaggerResponse(404, "Not found")]
    public async Task<IActionResult> GetBusinessRegistrationById(long id)
    {
        var result = await _registrationService.GetBusinessRegistrationByIdAsync(id);
        return FromServiceResult(result);
    }

    [HttpGet("business/{id:long}")]
    [SwaggerOperation(Summary = "Retrieves all registrations linked to a specific business ID.")]
    [SwaggerResponse(200, "List retrieved", typeof(List<ReadBusinessRegDto>))]
    [SwaggerResponse(404, "No registrations found for this business")]
    public async Task<IActionResult> GetBusinessRegistrationsByBusiness(long id)
    {
        var result = await _registrationService.GetBusinessRegistrationsByBusinessAsync(id);
        return FromServiceResult(result);
    }

    [HttpGet("{licensePlate}")]
    [SwaggerOperation(Summary = "Retrieves registrations matching a license plate.")]
    [SwaggerResponse(200, "Found registrations", typeof(List<ReadBusinessRegDto>))]
    [SwaggerResponse(404, "Not found")]
    public async Task<IActionResult> GetBusinessRegistrationByLicensePlate(string licensePlate)
    {
        var result = await _registrationService.GetBusinessRegistrationByLicensePlateAsync(licensePlate);
        return FromServiceResult(result);
    }

    [HttpGet("active/{licensePlate}")]
    [SwaggerOperation(Summary = "Retrieves only ACTIVE registrations for a license plate.")]
    [SwaggerResponse(200, "Found active registration", typeof(ReadBusinessRegDto))]
    [SwaggerResponse(404, "No active registration found")]
    public async Task<IActionResult> GetActiveBusinessRegistrationByLicencePlate(string licensePlate)
    {
        var result = await _registrationService.GetActiveBusinessRegistrationByLicencePlateAsync(licensePlate);
        return FromServiceResult(result);
    }
}