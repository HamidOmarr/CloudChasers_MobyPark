using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MobyPark.DTOs.Hotel;
using MobyPark.Services;
using MobyPark.Services.Results;

namespace MobyPark.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HotelsController : BaseController
{
    private readonly IHotelService _hotelService;
    
    public HotelsController(UserService users, IHotelService hotelService) : base(users)
    {
        _hotelService = hotelService;
    }

    [HttpPost]
    [Authorize(Policy = "CanManageHotels")]
    public async Task<IActionResult> CreateHotel([FromBody] CreateHotelDto hotel)
    {
        var result = await _hotelService.CreateHotelAsync(hotel);
        return result.Status switch
        {
            ServiceStatus.Success   => CreatedAtAction(nameof(GetHotelById), new { id = result.Data!.Id }, result.Data),
            ServiceStatus.NotFound  => NotFound(result.Error),
            ServiceStatus.BadRequest => BadRequest(result.Error),
            ServiceStatus.Fail      => Conflict(result.Error),
            ServiceStatus.Exception => StatusCode(500, result.Error),
            _ => BadRequest("Unknown error")
        };
    }

    [HttpPatch]
    [Authorize(Policy = "CanManageHotels")]
    public async Task<IActionResult> PatchHotel([FromBody] PatchHotelDto hotel)
    {
        var result = await _hotelService.PatchHotelAsync(hotel);
        return FromServiceResult(result);
    }

    [HttpDelete("{id:long}")]
    [Authorize(Policy = "CanManageHotels")]
    public async Task<IActionResult> DeleteHotel([FromRoute] long id)
    {
        var result = await _hotelService.DeleteHotelAsync(id);
        return FromServiceResult(result);
    }

    [HttpGet]
    public async Task<IActionResult> GetAllHotels()
    {
        var result = await _hotelService.GetAllHotelsAsync();
        return FromServiceResult(result);
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetHotelById([FromRoute] long id)
    {
        var result = await _hotelService.GetHotelByIdAsync(id);
        return FromServiceResult(result);
    }
}
