using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MobyPark.DTOs.Business;
using MobyPark.Services;
using MobyPark.Services.Interfaces;

namespace MobyPark.Controllers;

[ApiController]
[Route("api/BusinessParking")]
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
    public async Task<IActionResult> CreateBusinessRegistrationAdmin([FromBody]CreateBusinessRegAdminDto bReg)
    {
        var result = await _registrationService.CreateBusinessRegistrationAdminAsync(bReg);
        return FromServiceResult(result);
    }
    
    [HttpPost("self")]
    public async Task<IActionResult> CreateBusinessRegistration(CreateBusinessRegDto bReg)
    {
        long currentUserId = long.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var result = await _registrationService.CreateBusinessRegistrationAsync(bReg, currentUserId);
        return FromServiceResult(result);
    }
    
    [HttpPatch]
    [Authorize("CanManageBusinesses")]
    public async Task<IActionResult> SetBusinessRegistrationActiveAdmin(PatchBusinessRegDto bReg)
    {
        var result = await _registrationService.SetBusinessRegistrationActiveAdminAsync(bReg);
        return FromServiceResult(result);
    }
    
    [HttpPatch("self")]
    public async Task<IActionResult> SetBusinessRegistrationActive(
        PatchBusinessRegDto bReg)
    {
        long currentUserId = long.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var result = await _registrationService.SetBusinessRegistrationActiveAsync(bReg, currentUserId);
        return FromServiceResult(result);
    }
    
    [HttpDelete("{id:long}")]
    [Authorize("CanManageBusinesses")]
    public async Task<IActionResult> AdminDeleteBusinessRegistration(
        long id)
    {
        var result = await _registrationService.AdminDeleteBusinessRegistrationAsync(id);
        return FromServiceResult(result);
    }
    
    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetBusinessRegistrationById(long id)
    {
        var result = await _registrationService.GetBusinessRegistrationByIdAsync(id);
        return FromServiceResult(result);
    }
    
    [HttpGet("business/{id:long}")]
    public async Task<IActionResult> GetBusinessRegistrationsByBusiness(long id)
    {
        var result = await _registrationService.GetBusinessRegistrationsByBusinessAsync(id);
        return FromServiceResult(result);
    }
    
    [HttpGet("{licensePlate}")]
    public async Task<IActionResult> GetBusinessRegistrationByLicensePlate(string licensePlate)
    {
        var result = await _registrationService.GetBusinessRegistrationByLicensePlateAsync(licensePlate);
        return FromServiceResult(result);
    }
    
    [HttpGet("active/{licensePlate}")]
    public async Task<IActionResult> GetActiveBusinessRegistrationByLicencePlate(string licensePlate)
    {
        var result = await _registrationService.GetActiveBusinessRegistrationByLicencePlateAsync(licensePlate);
        return FromServiceResult(result);
    }

}