using System.Linq.Expressions;

using MobyPark.DTOs.Permission.Request;
using MobyPark.Models;
using MobyPark.Models.Repositories.Interfaces;
using MobyPark.Services;
using MobyPark.Services.Results.Permission;

using Moq;

namespace MobyPark.Tests;
[TestClass]
public sealed class PermissionServiceTests
{
    #region Setup
    private Mock<IPermissionRepository> _mockPermissionsRepo = null!;
    private Mock<IRolePermissionRepository> _mockRolePermissionRepo = null!;
    private PermissionService _permissionService = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _mockPermissionsRepo = new Mock<IPermissionRepository>();
        _mockRolePermissionRepo = new Mock<IRolePermissionRepository>();
        _permissionService = new PermissionService(_mockPermissionsRepo.Object, _mockRolePermissionRepo.Object);
    }

    #endregion

    #region Create

    [TestMethod]
    [DataRow("USERS", "READ")]
    [DataRow("LOTS", "MANAGE")]
    public async Task CreatePermission_ValidNewPermission_ReturnsSuccess(string resource, string action)
    {
        // Arrange
        var dto = new CreatePermissionDto { Resource = resource, Action = action };
        string expectedResource = resource.ToUpper();
        string expectedAction = action.ToUpper();

        _mockPermissionsRepo.Setup(permissionRepo => permissionRepo.Exists(It.IsAny<Expression<Func<PermissionModel, bool>>>())).ReturnsAsync(false);

        _mockPermissionsRepo.Setup(permissionRepo => permissionRepo.Create(
            It.Is<PermissionModel>(permission => permission.Resource == expectedResource && permission.Action == expectedAction))).ReturnsAsync(true);

        // Act
        var result = await _permissionService.CreatePermission(dto);

        // Assert
        Assert.IsInstanceOfType(result, typeof(CreatePermissionResult.Success));
        var successResult = (CreatePermissionResult.Success)result;
        Assert.AreEqual(expectedResource, successResult.Permission.Resource);
        Assert.AreEqual(expectedAction, successResult.Permission.Action);
        _mockPermissionsRepo.Verify(permissionRepo => permissionRepo.Create(It.IsAny<PermissionModel>()), Times.Once);
    }

    [TestMethod]
    [DataRow("USERS", "READ")]
    public async Task CreatePermission_AlreadyExists_ReturnsAlreadyExists(string resource, string action)
    {
        // Arrange
        var dto = new CreatePermissionDto { Resource = resource, Action = action };

        _mockPermissionsRepo.Setup(permissionRepo => permissionRepo.Exists(
                It.IsAny<Expression<Func<PermissionModel, bool>>>())).ReturnsAsync(true);

        // Act
        var result = await _permissionService.CreatePermission(dto);

        // Assert
        Assert.IsInstanceOfType(result, typeof(CreatePermissionResult.AlreadyExists));
        _mockPermissionsRepo.Verify(permissionRepo => permissionRepo.Create(It.IsAny<PermissionModel>()), Times.Never);
    }

    [TestMethod]
    [DataRow("USERS", "WRITE")]
    public async Task CreatePermission_DbInsertionFails_ReturnsError(string resource, string action)
    {
        // Arrange
        var dto = new CreatePermissionDto { Resource = resource, Action = action };

        _mockPermissionsRepo.Setup(permissionRepo => permissionRepo.Exists(It.IsAny<Expression<Func<PermissionModel, bool>>>())).ReturnsAsync(false);
        _mockPermissionsRepo.Setup(permissionRepo => permissionRepo.Create(It.IsAny<PermissionModel>())).ReturnsAsync(false);

        // Act
        var result = await _permissionService.CreatePermission(dto);

        // Assert
        Assert.IsInstanceOfType(result, typeof(CreatePermissionResult.Error));
        StringAssert.Contains(((CreatePermissionResult.Error)result).Message, "Database insertion failed");
    }

    [TestMethod]
    [DataRow("USERS", "DELETE")]
    public async Task CreatePermission_RepositoryThrows_ReturnsError(string resource, string action)
    {
        // Arrange
        var dto = new CreatePermissionDto { Resource = resource, Action = action };

        _mockPermissionsRepo.Setup(permissionRepo => permissionRepo.Exists(It.IsAny<Expression<Func<PermissionModel, bool>>>())).ReturnsAsync(false);
        _mockPermissionsRepo.Setup(permissionRepo => permissionRepo.Create(It.IsAny<PermissionModel>())).ThrowsAsync(new InvalidOperationException("DB Boom"));

        // Act
        var result = await _permissionService.CreatePermission(dto);

        // Assert
        Assert.IsInstanceOfType(result, typeof(CreatePermissionResult.Error));
        StringAssert.Contains(((CreatePermissionResult.Error)result).Message, "DB Boom");
    }

    #endregion

    #region GetById

    [TestMethod]
    [DataRow(1L, "USERS", "READ")]
    public async Task GetPermissionById_Found_ReturnsSuccess(long id, string resource, string action)
    {
        // Arrange
        var permission = new PermissionModel { Id = id, Resource = resource, Action = action };
        _mockPermissionsRepo.Setup(permissionRepo => permissionRepo.GetById<PermissionModel>(id)).ReturnsAsync(permission);

        // Act
        var result = await _permissionService.GetPermissionById(id);

        // Assert
        Assert.IsInstanceOfType(result, typeof(GetPermissionResult.Success));
        Assert.AreEqual(id, ((GetPermissionResult.Success)result).Permission.Id);
    }

    [TestMethod]
    [DataRow(99L)]
    public async Task GetPermissionById_NotFound_ReturnsNotFound(long id)
    {
        // Arrange
        _mockPermissionsRepo.Setup(permissionRepo => permissionRepo.GetById<PermissionModel>(id)).ReturnsAsync((PermissionModel?)null);

        // Act
        var result = await _permissionService.GetPermissionById(id);

        // Assert
        Assert.IsInstanceOfType(result, typeof(GetPermissionResult.NotFound));
    }

    #endregion

    #region GetByResourceAndAction

    [TestMethod]
    [DataRow("USERS", "READ")]
    [DataRow("lots", "manage")]
    public async Task GetPermissionByResourceAndAction_Found_ReturnsSuccess(string resource, string action)
    {
        // Arrange
        string expectedResource = resource.ToUpper();
        string expectedAction = action.ToUpper();
        var permission = new PermissionModel { Id = 1, Resource = expectedResource, Action = expectedAction };
        _mockPermissionsRepo.Setup(permissionRepo => permissionRepo.GetByResourceAndAction(expectedResource, expectedAction)).ReturnsAsync(permission);

        // Act
        var result = await _permissionService.GetPermissionByResourceAndAction(resource, action);

        // Assert
        Assert.IsInstanceOfType(result, typeof(GetPermissionResult.Success));
        Assert.AreEqual(expectedResource, ((GetPermissionResult.Success)result).Permission.Resource);
        Assert.AreEqual(expectedAction, ((GetPermissionResult.Success)result).Permission.Action);
    }

    [TestMethod]
    [DataRow("USERS", "NONEXISTENT")]
    public async Task GetPermissionByResourceAndAction_NotFound_ReturnsNotFound(string resource, string action)
    {
        // Arrange
        string expectedResource = resource.ToUpper();
        string expectedAction = action.ToUpper();
        _mockPermissionsRepo.Setup(permissionRepo => permissionRepo.GetByResourceAndAction(expectedResource, expectedAction)).ReturnsAsync((PermissionModel?)null);

        // Act
        var result = await _permissionService.GetPermissionByResourceAndAction(resource, action);

        // Assert
        Assert.IsInstanceOfType(result, typeof(GetPermissionResult.NotFound));
    }

    [TestMethod]
    [DataRow(null, "READ")]
    [DataRow("USERS", null)]
    [DataRow(" ", "READ")]
    [DataRow("USERS", " ")]
    public async Task GetPermissionByResourceAndAction_InvalidInput_ReturnsInvalidInput(string resource, string action)
    {
        // Act
        var result = await _permissionService.GetPermissionByResourceAndAction(resource, action);

        // Assert
        Assert.IsInstanceOfType(result, typeof(GetPermissionResult.InvalidInput));
    }

    #endregion

    #region GetAll

    [TestMethod]
    public async Task GetAllPermissions_Found_ReturnsSuccessList()
    {
        // Arrange
        var list = new List<PermissionModel>
        {
            new() { Id = 1, Resource = "R1", Action = "A1" },
            new() { Id = 2, Resource = "R2", Action = "A2" }
        };
        _mockPermissionsRepo.Setup(permissionRepo => permissionRepo.GetAll()).ReturnsAsync(list);

        // Act
        var result = await _permissionService.GetAllPermissions();

        // Assert
        Assert.IsInstanceOfType(result, typeof(GetPermissionListResult.Success));
        Assert.AreEqual(2, ((GetPermissionListResult.Success)result).Permissions.Count);
    }

    [TestMethod]
    public async Task GetAllPermissions_NotFound_ReturnsNotFound()
    {
        // Arrange
        _mockPermissionsRepo.Setup(permissionRepo => permissionRepo.GetAll()).ReturnsAsync(new List<PermissionModel>());

        // Act
        var result = await _permissionService.GetAllPermissions();

        // Assert
        Assert.IsInstanceOfType(result, typeof(GetPermissionListResult.NotFound));
    }

    #endregion

    #region GeByRoleId

    [TestMethod]
    [DataRow(1L, 3)]
    public async Task GetPermissionsByRoleId_Found_ReturnsSuccessList(long roleId, int count)
    {
        // Arrange
        var list = Enumerable.Range(1, count).Select(i => new PermissionModel { Id = i }).ToList();
        _mockPermissionsRepo.Setup(permissionRepo => permissionRepo.GetByRoleId(roleId)).ReturnsAsync(list);

        // Act
        var result = await _permissionService.GetPermissionsByRoleId(roleId);

        // Assert
        Assert.IsInstanceOfType(result, typeof(GetPermissionListResult.Success));
        Assert.AreEqual(count, ((GetPermissionListResult.Success)result).Permissions.Count);
    }

    [TestMethod]
    [DataRow(99L)]
    public async Task GetPermissionsByRoleId_NotFound_ReturnsNotFound(long roleId)
    {
        // Arrange
        _mockPermissionsRepo.Setup(permissionRepo => permissionRepo.GetByRoleId(roleId)).ReturnsAsync(new List<PermissionModel>());

        // Act
        var result = await _permissionService.GetPermissionsByRoleId(roleId);

        // Assert
        Assert.IsInstanceOfType(result, typeof(GetPermissionListResult.NotFound));
    }

    [TestMethod]
    [DataRow(1L)]
    public async Task GetPermissionsByRoleId_RepositoryThrows_ReturnsError(long roleId)
    {
        // Arrange
        _mockPermissionsRepo.Setup(permissionRepo => permissionRepo.GetByRoleId(roleId)).ThrowsAsync(new InvalidOperationException("DB Boom"));

        // Act
        var result = await _permissionService.GetPermissionsByRoleId(roleId);

        // Assert
        Assert.IsInstanceOfType(result, typeof(GetPermissionListResult.Error));
        StringAssert.Contains(((GetPermissionListResult.Error)result).Message, "DB Boom");
    }

    #endregion

    #region Exists

    [TestMethod]
    [DataRow("id", "1")]
    [DataRow("resource+action", "USERS:READ")]
    public async Task PermissionExists_ValidChecks_ReturnsCorrectResult(string checkBy, string value)
    {
        // Arrange
        _mockPermissionsRepo.Setup(permissionRepo => permissionRepo.Exists(It.IsAny<Expression<Func<PermissionModel, bool>>>())).ReturnsAsync(true);

        // Act
        var result = await _permissionService.PermissionExists(checkBy, value);

        // Assert
        Assert.IsInstanceOfType(result, typeof(PermissionExistsResult.Exists));
        _mockPermissionsRepo.Verify(permissionRepo => permissionRepo.Exists(It.IsAny<Expression<Func<PermissionModel, bool>>>()), Times.Once);
    }

    [TestMethod]
    [DataRow("id", "99")]
    [DataRow("resource+action", "USERS:WRITE")]
    public async Task PermissionExists_WhenNotExists_ReturnsNotExistsResult(string checkBy, string value)
    {
        // Arrange
        _mockPermissionsRepo.Setup(permissionRepo => permissionRepo.Exists(It.IsAny<Expression<Func<PermissionModel, bool>>>())).ReturnsAsync(false);

        // Act
        var result = await _permissionService.PermissionExists(checkBy, value);

        // Assert
        Assert.IsInstanceOfType(result, typeof(PermissionExistsResult.NotExists));
        _mockPermissionsRepo.Verify(permissionRepo => permissionRepo.Exists(It.IsAny<Expression<Func<PermissionModel, bool>>>()), Times.Once);
    }

    [TestMethod]
    [DataRow("id", "abc")]
    [DataRow("resource+action", "USERS.READ")]
    [DataRow("resource+action", "USERS:")]
    [DataRow("resource+action", ":READ")]
    [DataRow("unknown", "value")]
    [DataRow("id", " ")]
    [DataRow("id", null)]
    public async Task PermissionExists_InvalidInput_ReturnsInvalidInput(string checkBy, string value)
    {
        // Act
        var result = await _permissionService.PermissionExists(checkBy, value);

        // Assert
        Assert.IsInstanceOfType(result, typeof(PermissionExistsResult.InvalidInput));
        _mockPermissionsRepo.Verify(permissionRepo => permissionRepo.Exists(It.IsAny<Expression<Func<PermissionModel, bool>>>()), Times.Never);
    }

    [TestMethod]
    [DataRow("id", "1")]
    [DataRow("resource+action", "USERS:READ")]
    public async Task PermissionExists_RepositoryThrows_ReturnsError(string checkBy, string value)
    {
        // Arrange
        _mockPermissionsRepo.Setup(permissionRepo => permissionRepo.Exists(It.IsAny<Expression<Func<PermissionModel, bool>>>()))
            .ThrowsAsync(new InvalidOperationException("DB Boom"));

        // Act
        var result = await _permissionService.PermissionExists(checkBy, value);

        // Assert
        Assert.IsInstanceOfType(result, typeof(PermissionExistsResult.Error));
    }

    #endregion

    #region Delete

    [TestMethod]
    [DataRow(1L)]
    public async Task DeletePermission_Success_ReturnsSuccess(long id)
    {
        // Arrange
        var permission = new PermissionModel { Id = id, Resource = "R", Action = "A" };
        _mockPermissionsRepo.Setup(permissionRepo => permissionRepo.GetById<PermissionModel>(id)).ReturnsAsync(permission);
        _mockRolePermissionRepo.Setup(rp => rp.GetRolesByPermissionId(id)).ReturnsAsync(new List<RolePermissionModel>());
        _mockPermissionsRepo.Setup(permissionRepo => permissionRepo.Delete(permission)).ReturnsAsync(true);

        // Act
        var result = await _permissionService.DeletePermission(id);

        // Assert
        Assert.IsInstanceOfType(result, typeof(DeletePermissionResult.Success));
        _mockPermissionsRepo.Verify(permissionRepo => permissionRepo.Delete(permission), Times.Once);
    }

    [TestMethod]
    [DataRow(99L)]
    public async Task DeletePermission_NotFound_ReturnsNotFound(long id)
    {
        // Arrange
        _mockPermissionsRepo.Setup(permissionRepo => permissionRepo.GetById<PermissionModel>(id)).ReturnsAsync((PermissionModel?)null);

        // Act
        var result = await _permissionService.DeletePermission(id);

        // Assert
        Assert.IsInstanceOfType(result, typeof(DeletePermissionResult.NotFound));
        _mockPermissionsRepo.Verify(permissionRepo => permissionRepo.Delete(It.IsAny<PermissionModel>()), Times.Never);
    }

    [TestMethod]
    [DataRow(1L)]
    public async Task DeletePermission_HasRoleLinks_ReturnsConflict(long id)
    {
        // Arrange
        var permission = new PermissionModel { Id = id };
        _mockPermissionsRepo.Setup(permissionRepo => permissionRepo.GetById<PermissionModel>(id)).ReturnsAsync(permission);
        var links = new List<RolePermissionModel> { new() };
        _mockRolePermissionRepo.Setup(rp => rp.GetRolesByPermissionId(id)).ReturnsAsync(links);
        _mockRolePermissionRepo.Setup(rp => rp.RoleHasPermission(id, permission.Id)).ReturnsAsync(true);

        // Act
        var result = await _permissionService.DeletePermission(id);

        // Assert
        Assert.IsInstanceOfType(result, typeof(DeletePermissionResult.Conflict));
        _mockPermissionsRepo.Verify(permissionRepo => permissionRepo.Delete(It.IsAny<PermissionModel>()), Times.Never);
    }

    [TestMethod]
    [DataRow(1L)]
    public async Task DeletePermission_DbDeleteFails_ReturnsError(long id)
    {
        // Arrange
        var permission = new PermissionModel { Id = id };
        _mockPermissionsRepo.Setup(permissionRepo => permissionRepo.GetById<PermissionModel>(id)).ReturnsAsync(permission);
        _mockRolePermissionRepo.Setup(rp => rp.GetRolesByPermissionId(id)).ReturnsAsync(new List<RolePermissionModel>());
        _mockPermissionsRepo.Setup(permissionRepo => permissionRepo.Delete(permission)).ReturnsAsync(false);

        // Act
        var result = await _permissionService.DeletePermission(id);

        // Assert
        Assert.IsInstanceOfType(result, typeof(DeletePermissionResult.Error));
        StringAssert.Contains(((DeletePermissionResult.Error)result).Message, "Database deletion failed");
    }

    [TestMethod]
    [DataRow(1L)]
    public async Task DeletePermission_RepositoryThrows_ReturnsError(long id)
    {
        // Arrange
        var permission = new PermissionModel { Id = id };
        _mockPermissionsRepo.Setup(permissionRepo => permissionRepo.GetById<PermissionModel>(id)).ReturnsAsync(permission);
        _mockRolePermissionRepo.Setup(rp => rp.GetRolesByPermissionId(id)).ReturnsAsync(new List<RolePermissionModel>());
        _mockPermissionsRepo.Setup(permissionRepo => permissionRepo.Delete(permission))
            .ThrowsAsync(new InvalidOperationException("DB Boom"));

        // Act
        var result = await _permissionService.DeletePermission(id);

        // Assert
        Assert.IsInstanceOfType(result, typeof(DeletePermissionResult.Error));
        StringAssert.Contains(((DeletePermissionResult.Error)result).Message, "DB Boom");
    }

    #endregion
}