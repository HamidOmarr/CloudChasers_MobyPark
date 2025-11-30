using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MobyPark.DTOs.LicensePlate.Request;
using MobyPark.Services.Interfaces;
using MobyPark.Services.Results;
using MobyPark.Services.Results.LicensePlate;
using MobyPark.Services.Results.User;
using MobyPark.Services.Results.UserPlate;

namespace MobyPark.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LicensePlateController : BaseController
{
    private readonly IUserService _users;
    private readonly ILicensePlateService _licensePlates;
    private readonly IUserPlateService _userPlates;
    private readonly IParkingLotService _parkingLots;
    private readonly IAuthorizationService _authorizationService;

    public LicensePlateController(
        IUserService users,
        ILicensePlateService licensePlates,
        IUserPlateService userPlates,
        IParkingLotService lots,
        IAuthorizationService authorization) : base(users)
    {
        _users = users;
        _licensePlates = licensePlates;
        _userPlates = userPlates;
        _parkingLots = lots;
        _authorizationService = authorization;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateLicensePlateDto request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var user = await GetCurrentUserAsync();
        if (string.IsNullOrEmpty(request.LicensePlate))
            return BadRequest(new { error = "Required field missing" });

        var existingLicensePlate = await _licensePlates.GetByLicensePlate(request.LicensePlate);
        if (existingLicensePlate is not GetLicensePlateResult.NotFound)
            return Conflict( new { error = "License plate already exists" });

        var licensePlate = await _licensePlates.CreateLicensePlate(request);
        if (licensePlate is not CreateLicensePlateResult.Success success)
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Failed to create license plate" });

        await _userPlates.AddLicensePlateToUser(user.Id, success.plate.LicensePlateNumber);

        return StatusCode(201, new { status = "Success", licensePlate });
    }

    [Authorize]
    [HttpGet("{licensePlate}/entry")]
    public async Task<IActionResult> GetLicensePlateEntry([FromBody] EnterParkingLotDto request)
    {
        var user = await GetCurrentUserAsync();
        var userPlate = await _userPlates.GetUserPlateByUserIdAndPlate(user.Id, request.LicensePlate);

        if (userPlate is not GetUserPlateResult.Success successUserPlate)
            return NotFound(new { error = "License plate does not exist", data = request.LicensePlate });

        var lot = await _parkingLots.GetParkingLotByIdAsync((int)request.ParkingLotId);
        if (lot.Status is ServiceStatus.NotFound)
            return NotFound(new { error = "Parking lot does not exist", data = request.ParkingLotId });
        var availableSpots = await _parkingLots.GetAvailableSpotsByLotIdAsync(lot.Data!.Id);
        if (availableSpots.Status is not ServiceStatus.Success || availableSpots.Data <= 0)
            return Conflict(new { error = "No available spots in the parking lot", data = request.ParkingLotId });

        return Ok(new { status = "Accepted", plate = new { successUserPlate.Plate.LicensePlateNumber, successUserPlate.Plate.UserId } });
    }

    [Authorize]
    [HttpPut("{licensePlate}")]
    public async Task<IActionResult> UpdateLicensePlate([FromBody] UpdateLicensePlateRequest request)
    {
        var user = await GetCurrentUserAsync();
        var userPlates = await _userPlates.GetUserPlatesByUserId(user.Id);

        if (userPlates is not GetUserPlateListResult.Success successUserPlates)
            return NotFound(new { error = "No license plates found for user" });

        var existingPlate = successUserPlates.Plates
            .FirstOrDefault(uPlate => uPlate.LicensePlateNumber == request.OldLicensePlate);
        if (existingPlate is null) return NotFound(new { error = "Old license plate not found for user" });

        var newPlate = successUserPlates.Plates.FirstOrDefault(uPlate => uPlate.LicensePlateNumber == request.NewLicensePlate);
        if (newPlate is not null) return Conflict(new { error = "New license plate already exists for user" });

        existingPlate.LicensePlateNumber = request.NewLicensePlate;
        var updateResult = await _userPlates.UpdateUserPlate(existingPlate);

        return updateResult is not UpdateUserPlateResult.Success
            ? StatusCode(StatusCodes.Status500InternalServerError, new { error = "Failed to update license plate" })
            : Ok(new { status = "Success", updatedPlate = existingPlate });
    }

    [Authorize]
    [HttpDelete("{licensePlate}")]
    public async Task<IActionResult> DeleteLicensePlate(string licensePlate)
    {
        var user = await GetCurrentUserAsync();
        var userPlateResult = await _userPlates.GetUserPlateByUserIdAndPlate(user.Id, licensePlate);

        if (userPlateResult is not GetUserPlateResult.Success)
            return NotFound(new { error = "License plate not found for user" });

        var deleteResult = await _userPlates.RemoveUserPlate(user.Id, licensePlate);

        return deleteResult switch
        {
            DeleteUserPlateResult.Success => Ok(new { status = "Deleted" }),
            DeleteUserPlateResult.NotFound => NotFound(new { error = "License plate not found" }),
            DeleteUserPlateResult.InvalidOperation op => BadRequest(new { error = op.Message }),
            DeleteUserPlateResult.Error err => StatusCode(StatusCodes.Status500InternalServerError, new { error = err.Message }),
            _ => StatusCode(StatusCodes.Status500InternalServerError, new { error = "Unexpected error" })
        };
    }

    [Authorize]
    [HttpGet("{licensePlate}")]
    public async Task<IActionResult> GetLicensePlateDetails(string licensePlate)
    {
        var plate = await _licensePlates.GetByLicensePlate(licensePlate);
        if (plate is GetLicensePlateResult.NotFound)
            return NotFound(new { error = "License plate not found" });

        return Ok(new { status = "Success", plate });
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> GetUserLicensePlates([FromQuery] string? username = null)
    {
        long targetUserId;

        if (!string.IsNullOrEmpty(username))
        {
            var authResult = await _authorizationService.AuthorizeAsync(User, "CanManagePlates");
            if (!authResult.Succeeded)
                return Forbid("You do not have permission to view another user's license plates.");

            var targetUser = await _users.GetUserByUsername(username);

            if (targetUser is not GetUserResult.Success successTargetUser)
                return NotFound(new { error = "User not found" });

            targetUserId = successTargetUser.User.Id;
        }
        else
            targetUserId = GetCurrentUserId();

        var userPlatesResult = await _userPlates.GetUserPlatesByUserId(targetUserId);

        if (userPlatesResult is not GetUserPlateListResult.Success successUserPlates)
            return NotFound(new { error = "No license plates found for this user." });

        return Ok(successUserPlates.Plates);
    }
}
