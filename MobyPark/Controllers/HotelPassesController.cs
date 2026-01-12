using System.Security.Claims;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using MobyPark.DTOs.Hotel;
using MobyPark.Services.Interfaces;
using MobyPark.Services.Results;

namespace MobyPark.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HotelPassesController : BaseController
{
    private readonly IHotelPassService _hotelPassService;

    public HotelPassesController(IUserService users, IHotelPassService hotelPassService) : base(users)
    {
        _hotelPassService = hotelPassService;
    }

    [HttpPost]
    [Authorize(Policy = "CanManageHotels")]
    public async Task<IActionResult> CreateHotelPass([FromBody] AdminCreateHotelPassDto pass)
    {
        var result = await _hotelPassService.CreateHotelPassAsync(pass);
        return result.Status switch
        {
            ServiceStatus.Success => CreatedAtAction(nameof(GetHotelPassById), new { id = result.Data!.Id }, result.Data),
            ServiceStatus.NotFound => NotFound(result.Error),
            ServiceStatus.BadRequest => BadRequest(result.Error),
            ServiceStatus.Fail => Conflict(result.Error),
            ServiceStatus.Exception => StatusCode(500, result.Error),
            _ => BadRequest("Unknown error")
        };
    }

    [HttpPost("self")]
    [Authorize(Policy = "CanManageHotelPasses")]
    public async Task<IActionResult> CreateHotelPassAsHotel([FromBody] CreateHotelPassDto pass)
    {
        long currentUserId = long.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var result = await _hotelPassService.CreateHotelPassAsync(pass, currentUserId);
        return result.Status switch
        {
            ServiceStatus.Success => CreatedAtAction(nameof(GetHotelPassById), new { id = result.Data!.Id }, result.Data),
            ServiceStatus.NotFound => NotFound(result.Error),
            ServiceStatus.BadRequest => BadRequest(result.Error),
            ServiceStatus.Fail => Conflict(result.Error),
            ServiceStatus.Exception => StatusCode(500, result.Error),
            _ => BadRequest("Unknown error")
        };
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetHotelPassById([FromRoute] long id)
    {
        var result = await _hotelPassService.GetHotelPassByIdAsync(id);
        return result.Status switch
        {
            ServiceStatus.Success => Ok(result.Data),
            ServiceStatus.NotFound => NotFound(result.Error),
            ServiceStatus.BadRequest => BadRequest(result.Error),
            ServiceStatus.Fail => Conflict(result.Error),
            ServiceStatus.Exception => StatusCode(500, result.Error),
            _ => BadRequest("Unknown error")
        };
    }

    [HttpGet("parkinglot/{id:long}")]
    public async Task<IActionResult> GetHotelPassesByParkingLotId([FromRoute] long parkingLotId)
    {
        var result = await _hotelPassService.GetHotelPassesByParkingLotIdAsync(parkingLotId);
        return result.Status switch
        {
            ServiceStatus.Success => Ok(result.Data),
            ServiceStatus.NotFound => NotFound(result.Error),
            ServiceStatus.BadRequest => BadRequest(result.Error),
            ServiceStatus.Fail => Conflict(result.Error),
            ServiceStatus.Exception => StatusCode(500, result.Error),
            _ => BadRequest("Unknown error")
        };
    }

    [HttpGet("license-plate")]
    public async Task<IActionResult> GetHotelPassesByLicensePlateAsync([FromQuery] string plate)
    {
        var result = await _hotelPassService.GetHotelPassesByLicensePlateAsync(plate);
        return result.Status switch
        {
            ServiceStatus.Success => Ok(result.Data),
            ServiceStatus.NotFound => NotFound(result.Error),
            ServiceStatus.BadRequest => BadRequest(result.Error),
            ServiceStatus.Fail => Conflict(result.Error),
            ServiceStatus.Exception => StatusCode(500, result.Error),
            _ => BadRequest("Unknown error")
        };
    }

    [HttpGet("{id:long}/license-plate")]
    public async Task<IActionResult> GetActiveHotelPassByLicensePlateAndLotId([FromRoute] long id,
        [FromQuery] string plate)
    {
        var result = await _hotelPassService.GetActiveHotelPassByLicensePlateAndLotIdAsync(id, plate);
        return result.Status switch
        {
            ServiceStatus.Success => Ok(result.Data),
            ServiceStatus.NotFound => NotFound(result.Error),
            ServiceStatus.BadRequest => BadRequest(result.Error),
            ServiceStatus.Fail => Conflict(result.Error),
            ServiceStatus.Exception => StatusCode(500, result.Error),
            _ => BadRequest("Unknown error")
        };
    }

    [HttpPatch]
    [Authorize(Policy = "CanManageHotels")] //deze policies nog updaten
    public async Task<IActionResult> PatchHotelPass([FromBody] PatchHotelPassDto pass)
    {
        var result = await _hotelPassService.PatchHotelPassAsync(pass);
        return result.Status switch
        {
            ServiceStatus.Success => Ok(result.Data),
            ServiceStatus.NotFound => NotFound(result.Error),
            ServiceStatus.BadRequest => BadRequest(result.Error),
            ServiceStatus.Fail => Conflict(result.Error),
            ServiceStatus.Exception => StatusCode(500, result.Error),
            _ => BadRequest("Unknown error")
        };
    }

    [HttpPatch("self")]
    [Authorize(Policy = "CanManageHotelPasses")]
    public async Task<IActionResult> PatchHotelPassAsHotel([FromBody] PatchHotelPassDto pass)
    {
        long currentUserId = long.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var result = await _hotelPassService.PatchHotelPassAsync(pass, currentUserId);
        return result.Status switch
        {
            ServiceStatus.Success => Ok(result.Data),
            ServiceStatus.NotFound => NotFound(result.Error),
            ServiceStatus.BadRequest => BadRequest(result.Error),
            ServiceStatus.Fail => Conflict(result.Error),
            ServiceStatus.Exception => StatusCode(500, result.Error),
            ServiceStatus.Forbidden => StatusCode(403, result.Error),
            _ => BadRequest("Unknown error")
        };
    }

    [HttpDelete("admin/{id:long}")]
    [Authorize(Policy = "CanManageHotels")]
    public async Task<IActionResult> DeleteHotelPassById(long id)
    {
        var result = await _hotelPassService.DeleteHotelPassByIdAsync(id);
        return result.Status switch
        {
            ServiceStatus.Success => Ok(result.Data),
            ServiceStatus.NotFound => NotFound(result.Error),
            ServiceStatus.BadRequest => BadRequest(result.Error),
            ServiceStatus.Fail => Conflict(result.Error),
            ServiceStatus.Exception => StatusCode(500, result.Error),
            _ => BadRequest("Unknown error")
        };
    }

    [HttpDelete("self/{id:long}")]
    [Authorize(Policy = "CanManageHotelPasses")]
    public async Task<IActionResult> DeleteHotelPassAsHotelById(long id)
    {
        long currentUserId = long.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var result = await _hotelPassService.DeleteHotelPassByIdAsync(id, currentUserId);
        return result.Status switch
        {
            ServiceStatus.Success => Ok(result.Data),
            ServiceStatus.NotFound => NotFound(result.Error),
            ServiceStatus.BadRequest => BadRequest(result.Error),
            ServiceStatus.Fail => Conflict(result.Error),
            ServiceStatus.Exception => StatusCode(500, result.Error),
            ServiceStatus.Forbidden => StatusCode(403, result.Error),
            _ => BadRequest("Unknown error")
        };
    }
}