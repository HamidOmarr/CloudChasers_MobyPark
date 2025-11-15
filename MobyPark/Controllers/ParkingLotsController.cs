using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MobyPark.DTOs.ParkingLot.Request;
using MobyPark.Models;
using MobyPark.Services;
using MobyPark.Services.Interfaces;
using MobyPark.Services.Results;
using MobyPark.Services.Results.ParkingLot;

namespace MobyPark.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ParkingLotsController : BaseController
{
    private readonly ParkingLotService _parkingService;
    private readonly IAuthorizationService _authorizationService;

    public ParkingLotsController(UserService users, ParkingLotService parkingService, IAuthorizationService authorizationService) : base(users)
    {
        _parkingService = parkingService;
        _authorizationService = authorizationService;
    }

    [Authorize(Policy = "AdminOnly")]
    [HttpPost]
    public async Task<IActionResult> CreateParkingLot([FromBody] CreateParkingLotDto parkingLot)
    {
        var result = await _parkingService.CreateParkingLotAsync(parkingLot);
        return result.Status switch
            {
                ServiceStatus.Success   => Ok(result.Data),
                ServiceStatus.NotFound  => NotFound(result.Error),
                ServiceStatus.BadRequest => BadRequest(result.Error),
                ServiceStatus.Fail      => Conflict(result.Error),
                ServiceStatus.Exception => StatusCode(500, result.Error),
                _ => BadRequest("Unknown error")
            };
    }
    
    [HttpGet("{lotId:int}")]
    public async Task<IActionResult> GetParkingLotById(int lotId)
    {
        var result = await _parkingService.GetParkingLotByIdAsync(lotId);
        return result.Status switch
        {
            ServiceStatus.Success   => Ok(result.Data),
            ServiceStatus.NotFound  => NotFound(result.Error),
            ServiceStatus.BadRequest => BadRequest(result.Error),
            ServiceStatus.Fail      => Conflict(result.Error),
            ServiceStatus.Exception => StatusCode(500, result.Error),
            _ => BadRequest("Unknown error")
        };
    }
    
    [HttpGet("by-address")]
    public async Task<IActionResult> GetParkingLotByAddress([FromQuery] string address)
    {
        var result = await _parkingService.GetParkingLotByAddressAsync(address);
        return result.Status switch
        {
            ServiceStatus.Success   => Ok(result.Data),
            ServiceStatus.NotFound  => NotFound(result.Error),
            ServiceStatus.BadRequest => BadRequest(result.Error),
            ServiceStatus.Fail      => Conflict(result.Error),
            ServiceStatus.Exception => StatusCode(500, result.Error),
            _ => BadRequest("Unknown error")
        };
    }

    [Authorize(Policy = "AdminOnly")]
    [HttpPatch("by-id/{lotId:int}")]
    public async Task<IActionResult> UpdateParkingLotById(int lotId, [FromBody] PatchParkingLotDto lot)
    {
        var result = await _parkingService.PatchParkingLotByIdAsync(lotId, lot);
        return result.Status switch
        {
            ServiceStatus.Success   => Ok(result.Data),
            ServiceStatus.NotFound  => NotFound(result.Error),
            ServiceStatus.BadRequest => BadRequest(result.Error),
            ServiceStatus.Fail      => Conflict(result.Error),
            ServiceStatus.Exception => StatusCode(500, result.Error),
            _ => BadRequest("Unknown error")
        };
    }
    
    [Authorize(Policy = "AdminOnly")]
    [HttpPatch("by-address")]
    public async Task<IActionResult> UpdateParkingLotByAddress([FromQuery] string address, [FromBody] PatchParkingLotDto lot)
    {
        var result = await _parkingService.PatchParkingLotByAddressAsync(address, lot);
        return result.Status switch
        {
            ServiceStatus.Success   => Ok(result.Data),
            ServiceStatus.NotFound  => NotFound(result.Error),
            ServiceStatus.BadRequest => BadRequest(result.Error),
            ServiceStatus.Fail      => Conflict(result.Error),
            ServiceStatus.Exception => StatusCode(500, result.Error),
            _ => BadRequest("Unknown error")
        };
    }

    [Authorize(Policy = "AdminOnly")]
    [HttpDelete("by-id/{lotId:int}")]
    public async Task<IActionResult> DeleteParkingLotById(int lotId)
    {
        var result = await _parkingService.DeleteParkingLotByIdAsync(lotId);
        return result.Status switch
        {
            ServiceStatus.Success   => Ok(result.Data),
            ServiceStatus.NotFound  => NotFound(result.Error),
            ServiceStatus.BadRequest => BadRequest(result.Error),
            ServiceStatus.Fail      => Conflict(result.Error),
            ServiceStatus.Exception => StatusCode(500, result.Error),
            _ => BadRequest("Unknown error")
        };
    }
    
    [Authorize(Policy = "AdminOnly")]
    [HttpDelete("by-address/{address}")]
    public async Task<IActionResult> DeleteByAddress(string address)
    {
        var result = await _parkingService.DeleteParkingLotByAddressAsync(address);
        return result.Status switch
        {
            ServiceStatus.Success   => Ok(result.Data),
            ServiceStatus.NotFound  => NotFound(result.Error),
            ServiceStatus.BadRequest => BadRequest(result.Error),
            ServiceStatus.Fail      => Conflict(result.Error),
            ServiceStatus.Exception => StatusCode(500, result.Error),
            _ => BadRequest("Unknown error")
        };
    }

    [HttpGet("all")]
    public async Task<IActionResult> GetAll()
    {
        var result = await _parkingService.GetAllParkingLotsAsync();
        return result.Status switch
        {
            ServiceStatus.Success   => Ok(result.Data),
            ServiceStatus.NotFound  => NotFound(result.Error),
            ServiceStatus.BadRequest => BadRequest(result.Error),
            ServiceStatus.Fail      => Conflict(result.Error),
            ServiceStatus.Exception => StatusCode(500, result.Error),
            _ => BadRequest("Unknown error")
        };
    }
}
