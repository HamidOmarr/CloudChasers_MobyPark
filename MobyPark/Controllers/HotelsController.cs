using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using MobyPark.DTOs.Hotel;
using MobyPark.Services;
using MobyPark.Services.Interfaces;

using Swashbuckle.AspNetCore.Annotations;

namespace MobyPark.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class HotelsController : BaseController
{
    private readonly IHotelService _hotelService;

    public HotelsController(IUserService users, IHotelService hotelService) : base(users)
    {
        _hotelService = hotelService;
    }

    [HttpPost]
    [Authorize(Policy = "CanManageHotels")]
    [SwaggerOperation(Summary = "Creates a new hotel.")]
    [SwaggerResponse(200, "Hotel created", typeof(ReadHotelDto))]
    [SwaggerResponse(404, "Parking lot not found")]
    [SwaggerResponse(409, "Address or Parking Lot already taken")]
    public async Task<IActionResult> CreateHotel([FromBody] CreateHotelDto hotel)
    {
        var result = await _hotelService.CreateHotelAsync(hotel);
        return FromServiceResult(result);
    }

    [HttpPatch]
    [Authorize(Policy = "CanManageHotels")]
    [SwaggerOperation(Summary = "Updates an existing hotel.")]
    [SwaggerResponse(200, "Hotel updated", typeof(PatchHotelDto))]
    [SwaggerResponse(404, "Hotel not found")]
    [SwaggerResponse(409, "New address or parking lot already taken")]
    public async Task<IActionResult> PatchHotel([FromBody] PatchHotelDto hotel)
    {
        var result = await _hotelService.PatchHotelAsync(hotel);
        return FromServiceResult(result);
    }

    [HttpDelete("{id:long}")]
    [Authorize(Policy = "CanManageHotels")]
    [SwaggerOperation(Summary = "Deletes a hotel by ID.")]
    [SwaggerResponse(200, "Deleted successfully", typeof(bool))]
    [SwaggerResponse(404, "Hotel not found")]
    public async Task<IActionResult> DeleteHotel([FromRoute] long id)
    {
        var result = await _hotelService.DeleteHotelAsync(id);
        return FromServiceResult(result);
    }

    [HttpGet]
    [SwaggerOperation(Summary = "Retrieves a list of all hotels.")]
    [SwaggerResponse(200, "List retrieved", typeof(List<ReadHotelDto>))]
    public async Task<IActionResult> GetAllHotels()
    {
        var result = await _hotelService.GetAllHotelsAsync();
        return FromServiceResult(result);
    }

    [HttpGet("{id:long}")]
    [SwaggerOperation(Summary = "Retrieves a hotel by ID.")]
    [SwaggerResponse(200, "Hotel found", typeof(ReadHotelDto))]
    [SwaggerResponse(404, "Hotel not found")]
    public async Task<IActionResult> GetHotelById([FromRoute] long id)
    {
        var result = await _hotelService.GetHotelByIdAsync(id);
        return FromServiceResult(result);
    }
}