using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MobyPark.Models;
using MobyPark.Services.Results.ParkingLot;
using MobyPark.Services.Services;

namespace MobyPark.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ParkingLotsController : BaseController
{
    private readonly ServiceStack _services;

    public ParkingLotsController(ServiceStack services) : base(services.Sessions)
    {
        _services = services;
    }

    // ADMIN ONLY

    [Authorize(Policy = "AdminOnly")]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ParkingLotModel parkingLot, CancellationToken cancelToken)
    {
        var result = await _services.ParkingLots.InsertParkingLotAsync(parkingLot);
        return result switch
            {
                RegisterResult.Success success => CreatedAtAction(
                    nameof(GetById), new { id = success.ParkingLot.Id }, success.ParkingLot),
                RegisterResult.AddressTaken => Conflict(new
                    { error = "Address already taken, update the existing lot." }),
                RegisterResult.InvalidData invalid => BadRequest(new { error = invalid.Message }),
                RegisterResult.Error error => Problem(error.Message, statusCode: 500),
                _ => Problem("Unknown error", statusCode: 500)
            };
    }

    [Authorize(Policy = "AdminOnly")]
    [HttpPut("by-id/{lotId:int}")]
    public async Task<IActionResult> UpdateById(int lotId, [FromBody] ParkingLotModel lot)
    {
        var result = await _services.ParkingLots.UpdateParkingLotByIDAsync(lot, lotId);
        return result switch
        {
            RegisterResult.Success => Ok(new { message = "Parking lot modified" }),
            RegisterResult.NotFound nf => NotFound(new { message = nf.Message }),
            RegisterResult.Error error => Problem(error.Message, statusCode: 500),
            _ => Problem("Unknown error", statusCode: 500)
        };
    }
    
    [Authorize(Policy = "AdminOnly")]
    [HttpPut("by-address/{address}")]
    public async Task<IActionResult> UpdateByAddress(string address, [FromBody] ParkingLotModel lot)
    {
        var result = await _services.ParkingLots.UpdateParkingLotByAddressAsync(lot, address);
        return result switch
        {
            RegisterResult.Success => Ok(new { message = "Parking lot modified" }),
            RegisterResult.NotFound nf => NotFound(new { message = nf.Message }),
            RegisterResult.Error error => Problem(error.Message, statusCode: 500),
            _ => Problem("Unknown error", statusCode: 500)
        };
    }

    [Authorize(Policy = "AdminOnly")]
    [HttpDelete("by-id/{lotId:int}")]
    public async Task<IActionResult> DeleteById(int lotId)
    {
        var result = await _services.ParkingLots.DeleteParkingLotByIDAsync(lotId);
        
        return result switch
        {
            RegisterResult.SuccessfullyDeleted => Ok(new { message = "Parking lot deleted" }),
            RegisterResult.NotFound nf => NotFound(new { message = nf.Message }),
            RegisterResult.Error error => Problem(error.Message, statusCode: 500),
            _ => Problem("Unknown error", statusCode: 500)
        };
    }
    
    [Authorize(Policy = "AdminOnly")]
    [HttpDelete("by-address/{address}")]
    public async Task<IActionResult> DeleteByAddress(string address)
    {
        var result = await _services.ParkingLots.DeleteParkingLotByAddressAsync(address);
        return result switch
        {
            RegisterResult.SuccessfullyDeleted => Ok(new { message = "Parking lot deleted" }),
            RegisterResult.NotFound nf => NotFound(new { message = nf.Message }),
            RegisterResult.Error error => Problem(error.Message, statusCode: 500),
            _ => Problem("Unknown error", statusCode: 500)
        };
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        return Ok();
    }

    [HttpGet("{lotId:int}")]
    public async Task<IActionResult> GetById(int lotId)
    {
        var lot = await _services.ParkingLots.GetParkingLotById(lotId);
        return lot is null ? NotFound() : Ok(lot);
    }
}
