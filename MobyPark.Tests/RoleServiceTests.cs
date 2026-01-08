using System.Linq.Expressions;

using MobyPark.DTOs.Role.Request;
using MobyPark.Models;
using MobyPark.Models.Repositories.Interfaces;
using MobyPark.Services;
using MobyPark.Services.Results.Role;

using Moq;

namespace MobyPark.Tests;

[TestClass]
public sealed class RoleServiceTests
{
    #region Setup

    private Mock<IRoleRepository> _mockRolesRepo = null!;
    private RoleService _roleService = null!;

    private const string AdminRoleName = "ADMIN";
    private const string UserRoleName = "USER";
    private const long AdminRoleId = 1L;
    private const long UserRoleId = 2L;

    [TestInitialize]
    public void TestInitialize()
    {
        _mockRolesRepo = new Mock<IRoleRepository>();
        _roleService = new RoleService(_mockRolesRepo.Object);
    }

    #endregion

    #region Create

    [TestMethod]
    [DataRow("manager", "Manages content")]
    [DataRow("Guest", "Read-only access")]
    public async Task CreateRole_ValidNewRole_ReturnsSuccess(string name, string description)
    {
        // Arrange
        var dto = new CreateRoleDto { Name = name, Description = description };
        string expectedName = name.ToUpperInvariant();
        long newRoleId = 3L;

        _mockRolesRepo.Setup(roleRepo => roleRepo.Exists(It.IsAny<Expression<Func<RoleModel, bool>>>())).ReturnsAsync(false);
        _mockRolesRepo.Setup(roleRepo => roleRepo.CreateWithId(
            It.Is<RoleModel>(role => role.Name == expectedName && role.Description == description))).ReturnsAsync((true, newRoleId));

        // Act
        var result = await _roleService.CreateRole(dto);

        // Assert
        Assert.IsInstanceOfType(result, typeof(CreateRoleResult.Success));
        var successResult = (CreateRoleResult.Success)result;
        Assert.AreEqual(newRoleId, successResult.Role.Id);
        Assert.AreEqual(expectedName, successResult.Role.Name);
    }

    [TestMethod]
    [DataRow("Admin")]
    public async Task CreateRole_AlreadyExists_ReturnsAlreadyExists(string name)
    {
        // Arrange
        var dto = new CreateRoleDto { Name = name, Description = "Test" };

        _mockRolesRepo.Setup(roleRepo => roleRepo.Exists(It.IsAny<Expression<Func<RoleModel, bool>>>())).ReturnsAsync(true);

        // Act
        var result = await _roleService.CreateRole(dto);

        // Assert
        Assert.IsInstanceOfType(result, typeof(CreateRoleResult.AlreadyExists));
        _mockRolesRepo.Verify(roleRepo => roleRepo.CreateWithId(It.IsAny<RoleModel>()), Times.Never);
    }

    [TestMethod]
    [DataRow("NewRole")]
    public async Task CreateRole_DbInsertionFails_ReturnsError(string name)
    {
        // Arrange
        var dto = new CreateRoleDto { Name = name, Description = "Test" };

        _mockRolesRepo.Setup(roleRepo => roleRepo.Exists(It.IsAny<Expression<Func<RoleModel, bool>>>())).ReturnsAsync(false);
        _mockRolesRepo.Setup(roleRepo => roleRepo.CreateWithId(It.IsAny<RoleModel>())).ReturnsAsync((false, 0L));

        // Act
        var result = await _roleService.CreateRole(dto);

        // Assert
        Assert.IsInstanceOfType(result, typeof(CreateRoleResult.Error));
        StringAssert.Contains(((CreateRoleResult.Error)result).Message, "Role creation failed");
    }

    [TestMethod]
    [DataRow("ErrorRole")]
    public async Task CreateRole_RepositoryThrows_ReturnsError(string name)
    {
        // Arrange
        var dto = new CreateRoleDto { Name = name, Description = "Test" };

        _mockRolesRepo.Setup(roleRepo => roleRepo.Exists(It.IsAny<Expression<Func<RoleModel, bool>>>())).ReturnsAsync(false);
        _mockRolesRepo.Setup(roleRepo => roleRepo.CreateWithId(It.IsAny<RoleModel>())).ThrowsAsync(new InvalidOperationException("DB Boom"));

        // Act
        var result = await _roleService.CreateRole(dto);

        // Assert
        Assert.IsInstanceOfType(result, typeof(CreateRoleResult.Error));
        StringAssert.Contains(((CreateRoleResult.Error)result).Message, "DB Boom");
    }

    #endregion

    #region GetRoleById

    [TestMethod]
    [DataRow(UserRoleId)]
    public async Task GetRoleById_Found_ReturnsSuccess(long roleId)
    {
        // Arrange
        var role = new RoleModel { Id = roleId, Name = UserRoleName };
        _mockRolesRepo.Setup(roleRepo => roleRepo.GetById<RoleModel>(roleId)).ReturnsAsync(role);

        // Act
        var result = await _roleService.GetRoleById(roleId);

        // Assert
        Assert.IsInstanceOfType(result, typeof(GetRoleResult.Success));
        Assert.AreEqual(roleId, ((GetRoleResult.Success)result).Role.Id);
    }

    [TestMethod]
    [DataRow(99L)]
    public async Task GetRoleById_NotFound_ReturnsNotFound(long roleId)
    {
        // Arrange
        _mockRolesRepo.Setup(roleRepo => roleRepo.GetById<RoleModel>(roleId)).ReturnsAsync((RoleModel?)null);

        // Act
        var result = await _roleService.GetRoleById(roleId);

        // Assert
        Assert.IsInstanceOfType(result, typeof(GetRoleResult.NotFound));
    }

    #endregion

    #region GetRoleByName

    [TestMethod]
    [DataRow(UserRoleName)]
    public async Task GetRoleByName_Found_ReturnsSuccess(string roleName)
    {
        // Arrange
        var role = new RoleModel { Id = UserRoleId, Name = roleName };
        _mockRolesRepo.Setup(roleRepo => roleRepo.GetByName(roleName)).ReturnsAsync(role);

        // Act
        var result = await _roleService.GetRoleByName(roleName);

        // Assert
        Assert.IsInstanceOfType(result, typeof(GetRoleResult.Success));
        Assert.AreEqual(roleName, ((GetRoleResult.Success)result).Role.Name);
    }

    [TestMethod]
    [DataRow("NonExistentRole")]
    public async Task GetRoleByName_NotFound_ReturnsNotFound(string roleName)
    {
        // Arrange
        _mockRolesRepo.Setup(roleRepo => roleRepo.GetByName(roleName)).ReturnsAsync((RoleModel?)null);

        // Act
        var result = await _roleService.GetRoleByName(roleName);

        // Assert
        Assert.IsInstanceOfType(result, typeof(GetRoleResult.NotFound));
    }

    #endregion

    #region GetAllRoles

    [TestMethod]
    public async Task GetAllRoles_Found_ReturnsSuccessList()
    {
        // Arrange
        var roles = new List<RoleModel>
        {
            new() { Id = AdminRoleId, Name = AdminRoleName },
            new() { Id = UserRoleId, Name = UserRoleName }
        };
        _mockRolesRepo.Setup(roleRepo => roleRepo.GetAll()).ReturnsAsync(roles);

        // Act
        var result = await _roleService.GetAllRoles();

        // Assert
        Assert.IsInstanceOfType(result, typeof(GetRoleListResult.Success));
        Assert.AreEqual(2, ((GetRoleListResult.Success)result).Roles.Count);
    }

    [TestMethod]
    public async Task GetAllRoles_NotFound_ReturnsNotFound()
    {
        // Arrange
        _mockRolesRepo.Setup(roleRepo => roleRepo.GetAll()).ReturnsAsync(new List<RoleModel>());

        // Act
        var result = await _roleService.GetAllRoles();

        // Assert
        Assert.IsInstanceOfType(result, typeof(GetRoleListResult.NotFound));
    }

    #endregion

    #region RoleExists

    [TestMethod]
    [DataRow("id", "1")]
    [DataRow("name", "Admin")]
    public async Task RoleExists_WhenExists_ReturnsExistsResult(string checkBy, string value)
    {
        // Arrange
        _mockRolesRepo.Setup(roleRepo => roleRepo.Exists(It.IsAny<Expression<Func<RoleModel, bool>>>())).ReturnsAsync(true);

        // Act
        var result = await _roleService.RoleExists(checkBy, value);

        // Assert
        Assert.IsInstanceOfType(result, typeof(RoleExistsResult.Exists));
        _mockRolesRepo.Verify(roleRepo => roleRepo.Exists(It.IsAny<Expression<Func<RoleModel, bool>>>()), Times.Once);
    }

    [TestMethod]
    [DataRow("id", "99")]
    [DataRow("name", "NonExistent")]
    public async Task RoleExists_WhenNotExists_ReturnsNotExistsResult(string checkBy, string value)
    {
        // Arrange
        _mockRolesRepo.Setup(roleRepo => roleRepo.Exists(It.IsAny<Expression<Func<RoleModel, bool>>>())).ReturnsAsync(false);

        // Act
        var result = await _roleService.RoleExists(checkBy, value);

        // Assert
        Assert.IsInstanceOfType(result, typeof(RoleExistsResult.NotExists));
        _mockRolesRepo.Verify(roleRepo => roleRepo.Exists(It.IsAny<Expression<Func<RoleModel, bool>>>()), Times.Once);
    }

    [TestMethod]
    [DataRow("id", "abc")]
    [DataRow("name", " ")]
    [DataRow("name", null)]
    [DataRow("invalidCheck", "value")]
    public async Task RoleExists_InvalidInput_ReturnsInvalidInput(string checkBy, string value)
    {
        // Act
        var result = await _roleService.RoleExists(checkBy, value);

        // Assert
        Assert.IsInstanceOfType(result, typeof(RoleExistsResult.InvalidInput));
        _mockRolesRepo.Verify(roleRepo => roleRepo.Exists(It.IsAny<Expression<Func<RoleModel, bool>>>()), Times.Never);
    }

    #endregion

    #region Count

    [TestMethod]
    [DataRow(0)]
    [DataRow(5)]
    public async Task CountRoles_ReturnsCount(int count)
    {
        // Arrange
        _mockRolesRepo.Setup(roleRepo => roleRepo.Count()).ReturnsAsync(count);

        // Act
        var result = await _roleService.CountRoles();

        // Assert
        Assert.AreEqual(count, result);
    }

    #endregion

    #region Update

    [TestMethod]
    [DataRow(UserRoleId, "Updated Description")]
    public async Task UpdateRole_ValidChange_ReturnsSuccess(long roleId, string newDescription)
    {
        // Arrange
        var dto = new UpdateRoleDto { Description = newDescription };
        var existingRole = new RoleModel { Id = roleId, Name = UserRoleName, Description = "Old Description" };

        _mockRolesRepo.Setup(roleRepo => roleRepo.GetById<RoleModel>(roleId)).ReturnsAsync(existingRole);
        _mockRolesRepo.Setup(roleRepo => roleRepo.Update(It.IsAny<RoleModel>(), dto)).ReturnsAsync(true);

        // Act
        var result = await _roleService.UpdateRole(roleId, dto);

        // Assert
        Assert.IsInstanceOfType(result, typeof(UpdateRoleResult.Success));
        var successResult = (UpdateRoleResult.Success)result;
        Assert.AreEqual(newDescription, successResult.Role.Description);
        _mockRolesRepo.Verify(roleRepo => roleRepo.Update(
            It.Is<RoleModel>(role => role.Description == newDescription), dto), Times.Once);
    }

    [TestMethod]
    [DataRow(UserRoleId, "Same Description")]
    public async Task UpdateRole_NoChanges_ReturnsNoChangesMade(long roleId, string description)
    {
        // Arrange
        var dto = new UpdateRoleDto { Description = description };
        var existingRole = new RoleModel { Id = roleId, Name = UserRoleName, Description = description };

        _mockRolesRepo.Setup(roleRepo => roleRepo.GetById<RoleModel>(roleId)).ReturnsAsync(existingRole);

        // Act
        var result = await _roleService.UpdateRole(roleId, dto);

        // Assert
        Assert.IsInstanceOfType(result, typeof(UpdateRoleResult.NoChangesMade));
        _mockRolesRepo.Verify(roleRepo => roleRepo.Update(It.IsAny<RoleModel>(), It.IsAny<UpdateRoleDto>()), Times.Never);
    }

    [TestMethod]
    [DataRow(99L)]
    public async Task UpdateRole_NotFound_ReturnsNotFound(long roleId)
    {
        // Arrange
        var dto = new UpdateRoleDto { Description = "New Desc" };
        _mockRolesRepo.Setup(roleRepo => roleRepo.GetById<RoleModel>(roleId)).ReturnsAsync((RoleModel?)null);

        // Act
        var result = await _roleService.UpdateRole(roleId, dto);

        // Assert
        Assert.IsInstanceOfType(result, typeof(UpdateRoleResult.NotFound));
    }

    [TestMethod]
    [DataRow(UserRoleId)]
    public async Task UpdateRole_DbUpdateFails_ReturnsError(long roleId)
    {
        // Arrange
        var dto = new UpdateRoleDto { Description = "New Desc" };
        var existingRole = new RoleModel { Id = roleId, Name = UserRoleName, Description = "Old Desc" };

        _mockRolesRepo.Setup(roleRepo => roleRepo.GetById<RoleModel>(roleId)).ReturnsAsync(existingRole);
        _mockRolesRepo.Setup(roleRepo => roleRepo.Update(It.IsAny<RoleModel>(), dto)).ReturnsAsync(false);

        // Act
        var result = await _roleService.UpdateRole(roleId, dto);

        // Assert
        Assert.IsInstanceOfType(result, typeof(UpdateRoleResult.Error));
        StringAssert.Contains(((UpdateRoleResult.Error)result).Message, "Database update failed");
    }

    [TestMethod]
    [DataRow(UserRoleId)]
    public async Task UpdateRole_RepositoryThrows_ReturnsError(long roleId)
    {
        // Arrange
        var dto = new UpdateRoleDto { Description = "New Desc" };
        var existingRole = new RoleModel { Id = roleId, Name = UserRoleName, Description = "Old Desc" };

        _mockRolesRepo.Setup(roleRepo => roleRepo.GetById<RoleModel>(roleId)).ReturnsAsync(existingRole);
        _mockRolesRepo.Setup(roleRepo => roleRepo.Update(It.IsAny<RoleModel>(), dto)).ThrowsAsync(new InvalidOperationException("DB Boom"));

        // Act
        var result = await _roleService.UpdateRole(roleId, dto);

        // Assert
        Assert.IsInstanceOfType(result, typeof(UpdateRoleResult.Error));
        StringAssert.Contains(((UpdateRoleResult.Error)result).Message, "DB Boom");
    }

    #endregion

    #region Delete

    [TestMethod]
    [DataRow(UserRoleId)]
    public async Task DeleteRoleById_Success_ReturnsSuccess(long roleId)
    {
        // Arrange
        var role = new RoleModel { Id = roleId, Name = UserRoleName };
        _mockRolesRepo.Setup(roleRepo => roleRepo.GetById<RoleModel>(roleId)).ReturnsAsync(role);
        _mockRolesRepo.Setup(roleRepo => roleRepo.AnyUsersAssigned(roleId)).ReturnsAsync(false); // No users assigned
        _mockRolesRepo.Setup(roleRepo => roleRepo.DeleteRole(role)).ReturnsAsync(true);

        // Act
        var result = await _roleService.DeleteRoleById(roleId);

        // Assert
        Assert.IsInstanceOfType(result, typeof(DeleteRoleResult.Success));
        _mockRolesRepo.Verify(roleRepo => roleRepo.DeleteRole(role), Times.Once);
    }

    [TestMethod]
    [DataRow(UserRoleName)]
    public async Task DeleteRoleByName_Success_ReturnsSuccess(string roleName)
    {
        // Arrange
        var role = new RoleModel { Id = UserRoleId, Name = roleName };
        _mockRolesRepo.Setup(roleRepo => roleRepo.GetByName(roleName)).ReturnsAsync(role);
        _mockRolesRepo.Setup(roleRepo => roleRepo.AnyUsersAssigned(UserRoleId)).ReturnsAsync(false); // No users assigned
        _mockRolesRepo.Setup(roleRepo => roleRepo.DeleteRole(role)).ReturnsAsync(true);

        // Act
        var result = await _roleService.DeleteRoleByName(roleName);

        // Assert
        Assert.IsInstanceOfType(result, typeof(DeleteRoleResult.Success));
        _mockRolesRepo.Verify(roleRepo => roleRepo.DeleteRole(role), Times.Once);
    }

    [TestMethod]
    [DataRow(99L)]
    public async Task DeleteRoleById_NotFound_ReturnsNotFound(long roleId)
    {
        // Arrange
        _mockRolesRepo.Setup(roleRepo => roleRepo.GetById<RoleModel>(roleId)).ReturnsAsync((RoleModel?)null);

        // Act
        var result = await _roleService.DeleteRoleById(roleId);

        // Assert
        Assert.IsInstanceOfType(result, typeof(DeleteRoleResult.NotFound));
    }

    [TestMethod]
    [DataRow("NonExistent")]
    public async Task DeleteRoleByName_NotFound_ReturnsNotFound(string roleName)
    {
        // Arrange
        _mockRolesRepo.Setup(roleRepo => roleRepo.GetByName(roleName)).ReturnsAsync((RoleModel?)null);

        // Act
        var result = await _roleService.DeleteRoleByName(roleName);

        // Assert
        Assert.IsInstanceOfType(result, typeof(DeleteRoleResult.NotFound));
    }

    [TestMethod]
    [DataRow(AdminRoleId)]
    public async Task DeleteRoleById_ForbiddenForAdmin_ReturnsForbidden(long roleId)
    {
        // Arrange
        var role = new RoleModel { Id = roleId, Name = AdminRoleName };
        _mockRolesRepo.Setup(roleRepo => roleRepo.GetById<RoleModel>(roleId)).ReturnsAsync(role);

        // Act
        var result = await _roleService.DeleteRoleById(roleId);

        // Assert
        Assert.IsInstanceOfType(result, typeof(DeleteRoleResult.Forbidden));
        _mockRolesRepo.Verify(roleRepo => roleRepo.DeleteRole(It.IsAny<RoleModel>()), Times.Never);
    }

    [TestMethod]
    [DataRow(AdminRoleName)]
    public async Task DeleteRoleByName_ForbiddenForAdmin_ReturnsForbidden(string roleName)
    {
        // Arrange
        var role = new RoleModel { Id = AdminRoleId, Name = roleName };
        _mockRolesRepo.Setup(roleRepo => roleRepo.GetByName(roleName)).ReturnsAsync(role);

        // Act
        var result = await _roleService.DeleteRoleByName(roleName);

        // Assert
        Assert.IsInstanceOfType(result, typeof(DeleteRoleResult.Forbidden));
        _mockRolesRepo.Verify(roleRepo => roleRepo.DeleteRole(It.IsAny<RoleModel>()), Times.Never);
    }

    [TestMethod]
    [DataRow(UserRoleId)]
    public async Task DeleteRole_ConflictIfUsersAssigned_ReturnsConflict(long roleId)
    {
        // Arrange
        var role = new RoleModel { Id = roleId, Name = UserRoleName };
        _mockRolesRepo.Setup(roleRepo => roleRepo.GetById<RoleModel>(roleId)).ReturnsAsync(role);
        _mockRolesRepo.Setup(roleRepo => roleRepo.AnyUsersAssigned(roleId)).ReturnsAsync(true);

        // Act
        var result = await _roleService.DeleteRoleById(roleId); // Test via ID method

        // Assert
        Assert.IsInstanceOfType(result, typeof(DeleteRoleResult.Conflict));
        _mockRolesRepo.Verify(roleRepo => roleRepo.DeleteRole(It.IsAny<RoleModel>()), Times.Never);
    }

    [TestMethod]
    [DataRow(UserRoleId)]
    public async Task DeleteRole_DbDeleteFails_ReturnsError(long roleId)
    {
        // Arrange
        var role = new RoleModel { Id = roleId, Name = UserRoleName };
        _mockRolesRepo.Setup(roleRepo => roleRepo.GetById<RoleModel>(roleId)).ReturnsAsync(role);
        _mockRolesRepo.Setup(roleRepo => roleRepo.AnyUsersAssigned(roleId)).ReturnsAsync(false);
        _mockRolesRepo.Setup(roleRepo => roleRepo.DeleteRole(role)).ReturnsAsync(false);

        // Act
        var result = await _roleService.DeleteRoleById(roleId);

        // Assert
        Assert.IsInstanceOfType(result, typeof(DeleteRoleResult.Error));
        StringAssert.Contains(((DeleteRoleResult.Error)result).Message, "Database deletion failed");
    }

    [TestMethod]
    [DataRow(UserRoleId)]
    public async Task DeleteRole_RepositoryThrows_ReturnsError(long roleId)
    {
        // Arrange
        var role = new RoleModel { Id = roleId, Name = UserRoleName };
        _mockRolesRepo.Setup(roleRepo => roleRepo.GetById<RoleModel>(roleId)).ReturnsAsync(role);
        _mockRolesRepo.Setup(roleRepo => roleRepo.AnyUsersAssigned(roleId)).ReturnsAsync(false);
        _mockRolesRepo.Setup(roleRepo => roleRepo.DeleteRole(role)).ThrowsAsync(new InvalidOperationException("DB Boom"));

        // Act
        var result = await _roleService.DeleteRoleById(roleId);

        // Assert
        Assert.IsInstanceOfType(result, typeof(DeleteRoleResult.Error));
        StringAssert.Contains(((DeleteRoleResult.Error)result).Message, "DB Boom");
    }

    #endregion
}