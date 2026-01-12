using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using MobyPark.DTOs.Role.Request;
using MobyPark.DTOs.RolePermission.Request;
using MobyPark.Services.Interfaces;
using MobyPark.Services.Results.Role;
using MobyPark.Services.Results.RolePermission;

namespace MobyPark.Controllers;

[ApiController]
[Authorize]  // TODO: Add minimum authorization policy, related to role viewing, with management rights as higher level where needed
[Route("api/[controller]")]
public class RolesController : ControllerBase
{
    private readonly IRoleService _roles;
    private readonly IRolePermissionService _rolePermissions;

    public RolesController(IRoleService roles, IRolePermissionService rPermissions)
    {
        _roles = roles;
        _rolePermissions = rPermissions;
    }

    [HttpPost]
    [Authorize(Policy = "CanManageConfig")]  // Should be ManageRoles or similar once created in the role and permission system
    public async Task<IActionResult> Create([FromBody] CreateRoleDto request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var result = await _roles.CreateRole(request);

        return result switch
        {
            CreateRoleResult.Success success =>
                CreatedAtAction(nameof(GetById),
                    new { roleId = success.Role.Id },
                    success.Role),
            CreateRoleResult.AlreadyExists => Conflict(new { error = "Role with the same name already exists." }),
            CreateRoleResult.Error err => StatusCode(StatusCodes.Status500InternalServerError, new { error = err.Message }),
            _ => StatusCode(StatusCodes.Status500InternalServerError, new { error = "An unknown role creation error occurred." })
        };
    }

    [HttpGet("{roleId}")]
    public async Task<IActionResult> GetById(long roleId)
    {
        var result = await _roles.GetRoleById(roleId);

        return result switch
        {
            GetRoleResult.Success success => Ok(success.Role),
            GetRoleResult.NotFound => NotFound(new { error = "Role not found." }),
            _ => StatusCode(StatusCodes.Status500InternalServerError, new { error = "An unknown error occurred while retrieving the role." })
        };
    }

    [HttpGet("name/{roleName}")]
    public async Task<IActionResult> GetByName(string roleName)
    {
        var result = await _roles.GetRoleByName(roleName);

        return result switch
        {
            GetRoleResult.Success success => Ok(success.Role),
            GetRoleResult.NotFound => NotFound(new { error = "Role not found." }),
            GetRoleResult.InvalidInput invalid => BadRequest(new { error = invalid.Message }),
            GetRoleResult.Error err => StatusCode(StatusCodes.Status500InternalServerError, new { error = err.Message }),
            _ => StatusCode(StatusCodes.Status500InternalServerError, new { error = "An unknown error occurred while retrieving the role." })
        };
    }

    [HttpPut("roleId")]
    [Authorize(Policy = "CanManageConfig")]  // Should be ManageRoles or similar once created in the role and permission system
    public async Task<IActionResult> Update(long roleId, [FromBody] UpdateRoleDto request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var result = await _roles.UpdateRole(roleId, request);

        return result switch
        {
            UpdateRoleResult.Success success => Ok(success.Role),
            UpdateRoleResult.NoChangesMade => Ok(new { status = "No changes were made to the role." }),
            UpdateRoleResult.NotFound => NotFound(new { error = "Role not found." }),
            UpdateRoleResult.Error err => StatusCode(StatusCodes.Status500InternalServerError, new { error = err.Message }),
            _ => StatusCode(StatusCodes.Status500InternalServerError, new { error = "An unknown error occurred while updating the role." })
        };
    }

    [HttpDelete("{roleId}")]
    [Authorize(Policy = "CanManageConfig")]  // Should be ManageRoles or similar once created in the role and permission system
    public async Task<IActionResult> Delete(long roleId)
    {
        var result = await _roles.DeleteRoleById(roleId);

        return result switch
        {
            DeleteRoleResult.Success => Ok(new { status = "Role deleted successfully." }),
            DeleteRoleResult.NotFound => NotFound(new { error = "Role not found." }),
            DeleteRoleResult.Error err => StatusCode(StatusCodes.Status500InternalServerError, new { error = err.Message }),
            _ => StatusCode(StatusCodes.Status500InternalServerError, new { error = "An unknown error occurred while deleting the role." })
        };
    }

    [HttpDelete("name/{roleName}")]
    [Authorize(Policy = "CanManageConfig")] // Should be ManageRoles or similar once created in the role and permission system
    public async Task<IActionResult> DeleteByName(string roleName)
    {
        var result = await _roles.DeleteRoleByName(roleName);

        return result switch
        {
            DeleteRoleResult.Success => Ok(new { status = "Role deleted successfully." }),
            DeleteRoleResult.NotFound => NotFound(new { error = "Role not found." }),
            DeleteRoleResult.Error err => StatusCode(StatusCodes.Status500InternalServerError, new { error = err.Message }),
            _ => StatusCode(StatusCodes.Status500InternalServerError, new { error = "An unknown error occurred while deleting the role." })
        };
    }

    [HttpGet("exists")]
    public async Task<IActionResult> CheckRoleExists([FromQuery] string checkBy, [FromQuery] string value)
    {
        var result = await _roles.RoleExists(checkBy, value);

        return result switch
        {
            RoleExistsResult.Exists => Ok(new { exists = true }),
            RoleExistsResult.NotExists => Ok(new { exists = false }),
            RoleExistsResult.InvalidInput invalid => BadRequest(new { error = invalid.Message }),
            _ => StatusCode(StatusCodes.Status500InternalServerError, new { error = "An unknown error occurred." })
        };
    }

    [HttpGet("{roleId}/permissions")]
    public async Task<IActionResult> GetPermissionsForRole(long roleId)
    {
        var result = await _rolePermissions.GetRolePermissionsByRoleId(roleId);

        return result switch
        {
            GetRolePermissionListResult.Success s => Ok(s.RolePermissions), // Consider returning PermissionModels instead?
            GetRolePermissionListResult.NotFound => NotFound(new { error = "No permissions found for this role." }),
            GetRolePermissionListResult.Error err => StatusCode(StatusCodes.Status500InternalServerError, new { error = err.Message }),
            _ => StatusCode(StatusCodes.Status500InternalServerError, new { error = "An unknown error occurred." })
        };
    }

    [HttpPost("{roleId}/permissions")]
    [Authorize(Policy = "CanManageConfig")]
    public async Task<IActionResult> AddPermissionToRole(long roleId, [FromBody] AddPermissionToRoleDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var result = await _rolePermissions.AddPermissionToRole(roleId, dto.PermissionId);

        return result switch
        {
            AddPermissionToRoleResult.Success s => Ok(s.RolePermission),
            AddPermissionToRoleResult.AlreadyAssigned => Conflict(new { error = "Permission is already assigned to this role." }),
            AddPermissionToRoleResult.RoleNotFound => NotFound(new { error = "Role not found." }),
            AddPermissionToRoleResult.PermissionNotFound => NotFound(new { error = "Permission not found." }),
            AddPermissionToRoleResult.Error err => StatusCode(StatusCodes.Status500InternalServerError, new { error = err.Message }),
            _ => StatusCode(StatusCodes.Status500InternalServerError, new { error = "An unknown error occurred." })
        };
    }

    [HttpDelete("{roleId}/permissions/{permissionId}")]
    [Authorize(Policy = "CanManageConfig")]
    public async Task<IActionResult> RemovePermissionFromRole(long roleId, long permissionId)
    {
        var result = await _rolePermissions.RemovePermissionFromRole(roleId, permissionId);

        return result switch
        {
            RemovePermissionFromRoleResult.Success => Ok(new { status = "Permission removed from role." }),
            RemovePermissionFromRoleResult.NotFound => NotFound(new { error = "Permission assignment not found for this role." }),
            RemovePermissionFromRoleResult.Forbidden forbidden => StatusCode(StatusCodes.Status403Forbidden, new { error = forbidden.Message }),
            RemovePermissionFromRoleResult.Error err => StatusCode(StatusCodes.Status500InternalServerError, new { error = err.Message }),
            _ => StatusCode(StatusCodes.Status500InternalServerError, new { error = "An unknown error occurred." })
        };
    }
}