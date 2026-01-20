using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using MobyPark.DTOs.Role.Request;
using MobyPark.DTOs.Role.Response;
using MobyPark.DTOs.RolePermission.Request;
using MobyPark.DTOs.Shared;
using MobyPark.Models;
using MobyPark.Services.Interfaces;
using MobyPark.Services.Results.Role;
using MobyPark.Services.Results.RolePermission;

using Swashbuckle.AspNetCore.Annotations;

namespace MobyPark.Controllers;

[ApiController]
[Authorize]  // TODO: Add minimum authorization policy, related to role viewing, with management rights as higher level where needed
[Route("api/[controller]")]
[Produces("application/json")]
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
    [SwaggerOperation(Summary = "Creates a new role.")]
    [SwaggerResponse(201, "Role created successfully", typeof(RoleModel))]
    [SwaggerResponse(400, "Invalid role data")]
    [SwaggerResponse(409, "Role with this name already exists")]
    public async Task<IActionResult> Create([FromBody] CreateRoleDto request)
    {
        var result = await _roles.CreateRole(request);

        return result switch
        {
            CreateRoleResult.Success success =>
                CreatedAtAction(nameof(GetById),
                    new StatusResponseDto { Message = success.Role.Id.ToString() },
                    success.Role),
            CreateRoleResult.AlreadyExists => Conflict(new ErrorResponseDto { Error = "Role with the same name already exists." }),
            CreateRoleResult.Error err => StatusCode(500, new ErrorResponseDto { Error = err.Message }),
            _ => StatusCode(500, new ErrorResponseDto { Error = "An unknown role creation error occurred." })
        };
    }

    [HttpGet("{roleId:long}")]
    [SwaggerOperation(Summary = "Retrieves a role by its unique ID.")]
    [SwaggerResponse(200, "Role found", typeof(RoleModel))]
    [SwaggerResponse(404, "Role not found")]
    public async Task<IActionResult> GetById(long roleId)
    {
        var result = await _roles.GetRoleById(roleId);

        return result switch
        {
            GetRoleResult.Success success => Ok(success.Role),
            GetRoleResult.NotFound => NotFound(new ErrorResponseDto { Error = "Role not found." }),
            _ => StatusCode(500, new ErrorResponseDto { Error = "An unknown error occurred while retrieving the role." })
        };
    }

    [HttpGet("name/{roleName}")]
    [SwaggerOperation(Summary = "Retrieves a role by its name.")]
    [SwaggerResponse(200, "Role found", typeof(RoleModel))]
    [SwaggerResponse(400, "Invalid name input")]
    [SwaggerResponse(404, "Role not found")]
    public async Task<IActionResult> GetByName(string roleName)
    {
        var result = await _roles.GetRoleByName(roleName);

        return result switch
        {
            GetRoleResult.Success success => Ok(success.Role),
            GetRoleResult.NotFound => NotFound(new ErrorResponseDto { Error = "Role not found." }),
            GetRoleResult.InvalidInput invalid => BadRequest(new ErrorResponseDto { Error = invalid.Message }),
            GetRoleResult.Error err => StatusCode(500, new ErrorResponseDto { Error = err.Message }),
            _ => StatusCode(500, new ErrorResponseDto { Error = "An unknown error occurred while retrieving the role." })
        };
    }

    [HttpPut("{roleId:long}")]
    [Authorize(Policy = "CanManageConfig")]
    [SwaggerOperation(Summary = "Updates an existing role.")]
    [SwaggerResponse(200, "Update successful", typeof(RoleModel))]
    [SwaggerResponse(400, "Invalid update data")]
    [SwaggerResponse(404, "Role not found")]
    public async Task<IActionResult> Update(long roleId, [FromBody] UpdateRoleDto request)
    {
        var result = await _roles.UpdateRole(roleId, request);

        return result switch
        {
            UpdateRoleResult.Success success => Ok(success.Role),
            UpdateRoleResult.NoChangesMade => Ok(new StatusResponseDto { Status = "No changes were made to the role." }),
            UpdateRoleResult.NotFound => NotFound(new ErrorResponseDto { Error = "Role not found." }),
            UpdateRoleResult.Error err => StatusCode(500, new ErrorResponseDto { Error = err.Message }),
            _ => StatusCode(500, new ErrorResponseDto { Error = "An unknown error occurred while updating the role." })
        };
    }

    [HttpDelete("{roleId}")]
    [Authorize(Policy = "CanManageConfig")]  // Should be ManageRoles or similar once created in the role and permission system
    [SwaggerOperation(Summary = "Deletes a role by ID.")]
    [SwaggerResponse(200, "Deleted successfully")]
    [SwaggerResponse(404, "Role not found")]
    [SwaggerResponse(409, "Cannot delete role (e.g., users assigned)")]
    public async Task<IActionResult> Delete(long roleId)
    {
        var result = await _roles.DeleteRoleById(roleId);

        return result switch
        {
            DeleteRoleResult.Success => Ok(new StatusResponseDto { Status = "Role deleted successfully." }),
            DeleteRoleResult.NotFound => NotFound(new ErrorResponseDto { Error = "Role not found." }),
            DeleteRoleResult.Conflict c => Conflict(new ErrorResponseDto { Error = c.Message }),
            DeleteRoleResult.Forbidden f => StatusCode(403, new ErrorResponseDto { Error = f.Message }),
            DeleteRoleResult.Error err => StatusCode(500, new ErrorResponseDto { Error = err.Message }),
            _ => StatusCode(500, new ErrorResponseDto { Error = "An unknown error occurred while deleting the role." })
        };
    }

    [HttpDelete("name/{roleName}")]
    [Authorize(Policy = "CanManageConfig")] // Should be ManageRoles or similar once created in the role and permission system
    [SwaggerOperation(Summary = "Deletes a role by name.")]
    [SwaggerResponse(200, "Deleted successfully")]
    [SwaggerResponse(403, "Forbidden to delete this role")]
    [SwaggerResponse(404, "Role not found")]
    [SwaggerResponse(409, "Cannot delete role (e.g., users assigned)")]
    public async Task<IActionResult> DeleteByName(string roleName)
    {
        var result = await _roles.DeleteRoleByName(roleName);

        return result switch
        {
            DeleteRoleResult.Success => Ok(new StatusResponseDto { Status = "Role deleted successfully." }),
            DeleteRoleResult.NotFound => NotFound(new ErrorResponseDto { Error = "Role not found." }),
            DeleteRoleResult.Conflict c => Conflict(new ErrorResponseDto { Error = c.Message }),
            DeleteRoleResult.Forbidden f => StatusCode(403, new ErrorResponseDto { Error = f.Message }),
            DeleteRoleResult.Error err => StatusCode(500, new ErrorResponseDto { Error = err.Message }),
            _ => StatusCode(500, new ErrorResponseDto { Error = "An unknown error occurred while deleting the role." })
        };
    }

    [HttpGet("exists")]
    [SwaggerOperation(Summary = "Checks if a role exists.")]
    [SwaggerResponse(200, "Check completed", typeof(RoleExistsResponseDto))]
    [SwaggerResponse(400, "Invalid search parameters")]
    public async Task<IActionResult> CheckRoleExists([FromQuery] string checkBy, [FromQuery] string value)
    {
        var result = await _roles.RoleExists(checkBy, value);

        return result switch
        {
            RoleExistsResult.Exists => Ok(new RoleExistsResponseDto { Exists = true }),
            RoleExistsResult.NotExists => Ok(new RoleExistsResponseDto { Exists = false }),
            RoleExistsResult.InvalidInput invalid => BadRequest(new ErrorResponseDto { Error = invalid.Message }),
            _ => StatusCode(500, new ErrorResponseDto { Error = "An unknown error occurred." })
        };
    }

    [HttpGet("{roleId:long}/permissions")]
    [SwaggerOperation(Summary = "Gets all permissions assigned to a role.")]
    [SwaggerResponse(200, "Permissions retrieved", typeof(List<RolePermissionModel>))]
    [SwaggerResponse(404, "Role or permissions not found")]
    public async Task<IActionResult> GetPermissionsForRole(long roleId)
    {
        var result = await _rolePermissions.GetRolePermissionsByRoleId(roleId);

        return result switch
        {
            GetRolePermissionListResult.Success s => Ok(s.RolePermissions),
            GetRolePermissionListResult.NotFound => NotFound(new ErrorResponseDto { Error = "No permissions found for this role." }),
            GetRolePermissionListResult.Error err => StatusCode(500, new ErrorResponseDto { Error = err.Message }),
            _ => StatusCode(500, new ErrorResponseDto { Error = "An unknown error occurred." })
        };
    }

    [HttpPost("{roleId:long}/permissions")]
    [Authorize(Policy = "CanManageConfig")]
    [SwaggerOperation(Summary = "Assigns a permission to a role.")]
    [SwaggerResponse(200, "Permission assigned", typeof(RolePermissionModel))]
    [SwaggerResponse(404, "Role or permission not found")]
    [SwaggerResponse(409, "Permission already assigned")]
    public async Task<IActionResult> AddPermissionToRole(long roleId, [FromBody] AddPermissionToRoleDto dto)
    {
        var result = await _rolePermissions.AddPermissionToRole(roleId, dto.PermissionId);

        return result switch
        {
            AddPermissionToRoleResult.Success s => Ok(s.RolePermission),
            AddPermissionToRoleResult.AlreadyAssigned => Conflict(new ErrorResponseDto { Error = "Permission is already assigned to this role." }),
            AddPermissionToRoleResult.RoleNotFound => NotFound(new ErrorResponseDto { Error = "Role not found." }),
            AddPermissionToRoleResult.PermissionNotFound => NotFound(new ErrorResponseDto { Error = "Permission not found." }),
            AddPermissionToRoleResult.Error err => StatusCode(500, new ErrorResponseDto { Error = err.Message }),
            _ => StatusCode(500, new ErrorResponseDto { Error = "An unknown error occurred." })
        };
    }

    [HttpDelete("{roleId:long}/permissions/{permissionId:long}")]
    [Authorize(Policy = "CanManageConfig")]
    [SwaggerOperation(Summary = "Removes a permission from a role.")]
    [SwaggerResponse(200, "Permission removed")]
    [SwaggerResponse(403, "Action forbidden")]
    [SwaggerResponse(404, "Assignment not found")]
    public async Task<IActionResult> RemovePermissionFromRole(long roleId, long permissionId)
    {
        var result = await _rolePermissions.RemovePermissionFromRole(roleId, permissionId);

        return result switch
        {
            RemovePermissionFromRoleResult.Success => Ok(new StatusResponseDto { Status = "Permission removed from role." }),
            RemovePermissionFromRoleResult.NotFound => NotFound(new ErrorResponseDto { Error = "Permission assignment not found for this role." }),
            RemovePermissionFromRoleResult.Forbidden forbidden => StatusCode(403, new ErrorResponseDto { Error = forbidden.Message }),
            RemovePermissionFromRoleResult.Error err => StatusCode(500, new ErrorResponseDto { Error = err.Message }),
            _ => StatusCode(500, new ErrorResponseDto { Error = "An unknown error occurred." })
        };
    }
}