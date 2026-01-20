using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using MobyPark.DTOs.ParkingLot.Request;
using MobyPark.Services.Interfaces;

using Swashbuckle.AspNetCore.Annotations;

namespace MobyPark.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
[Produces("application/json")]
public class ParkingLotsController : BaseController
{
    private readonly IParkingLotService _parkingService;

    public ParkingLotsController(IUserService users, IParkingLotService parkingService) : base(users)
    {
        _parkingService = parkingService;
    }

    [HttpPost]
    [Authorize(Policy = "CanManageParkingLot")]
    [SwaggerOperation(Summary = "Creates a new parking lot.")]
    [SwaggerResponse(200, "Parking lot created successfully", typeof(ReadParkingLotDto))]
    [SwaggerResponse(409, "Parking lot address already taken")]
    public async Task<IActionResult> CreateParkingLot([FromBody] CreateParkingLotDto parkingLot)
    {
        var result = await _parkingService.CreateParkingLotAsync(parkingLot);
        return FromServiceResult(result);
    }

    [HttpGet("{lotId:long}")]
    [SwaggerOperation(Summary = "Retrieves a parking lot by its ID.")]
    [SwaggerResponse(200, "Parking lot found", typeof(ReadParkingLotDto))]
    [SwaggerResponse(404, "Parking lot not found")]
    public async Task<IActionResult> GetParkingLotById(long lotId)
    {
        var result = await _parkingService.GetParkingLotByIdAsync(lotId);
        return FromServiceResult(result);
    }

    [HttpGet("by-address")]
    [SwaggerOperation(Summary = "Retrieves a parking lot by its address.")]
    [SwaggerResponse(200, "Parking lot found", typeof(ReadParkingLotDto))]
    [SwaggerResponse(404, "Parking lot not found")]
    public async Task<IActionResult> GetParkingLotByAddress([FromQuery] string address)
    {
        var result = await _parkingService.GetParkingLotByAddressAsync(address);
        return FromServiceResult(result);
    }

    [HttpPatch("by-id/{lotId:long}")]
    [Authorize(Policy = "CanManageParkingLot")]
    [SwaggerOperation(Summary = "Updates a parking lot by ID.")]
    [SwaggerResponse(200, "Update successful", typeof(ReadParkingLotDto))]
    [SwaggerResponse(400, "Invalid update data")]
    [SwaggerResponse(404, "Parking lot not found")]
    [SwaggerResponse(409, "New address conflicts with existing lot")]
    public async Task<IActionResult> PatchParkingLotById(long lotId, [FromBody] PatchParkingLotDto lot)
    {
        var result = await _parkingService.PatchParkingLotByIdAsync(lotId, lot);
        return FromServiceResult(result);
    }

    [HttpPatch("by-address")]
    [Authorize(Policy = "CanManageParkingLot")]
    [SwaggerOperation(Summary = "Updates a parking lot by address.")]
    [SwaggerResponse(200, "Update successful", typeof(ReadParkingLotDto))]
    [SwaggerResponse(400, "Invalid update data")]
    [SwaggerResponse(404, "Parking lot not found")]
    [SwaggerResponse(409, "New address conflicts with existing lot")]
    public async Task<IActionResult> PatchParkingLotByAddress([FromQuery] string address, [FromBody] PatchParkingLotDto lot)
    {
        var result = await _parkingService.PatchParkingLotByAddressAsync(address, lot);
        return FromServiceResult(result);
    }

    [HttpDelete("by-id/{lotId:long}")]
    [Authorize(Policy = "CanManageParkingLot")]
    [SwaggerOperation(Summary = "Deletes a parking lot by ID.")]
    [SwaggerResponse(200, "Deletion successful", typeof(bool))]
    [SwaggerResponse(404, "Parking lot not found")]
    public async Task<IActionResult> DeleteParkingLotById(long lotId)
    {
        var result = await _parkingService.DeleteParkingLotByIdAsync(lotId);
        return FromServiceResult(result);
    }

    [HttpDelete("by-address/{address}")]
    [Authorize(Policy = "CanManageParkingLot")]
    [SwaggerOperation(Summary = "Deletes a parking lot by address.")]
    [SwaggerResponse(200, "Deletion successful", typeof(bool))]
    [SwaggerResponse(404, "Parking lot not found")]
    public async Task<IActionResult> DeleteByAddress(string address)
    {
        var result = await _parkingService.DeleteParkingLotByAddressAsync(address);
        return FromServiceResult(result);
    }

    [HttpGet("all")]
    [SwaggerOperation(Summary = "Retrieves all registered parking lots.")]
    [SwaggerResponse(200, "List retrieved", typeof(List<ReadParkingLotDto>))]
    public async Task<IActionResult> GetAll()
    {
        var result = await _parkingService.GetAllParkingLotsAsync();
        return FromServiceResult(result);
    }
}