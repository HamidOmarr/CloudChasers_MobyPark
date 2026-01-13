using MobyPark.DTOs.Role.Request;
using MobyPark.Models;
using MobyPark.Models.Repositories.Interfaces;
using MobyPark.Services.Interfaces;
using MobyPark.Services.Results.Role;
using MobyPark.Validation;

namespace MobyPark.Services;

public class RoleService : IRoleService
{
    private readonly IRoleRepository _roles;

    public RoleService(IRoleRepository roles)
    {
        _roles = roles;
    }

    public async Task<CreateRoleResult> CreateRole(CreateRoleDto dto)
    {
        dto.Name = dto.Name.Upper();

        var existsResult = await RoleExists("name", dto.Name);
        if (existsResult is not RoleExistsResult.NotExists)
        {
            return existsResult switch
            {
                RoleExistsResult.Exists => new CreateRoleResult.AlreadyExists(),
                RoleExistsResult.InvalidInput err => new CreateRoleResult.Error(err.Message),
                RoleExistsResult.Error err => new CreateRoleResult.Error(err.Message),
                _ => new CreateRoleResult.Error("Unknown error occurred while checking role existence.")
            };
        }

        var role = new RoleModel { Name = dto.Name, Description = dto.Description };

        try
        {
            (bool createdSuccessfully, long id) = await _roles.CreateWithId(role);
            if (!createdSuccessfully)
                return new CreateRoleResult.Error("Role creation failed.");

            role.Id = id;
            return new CreateRoleResult.Success(role);
        }
        catch (Exception ex)
        { return new CreateRoleResult.Error("An error occurred while creating the role: " + ex.Message); }
    }

    public async Task<GetRoleResult> GetRoleById(long roleId)
    {
        var role = await _roles.GetById<RoleModel>(roleId);
        if (role is null)
            return new GetRoleResult.NotFound();
        return new GetRoleResult.Success(role);
    }

    public async Task<GetRoleResult> GetRoleByName(string roleName)
    {
        var role = await _roles.GetByName(roleName);
        if (role is null)
            return new GetRoleResult.NotFound();
        return new GetRoleResult.Success(role);
    }

    public async Task<GetRoleListResult> GetAllRoles()
    {
        var roles = await _roles.GetAll();
        if (roles.Count == 0)
            return new GetRoleListResult.NotFound();
        return new GetRoleListResult.Success(roles);
    }

    public async Task<RoleExistsResult> RoleExists(string checkBy, string filterValue)
    {
        checkBy = checkBy.Lower();
        filterValue = filterValue.TrimSafe();

        if (string.IsNullOrEmpty(filterValue))
            return new RoleExistsResult.InvalidInput("Filter value cannot be empty or whitespace.");

        long id = 0;
        if (checkBy == "id" && !long.TryParse(filterValue, out id))
            return new RoleExistsResult.InvalidInput("ID must be a valid long integer when checking by 'id'.");
        if (checkBy != "id" && checkBy != "name")
            return new RoleExistsResult.InvalidInput("Invalid checkBy parameter. Must be 'id' or 'name'.");


        bool exists = checkBy switch
        {
            "id" => await _roles.Exists(role => role.Id == id),
            "name" => await _roles.Exists(role => role.Name.Equals(filterValue, StringComparison.OrdinalIgnoreCase)),
            _ => false
        };

        return exists ? new RoleExistsResult.Exists() : new RoleExistsResult.NotExists();
    }

    public async Task<int> CountRoles() => await _roles.Count();

    public async Task<UpdateRoleResult> UpdateRole(long roleId, UpdateRoleDto dto)
    {
        var getResult = await GetRoleById(roleId);
        if (getResult is not GetRoleResult.Success success)
            return new UpdateRoleResult.NotFound();
        var existingRole = success.Role;

        bool changed = false;
        if (dto.Description != null && dto.Description != existingRole.Description)
        {
            existingRole.Description = dto.Description;
            changed = true;
        }

        if (!changed)
            return new UpdateRoleResult.NoChangesMade();

        try
        {
            bool saved = await _roles.Update(existingRole, dto);
            if (!saved)
                return new UpdateRoleResult.Error("Database update failed or reported no changes.");

            return new UpdateRoleResult.Success(existingRole);
        }
        catch (Exception ex)
        { return new UpdateRoleResult.Error(ex.Message); }
    }

    public async Task<DeleteRoleResult> DeleteRoleById(long roleId)
    {
        var getResult = await GetRoleById(roleId);
        if (getResult is GetRoleResult.NotFound)
            return new DeleteRoleResult.NotFound();

        var role = ((GetRoleResult.Success)getResult).Role;
        return await DeleteRole(role);
    }

    public async Task<DeleteRoleResult> DeleteRoleByName(string roleName)
    {
        var getResult = await GetRoleByName(roleName);
        if (getResult is not GetRoleResult.Success success)
        {
            return getResult switch
            {
                GetRoleResult.NotFound => new DeleteRoleResult.NotFound(),
                GetRoleResult.InvalidInput invalid => new DeleteRoleResult.Error(invalid.Message),
                _ => new DeleteRoleResult.Error("Failed to find role.")
            };
        }

        var role = success.Role;
        return await DeleteRole(role);
    }

    private async Task<DeleteRoleResult> DeleteRole(RoleModel role)
    {
        if (role.Name.Equals("ADMIN", StringComparison.OrdinalIgnoreCase))
            return new DeleteRoleResult.Forbidden("Cannot delete the ADMIN role.");

        try
        {
            if (await _roles.AnyUsersAssigned(role.Id))
                return new DeleteRoleResult.Conflict("Cannot delete a role that has users assigned to it.");

            if (!await _roles.DeleteRole(role))
                return new DeleteRoleResult.Error("Database deletion failed.");

            return new DeleteRoleResult.Success();
        }
        catch (Exception ex)
        { return new DeleteRoleResult.Error(ex.Message); }
    }
}