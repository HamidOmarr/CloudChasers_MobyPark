using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using MobyPark.DTOs.LicensePlate.Request;
using MobyPark.DTOs.LicensePlate.Response;
using MobyPark.DTOs.Shared;
using MobyPark.Services.Interfaces;
using MobyPark.Services.Results;
using MobyPark.Services.Results.LicensePlate;
using MobyPark.Services.Results.User;
using MobyPark.Services.Results.UserPlate;

using Swashbuckle.AspNetCore.Annotations;

namespace MobyPark.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
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
    [SwaggerOperation(Summary = "Registers a new license plate for the current user.")]
    [SwaggerResponse(201, "License plate created", typeof(ReadLicensePlateDto))]
    [SwaggerResponse(409, "License plate already exists")]
    public async Task<IActionResult> Create([FromBody] CreateLicensePlateDto request)
    {
        var user = await GetCurrentUserAsync();

        var existingLicensePlate = await _licensePlates.GetByLicensePlate(request.LicensePlate);
        if (existingLicensePlate is not GetLicensePlateResult.NotFound)
            return Conflict(new ErrorResponseDto { Error = "License plate already exists" });

        var licensePlate = await _licensePlates.CreateLicensePlate(request);
        if (licensePlate is not CreateLicensePlateResult.Success success)
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ErrorResponseDto { Error = "Failed to create license plate" });

        await _userPlates.AddLicensePlateToUser(user.Id, success.plate.LicensePlateNumber);

        return StatusCode(201, new ReadLicensePlateDto
        {
            Status = "Success",
            LicensePlate = success.plate.LicensePlateNumber,
            UserId = user.Id
        });
    }

    [Authorize]
    [HttpGet("{licensePlate}/entry")]
    [SwaggerOperation(Summary = "Checks if a license plate is allowed to enter a parking lot.")]
    [SwaggerResponse(200, "Entry accepted", typeof(ReadLicensePlateDto))]
    [SwaggerResponse(404, "License plate or Parking lot not found")]
    [SwaggerResponse(409, "No spots available")]
    public async Task<IActionResult> GetLicensePlateEntry(
        [FromRoute] string licensePlate, [FromQuery] long parkingLotId)
    {
        var user = await GetCurrentUserAsync();
        var userPlate = await _userPlates.GetUserPlateByUserIdAndPlate(user.Id, licensePlate);

        if (userPlate is not GetUserPlateResult.Success successUserPlate)
            return NotFound(new ErrorResponseDto { Error = "License plate does not exist for this user", Data = licensePlate });

        var lot = await _parkingLots.GetParkingLotByIdAsync(parkingLotId);
        if (lot.Status is ServiceStatus.NotFound)
            return NotFound(new ErrorResponseDto { Error = "Parking lot does not exist", Data = parkingLotId });

        var availableSpots = await _parkingLots.GetAvailableSpotsByLotIdAsync(lot.Data!.Id);
        if (availableSpots.Status is not ServiceStatus.Success || availableSpots.Data <= 0)
            return Conflict(new ErrorResponseDto { Error = "No available spots in the parking lot", Data = parkingLotId });

        return Ok(new ReadLicensePlateDto
        {
            Status = "Accepted",
            LicensePlate = successUserPlate.Plate.LicensePlateNumber,
            UserId = successUserPlate.Plate.UserId
        });
    }

    [Authorize]
    [HttpPut("{licensePlate}")]
    [SwaggerOperation(Summary = "Updates an existing license plate number.")]
    [SwaggerResponse(200, "Update successful", typeof(ReadLicensePlateDto))]
    [SwaggerResponse(404, "Old license plate not found")]
    [SwaggerResponse(409, "New license plate already exists")]
    public async Task<IActionResult> UpdateLicensePlate([FromRoute] string licensePlate, [FromBody] UpdateLicensePlateRequest request)
    {
        var user = await GetCurrentUserAsync();

        if (!string.Equals(licensePlate, request.OldLicensePlate, StringComparison.OrdinalIgnoreCase))
            return BadRequest(new ErrorResponseDto { Error = "Path license plate must match body OldLicensePlate" });

        var userPlates = await _userPlates.GetUserPlatesByUserId(user.Id);

        if (userPlates is not GetUserPlateListResult.Success successUserPlates)
            return NotFound(new ErrorResponseDto { Error = "No license plates found for user" });

        var existingPlate = successUserPlates.Plates
            .FirstOrDefault(uPlate => uPlate.LicensePlateNumber == request.OldLicensePlate);
        if (existingPlate is null) return NotFound(new ErrorResponseDto { Error = "Old license plate not found for user" });

        var newPlate = successUserPlates.Plates.FirstOrDefault(uPlate => uPlate.LicensePlateNumber == request.NewLicensePlate);
        if (newPlate is not null) return Conflict(new ErrorResponseDto { Error = "New license plate already exists for user" });

        existingPlate.LicensePlateNumber = request.NewLicensePlate;
        var updateResult = await _userPlates.UpdateUserPlate(existingPlate);

        return updateResult is not UpdateUserPlateResult.Success
            ? StatusCode(StatusCodes.Status500InternalServerError, new ErrorResponseDto { Error = "Failed to update license plate" })
            : Ok(new ReadLicensePlateDto
            {
                Status = "Success",
                LicensePlate = existingPlate.LicensePlateNumber,
                UserId = existingPlate.UserId
            });
    }

    [Authorize]
    [HttpDelete("{licensePlate}")]
    [SwaggerOperation(Summary = "Deletes a license plate.")]
    [SwaggerResponse(200, "Deleted successfully", typeof(bool))]
    [SwaggerResponse(404, "License plate not found")]
    public async Task<IActionResult> DeleteLicensePlate(string licensePlate)
    {
        var user = await GetCurrentUserAsync();
        var userPlateResult = await _userPlates.GetUserPlateByUserIdAndPlate(user.Id, licensePlate);

        if (userPlateResult is not GetUserPlateResult.Success)
            return NotFound(new ErrorResponseDto { Error = "License plate not found for user" });

        var deleteResult = await _userPlates.RemoveUserPlate(user.Id, licensePlate);

        return deleteResult switch
        {
            DeleteUserPlateResult.Success => Ok(true),
            DeleteUserPlateResult.NotFound => NotFound(new ErrorResponseDto { Error = "License plate not found" }),
            DeleteUserPlateResult.InvalidOperation op => BadRequest(new ErrorResponseDto { Error = op.Message }),
            DeleteUserPlateResult.Error err => StatusCode(StatusCodes.Status500InternalServerError, new ErrorResponseDto { Error = err.Message }),
            _ => StatusCode(StatusCodes.Status500InternalServerError, new ErrorResponseDto { Error = "Unexpected error" })
        };
    }

    [Authorize]
    [HttpGet("{licensePlate}")]
    [SwaggerOperation(Summary = "Retrieves details of a specific license plate.")]
    [SwaggerResponse(200, "Found plate", typeof(ReadLicensePlateDto))]
    [SwaggerResponse(404, "Not found")]
    public async Task<IActionResult> GetLicensePlateDetails(string licensePlate)
    {
        var user = await GetCurrentUserAsync();

        var plate = await _userPlates.GetUserPlateByUserIdAndPlate(user.Id, licensePlate);
        if (plate is not GetUserPlateResult.Success successPlate)
            return NotFound(new ErrorResponseDto { Error = "License plate not found for user" });

        return Ok(new ReadLicensePlateDto
        {
            Status = "Success",
            LicensePlate = successPlate.Plate.LicensePlateNumber,
            UserId = successPlate.Plate.UserId
        });
    }

    [Authorize]
    [HttpGet]
    [SwaggerOperation(Summary = "Retrieves all license plates for a user.")]
    [SwaggerResponse(200, "List retrieved", typeof(List<ReadLicensePlateDto>))]
    [SwaggerResponse(403, "Not authorized to view other users' plates")]
    [SwaggerResponse(404, "User not found")]
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
                return NotFound(new ErrorResponseDto { Error = "User not found" });

            targetUserId = successTargetUser.User.Id;
        }
        else
            targetUserId = GetCurrentUserId();

        var userPlatesResult = await _userPlates.GetUserPlatesByUserId(targetUserId);

        if (userPlatesResult is not GetUserPlateListResult.Success successUserPlates)
            return NotFound(new ErrorResponseDto { Error = "No license plates found for this user." });

        var plates = successUserPlates.Plates.Select(p => new ReadLicensePlateDto
        {
            LicensePlate = p.LicensePlateNumber,
            UserId = p.UserId
        }).ToList();

        return Ok(plates);
    }
}