using MobyPark.Models;
using MobyPark.Models.Repositories.Interfaces;
using MobyPark.Models.Repositories.RepositoryStack;
using MobyPark.Validation;

namespace MobyPark.Services;

public class RoleService
{
    private readonly IRoleRepository _roles;

    public RoleService(IRepositoryStack repoStack)
    {
        _roles = repoStack.Roles;
    }

    public async Task<RoleModel> CreateRole(string roleName, string description)
    {
        var role = new RoleModel { Name = roleName.ToUpperInvariant(), Description = description };

        (bool createdSuccessfully, long id) = await _roles.CreateWithId(role);
        if (createdSuccessfully) role.Id = id;
        return role;
    }

    public async Task<RoleModel?> GetRoleById(long roleId)
    {
        if (!await RoleExists("id", roleId.ToString())) return null;
        return await _roles.GetById<RoleModel>(roleId);
    }

    public async Task<List<string>> GetAllRoleNames()
    {
        var roles = await _roles.GetAll();
        return roles.Select(role => role.Name).ToList();
    }

    public async Task<bool> RoleExists(string checkBy, string filterValue)
    {
        if (string.IsNullOrWhiteSpace(filterValue))
            throw new ArgumentException("Filter value cannot be empty or whitespace.", nameof(filterValue));

        bool exists = checkBy.ToLower() switch
        {
            "id" => long.TryParse(filterValue, out long id) && await _roles.Exists(role => role.Id == id),
            "name" => await _roles.Exists(role => role.Name == filterValue),
            _ => throw new ArgumentException("Invalid checkBy parameter. Must be 'id' or 'name'.", nameof(checkBy))
        };

        return exists;
    }

    public async Task<int> CountRoles() => await _roles.Count();

    private async Task<bool> UpdateRole(long roleId, string newRoleName = "", string newDescription = "")
    {
        var role = await GetRoleById(roleId);
        if (role is null) return false;

        role.Name = string.IsNullOrWhiteSpace(newRoleName) ? role.Name : newRoleName.ToUpperInvariant();
        role.Description = string.IsNullOrWhiteSpace(newDescription) ? role.Description : newDescription;

        bool updatedSuccessfully = await _roles.Update(role);
        return updatedSuccessfully;
    }

    public async Task<bool> UpdateRoleName(long roleId, string newRoleName) => await UpdateRole(roleId, newRoleName: newRoleName);

    public async Task<bool> UpdateRoleDescription(long roleId, string newDescription) => await UpdateRole(roleId, newDescription: newDescription);

    private async Task<bool> DeleteRole(RoleModel role)
    {
        if (role.Name.ToLower() == "admin")
            throw new InvalidOperationException("Cannot delete the admin role.");

        if (await _roles.AnyUsersAssigned(role.Id))
            throw new InvalidOperationException("Cannot delete a role that has users assigned to it.");

        return await _roles.DeleteRole(role);
    }

    public async Task<bool> DeleteRoleByName(string roleName)
    {
        if (!await RoleExists("name", roleName)) return false;

        var role = (await _roles.GetByName(roleName))!;
        return await DeleteRole(role);
    }

    public async Task<bool> DeleteRoleById(long roleId)
    {
        if (!await RoleExists("id", roleId.ToString())) return false;

        var role = (await GetRoleById(roleId))!;
        return await DeleteRole(role);
    }
}
