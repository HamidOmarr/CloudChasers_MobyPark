using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MobyPark.DTOs.Hotel;
using MobyPark.Services;
using MobyPark.Services.Results;

namespace MobyPark.Controllers;

[ApiController]
[Authorize(Policy = "CanManageHotelPasses")]
[Route("api/[controller]")]
public class HotelPassesController : BaseController
{
    //alleen voor hotels dus moet role checken
    private readonly HotelPassService _hotelService;
    private readonly IAuthorizationService _authorizationService;
    
    public HotelPassesController(UserService users, HotelPassService hotelService, IAuthorizationService authorizationService) : base(users)
    {
        _hotelService = hotelService;
        _authorizationService = authorizationService;
    }

    [HttpPost]
    public async Task<IActionResult> CreateHotelPass([FromBody] CreateHotelPassDto pass)
    {
        var result = await _hotelService.CreateHotelPassAsync(pass);
        return result.Status switch
        {
            ServiceStatus.Success   => CreatedAtAction(nameof(GetHotelPassById), new { id = result.Data!.Id }, result.Data),
            ServiceStatus.NotFound  => NotFound(result.Error),
            ServiceStatus.BadRequest => BadRequest(result.Error),
            ServiceStatus.Fail      => Conflict(result.Error),
            ServiceStatus.Exception => StatusCode(500, result.Error),
            _ => BadRequest("Unknown error")
        };
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetHotelPassById([FromRoute] long id)
    {
        var result = await _hotelService.GetHotelPassByIdAsync(id);
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

    [HttpGet("parkinglot/{id:long}")]
    public async Task<IActionResult> GetHotelPassesByParkingLotId([FromRoute] long parkingLotId)
    {
        var result = await _hotelService.GetHotelPassesByParkingLotIdAsync(parkingLotId);
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

    [HttpGet("license-plate")]
    public async Task<IActionResult> GetHotelPassesByLicensePlateAsync([FromQuery] string plate)
    {
        var result = await _hotelService.GetHotelPassesByLicensePlateAsync(plate);
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

    [HttpGet("{id:long}/license-plate")]
    public async Task<IActionResult> GetActiveHotelPassByLicensePlateAndLotId([FromRoute] long id,
        [FromQuery] string plate)
    {
        var result = await _hotelService.GetActiveHotelPassByLicensePlateAndLotIdAsync(id, plate);
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

    [HttpPatch]
    public async Task<IActionResult> PatchHotelPass([FromBody] PatchHotelPassDto pass)
    {
        var result = await _hotelService.PatchHotelPassAsync(pass);
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

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> DeleteHotelPassById(long id)
    {
        var result = await _hotelService.DeleteHotelPassByIdAsync(id);
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