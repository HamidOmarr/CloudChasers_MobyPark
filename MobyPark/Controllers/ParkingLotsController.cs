using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MobyPark.DTOs.ParkingLot.Request;
using MobyPark.Services;
using MobyPark.Services.Interfaces;
using MobyPark.Services.Results.ParkingLot;

namespace MobyPark.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ParkingLotsController : BaseController
{
    private readonly IParkingLotService _parkingLots;
    private readonly IAuthorizationService _authorizationService;

    public ParkingLotsController(UserService users, IParkingLotService parkingLots, IAuthorizationService authorizationService) : base(users)
    {
        _parkingLots = parkingLots;
        _authorizationService = authorizationService;
    }

    [Authorize(Policy = "CanManageParkingLots")]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateParkingLotDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var result = await _parkingLots.CreateParkingLot(dto);
        return result switch
        {
            CreateLotResult.Success s => StatusCode(201, s.Lot),
            CreateLotResult.Error e => StatusCode(StatusCodes.Status500InternalServerError, new { error = e.Message }),
            _ => StatusCode(StatusCodes.Status500InternalServerError, new { error = "An unknown error occurred." })
        };
    }

    [Authorize(Policy = "CanManageParkingLots")]
    [HttpPut("{lotId}")]
    public async Task<IActionResult> Update(int lotId, [FromBody] UpdateParkingLotDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var getResult = await _parkingLots.GetParkingLotById(lotId);
        if (getResult is not GetLotResult.Success success)
        {
            return getResult switch
            {
                GetLotResult.NotFound => NotFound(new { error = "Parking lot not found" }),
                _ => StatusCode(StatusCodes.Status500InternalServerError, new { error = "Failed to retrieve lot" })
            };
        }

        var existingLot = success.Lot;

        var updateResult = await _parkingLots.UpdateParkingLot(existingLot.Id, dto);
        return updateResult switch
        {
            UpdateLotResult.Success updated => Ok(updated.Lot),
            UpdateLotResult.NotFound => NotFound(new { error = "Parking lot not found (Concurrency issue)." }),
            UpdateLotResult.Error e => StatusCode(StatusCodes.Status500InternalServerError, new { error = e.Message }),
            _ => StatusCode(StatusCodes.Status500InternalServerError, new { error = "An unknown update error occurred." })
        };
    }

    [Authorize(Policy = "CanManageParkingLots")]
    [HttpDelete("{lotId}")]
    public async Task<IActionResult> Delete(int lotId)
    {
        var result = await _parkingLots.DeleteParkingLot(lotId);
        return result switch
        {
            DeleteLotResult.Success => Ok(new { status = "Deleted" }),
            DeleteLotResult.NotFound => NotFound(new { error = "Parking lot not found" }),
            DeleteLotResult.Error e => StatusCode(StatusCodes.Status500InternalServerError, new { error = e.Message }),
            _ => StatusCode(StatusCodes.Status500InternalServerError, new { error = "An unknown delete error occurred." })
        };
    }

    [Authorize(Policy = "CanManageParkingLots")]
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result = await _parkingLots.GetAllParkingLots();

        return result switch
        {
            GetLotListResult.Success s => Ok(s.Lots),
            GetLotListResult.NotFound => NotFound(new { error = "No parking lots found" }),
            _ => StatusCode(StatusCodes.Status500InternalServerError, new { error = "An unknown error occurred." })
        };
    }

    [Authorize(Policy = "CanReadParkingLots")]
    [HttpGet("{lotId}")]
    public async Task<IActionResult> GetById(int lotId)
    {
        var result = await _parkingLots.GetParkingLotById(lotId);

        if (result is not GetLotResult.Success success)
        {
            return result switch
            {
                GetLotResult.NotFound => NotFound(new { error = "Parking lot not found" }),
                GetLotResult.InvalidInput e => BadRequest(new { error = e.Message }),
                _ => StatusCode(StatusCodes.Status500InternalServerError, new { error = "An unknown error occurred." })
            };
        }

        var lot = success.Lot;
        var authorizationResult = await _authorizationService.AuthorizeAsync(User, "CanManageParkingLots");

        if (authorizationResult.Succeeded)
            return Ok(lot);

        return Ok(new
        {
            lot.Name,
            lot.Location,
            lot.Address,
            lot.Tariff,
            lot.DayTariff,
            spotsAvailable = lot.AvailableSpots
        });
    }
}
