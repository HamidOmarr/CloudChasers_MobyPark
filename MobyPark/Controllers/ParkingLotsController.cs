using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MobyPark.DTOs.ParkingLot.Request;
using MobyPark.Models;
using MobyPark.Services;
using MobyPark.Services.Interfaces;
using MobyPark.Services.Results;

namespace MobyPark.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class ParkingLotsController : BaseController
{
    private readonly IParkingLotService _parkingService;
    private readonly IAuthorizationService _authorizationService;

    public ParkingLotsController(UserService users, IParkingLotService parkingService, IAuthorizationService authorizationService) : base(users)
    {
        _parkingService = parkingService;
        _authorizationService = authorizationService;
    }

    [Authorize(Policy = "CanManageParkingLot")]
    [HttpPost]
    public async Task<IActionResult> CreateParkingLot([FromBody] CreateParkingLotDto parkingLot)
    {
        var result = await _parkingService.CreateParkingLotAsync(parkingLot);
        return result.Status switch
        {
            ServiceStatus.Success   => CreatedAtAction(nameof(GetParkingLotById), new { id = result.Data!.Id }, result.Data),
            ServiceStatus.NotFound  => NotFound(result.Error),
            ServiceStatus.BadRequest => BadRequest(result.Error),
            ServiceStatus.Fail      => Conflict(result.Error),
            ServiceStatus.Exception => StatusCode(500, result.Error),
            _ => BadRequest("Unknown error")
        };
    }
    
    [HttpGet("{lotId:long}")]
    public async Task<IActionResult> GetParkingLotById(long lotId)
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

    [Authorize(Policy = "CanManageParkingLot")]
    [HttpPatch("by-id/{lotId:long}")]
    public async Task<IActionResult> PatchParkingLotById(long lotId, [FromBody] PatchParkingLotDto lot)
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
    
    [Authorize(Policy = "CanManageParkingLot")]
    [HttpPatch("by-address")]
    public async Task<IActionResult> PatchParkingLotByAddress([FromQuery] string address, [FromBody] PatchParkingLotDto lot)
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

    [Authorize(Policy = "CanManageParkingLot")]
    [HttpDelete("by-id/{lotId:long}")]
    public async Task<IActionResult> DeleteParkingLotById(long lotId)
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
    
    [Authorize(Policy = "CanManageParkingLot")]
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
