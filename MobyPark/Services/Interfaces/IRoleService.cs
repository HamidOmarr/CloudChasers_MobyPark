using MobyPark.DTOs.Role.Request;
using MobyPark.Services.Results.Role;

namespace MobyPark.Services.Interfaces;

public interface IRoleService
{
    Task<CreateRoleResult> CreateRole(CreateRoleDto dto);
    Task<GetRoleResult> GetRoleById(long roleId);
    Task<GetRoleResult> GetRoleByName(string roleName);
    Task<GetRoleListResult> GetAllRoles();
    Task<RoleExistsResult> RoleExists(string checkBy, string filterValue);
    Task<int> CountRoles();
    Task<UpdateRoleResult> UpdateRole(long roleId, UpdateRoleDto dto);
    Task<DeleteRoleResult> DeleteRoleById(long roleId);
    Task<DeleteRoleResult> DeleteRoleByName(string roleName);
}