using System.Security.Claims;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using MobyPark.DTOs.Hotel;
using MobyPark.Services.Interfaces;

using Swashbuckle.AspNetCore.Annotations;

namespace MobyPark.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class HotelPassesController : BaseController
{
    private readonly IHotelPassService _hotelPassService;

    public HotelPassesController(IUserService users, IHotelPassService hotelPassService) : base(users)
    {
        _hotelPassService = hotelPassService;
    }

    [HttpPost]
    [Authorize(Policy = "CanManageHotels")]
    [SwaggerOperation(Summary = "Creates a new hotel pass (Admin).")]
    [SwaggerResponse(200, "Pass created", typeof(ReadHotelPassDto))]
    [SwaggerResponse(400, "Invalid dates or no spots available")]
    [SwaggerResponse(409, "Pass overlaps with existing reservation")]
    public async Task<IActionResult> CreateHotelPass([FromBody] AdminCreateHotelPassDto pass)
    {
        var result = await _hotelPassService.CreateHotelPassAsync(pass);
        return FromServiceResult(result);
    }

    [HttpPost("self")]
    [Authorize(Policy = "CanManageHotelPasses")]
    [SwaggerOperation(Summary = "Creates a hotel pass for the logged-in hotelier's lot.")]
    [SwaggerResponse(200, "Pass created", typeof(ReadHotelPassDto))]
    [SwaggerResponse(400, "Invalid dates or no spots available")]
    [SwaggerResponse(404, "User, Hotel, or Parking Lot not found")]
    [SwaggerResponse(409, "User not authorized or Pass overlaps")]
    public async Task<IActionResult> CreateHotelPassAsHotel([FromBody] CreateHotelPassDto pass)
    {
        long currentUserId = GetCurrentUserId();
        var result = await _hotelPassService.CreateHotelPassAsync(pass, currentUserId);
        return FromServiceResult(result);
    }

    [HttpGet("{id:long}")]
    [SwaggerOperation(Summary = "Retrieves a hotel pass by ID.")]
    [SwaggerResponse(200, "Pass found", typeof(ReadHotelPassDto))]
    [SwaggerResponse(404, "Pass not found")]
    public async Task<IActionResult> GetHotelPassById([FromRoute] long id)
    {
        var result = await _hotelPassService.GetHotelPassByIdAsync(id);
        return FromServiceResult(result);
    }

    [HttpGet("parkinglot/{id:long}")]
    [SwaggerOperation(Summary = "Retrieves all passes for a specific parking lot.")]
    [SwaggerResponse(200, "List retrieved", typeof(List<ReadHotelPassDto>))]
    [SwaggerResponse(404, "Parking lot has no passes")]
    public async Task<IActionResult> GetHotelPassesByParkingLotId([FromRoute] long parkingLotId)
    {
        var result = await _hotelPassService.GetHotelPassesByParkingLotIdAsync(parkingLotId);
        return FromServiceResult(result);
    }

    [HttpGet("license-plate")]
    [SwaggerOperation(Summary = "Retrieves all passes associated with a license plate.")]
    [SwaggerResponse(200, "List retrieved", typeof(List<ReadHotelPassDto>))]
    [SwaggerResponse(404, "No passes found for this plate")]
    public async Task<IActionResult> GetHotelPassesByLicensePlateAsync([FromQuery] string plate)
    {
        var result = await _hotelPassService.GetHotelPassesByLicensePlateAsync(plate);
        return FromServiceResult(result);
    }

    [HttpGet("{parkingLotId:long}/license-plate")]
    [SwaggerOperation(Summary = "Retrieves the currently ACTIVE pass for a specific lot and plate.")]
    [SwaggerResponse(200, "Pass found", typeof(ReadHotelPassDto))]
    [SwaggerResponse(404, "No active pass found")]
    public async Task<IActionResult> GetActiveHotelPassByLicensePlateAndLotId(
        [FromRoute] long parkingLotId, [FromQuery] string plate)
    {
        var result = await _hotelPassService.GetActiveHotelPassByLicensePlateAndLotIdAsync(parkingLotId, plate);
        return FromServiceResult(result);
    }

    [HttpPatch]
    [Authorize(Policy = "CanManageHotels")] //deze policies nog updaten
    [SwaggerOperation(Summary = "Updates a hotel pass (Admin).")]
    [SwaggerResponse(200, "Pass updated", typeof(ReadHotelPassDto))]
    [SwaggerResponse(400, "Invalid dates or no spots available")]
    [SwaggerResponse(404, "Pass not found")]
    [SwaggerResponse(409, "New dates overlap with another reservation")]
    public async Task<IActionResult> PatchHotelPass([FromBody] PatchHotelPassDto pass)
    {
        var result = await _hotelPassService.PatchHotelPassAsync(pass);
        return FromServiceResult(result);
    }

    [HttpPatch("self")]
    [Authorize(Policy = "CanManageHotelPasses")]
    [SwaggerOperation(Summary = "Updates a hotel pass for the logged-in hotelier.")]
    [SwaggerResponse(200, "Pass updated", typeof(ReadHotelPassDto))]
    [SwaggerResponse(400, "Invalid dates or no spots")]
    [SwaggerResponse(403, "Forbidden (Pass belongs to another lot)")]
    [SwaggerResponse(404, "Pass not found")]
    [SwaggerResponse(409, "User not authorized or Overlap detected")]
    public async Task<IActionResult> PatchHotelPassAsHotel([FromBody] PatchHotelPassDto pass)
    {
        long currentUserId = GetCurrentUserId();
        var result = await _hotelPassService.PatchHotelPassAsync(pass, currentUserId);
        return FromServiceResult(result);
    }

    [HttpDelete("admin/{id:long}")]
    [Authorize(Policy = "CanManageHotels")]
    [SwaggerOperation(Summary = "Deletes a hotel pass (Admin).")]
    [SwaggerResponse(200, "Deleted successfully", typeof(bool))]
    [SwaggerResponse(404, "Pass not found")]
    public async Task<IActionResult> DeleteHotelPassById(long id)
    {
        var result = await _hotelPassService.DeleteHotelPassByIdAsync(id);
        return FromServiceResult(result);
    }

    [HttpDelete("self/{id:long}")]
    [Authorize(Policy = "CanManageHotelPasses")]
    [SwaggerOperation(Summary = "Deletes a hotel pass created by the logged-in hotelier.")]
    [SwaggerResponse(200, "Deleted successfully", typeof(bool))]
    [SwaggerResponse(403, "Forbidden (Pass belongs to another lot)")]
    [SwaggerResponse(404, "Pass not found")]
    [SwaggerResponse(409, "User not authorized to manage passes")]
    public async Task<IActionResult> DeleteHotelPassAsHotelById(long id)
    {
        long currentUserId = GetCurrentUserId();
        var result = await _hotelPassService.DeleteHotelPassByIdAsync(id, currentUserId);
        return FromServiceResult(result);
    }
}