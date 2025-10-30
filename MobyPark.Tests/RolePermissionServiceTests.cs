using MobyPark.Models;
using MobyPark.Models.Repositories.Interfaces;
using MobyPark.Services;
using MobyPark.Services.Interfaces;
using MobyPark.Services.Results.Permission;
using MobyPark.Services.Results.Role;
using MobyPark.Services.Results.RolePermission;
using Moq;

namespace MobyPark.Tests;

[TestClass]
public sealed class RolePermissionServiceTests
{
    #region Setup
    private Mock<IRolePermissionRepository> _mockRolePermissionsRepo = null!;
    private Mock<IRoleService> _mockRoleService = null!;
    private Mock<IPermissionService> _mockPermissionService = null!;
    private RolePermissionService _rolePermissionService = null!;

    private const long AdminRoleId = 1L;
    private const long UserRoleId = 2L;
    private const long PermissionId1 = 101L;

    [TestInitialize]
    public void TestInitialize()
    {
        _mockRolePermissionsRepo = new Mock<IRolePermissionRepository>();
        _mockRoleService = new Mock<IRoleService>();
        _mockPermissionService = new Mock<IPermissionService>();

        _rolePermissionService = new RolePermissionService(
            _mockRolePermissionsRepo.Object,
            _mockRoleService.Object,
            _mockPermissionService.Object
        );
    }

    #endregion

    #region GetByRoleId

    [TestMethod]
    [DataRow(UserRoleId, 3)]
    public async Task GetRolePermissionsByRoleId_Found_ReturnsSuccessList(long roleId, int count)
    {
        // Arrange
        var permissions = Enumerable.Range(1, count)
            .Select(i => new RolePermissionModel { RoleId = roleId, PermissionId = i })
            .ToList();
        _mockRolePermissionsRepo.Setup(rolePermissionRepo => rolePermissionRepo.GetPermissionsByRoleId(roleId))
            .ReturnsAsync(permissions);

        // Act
        var result = await _rolePermissionService.GetRolePermissionsByRoleId(roleId);

        // Assert
        Assert.IsInstanceOfType(result, typeof(GetRolePermissionListResult.Success));
        Assert.AreEqual(count, ((GetRolePermissionListResult.Success)result).RolePermissions.Count);
    }

    [TestMethod]
    [DataRow(99L)]
    public async Task GetRolePermissionsByRoleId_NotFound_ReturnsNotFound(long roleId)
    {
        // Arrange
        _mockRolePermissionsRepo.Setup(rolePermissionRepo => rolePermissionRepo.GetPermissionsByRoleId(roleId))
            .ReturnsAsync(new List<RolePermissionModel>());

        // Act
        var result = await _rolePermissionService.GetRolePermissionsByRoleId(roleId);

        // Assert
        Assert.IsInstanceOfType(result, typeof(GetRolePermissionListResult.NotFound));
    }

    [TestMethod]
    [DataRow(UserRoleId)]
    public async Task GetRolePermissionsByRoleId_RepositoryThrows_ReturnsError(long roleId)
    {
        // Arrange
        _mockRolePermissionsRepo.Setup(rolePermissionRepo => rolePermissionRepo.GetPermissionsByRoleId(roleId))
            .ThrowsAsync(new InvalidOperationException("DB error"));

        // Act
        var result = await _rolePermissionService.GetRolePermissionsByRoleId(roleId);

        // Assert
        Assert.IsInstanceOfType(result, typeof(GetRolePermissionListResult.Error));
    }

    #endregion

    #region GetByPermissionId

    [TestMethod]
    [DataRow(PermissionId1, 2)]
    public async Task GetRolesByPermissionId_Found_ReturnsSuccessList(long permissionId, int count)
    {
        // Arrange
        var roles = Enumerable.Range(1, count)
            .Select(i => new RolePermissionModel { RoleId = i, PermissionId = permissionId })
            .ToList();
        _mockRolePermissionsRepo.Setup(rolePermissionRepo => rolePermissionRepo.GetRolesByPermissionId(permissionId))
            .ReturnsAsync(roles);

        // Act
        var result = await _rolePermissionService.GetRolesByPermissionId(permissionId);

        // Assert
        Assert.IsInstanceOfType(result, typeof(GetRolePermissionListResult.Success));
        Assert.AreEqual(count, ((GetRolePermissionListResult.Success)result).RolePermissions.Count);
    }

    [TestMethod]
    [DataRow(999L)]
    public async Task GetRolesByPermissionId_NotFound_ReturnsNotFound(long permissionId)
    {
        // Arrange
        _mockRolePermissionsRepo.Setup(rolePermissionRepo => rolePermissionRepo.GetRolesByPermissionId(permissionId))
            .ReturnsAsync(new List<RolePermissionModel>());

        // Act
        var result = await _rolePermissionService.GetRolesByPermissionId(permissionId);

        // Assert
        Assert.IsInstanceOfType(result, typeof(GetRolePermissionListResult.NotFound));
    }

    [TestMethod]
    [DataRow(PermissionId1)]
    public async Task GetRolesByPermissionId_RepositoryThrows_ReturnsError(long permissionId)
    {
        // Arrange
        _mockRolePermissionsRepo.Setup(rolePermissionRepo => rolePermissionRepo.GetRolesByPermissionId(permissionId))
            .ThrowsAsync(new InvalidOperationException("DB error"));

        // Act
        var result = await _rolePermissionService.GetRolesByPermissionId(permissionId);

        // Assert
        Assert.IsInstanceOfType(result, typeof(GetRolePermissionListResult.Error));
    }

    #endregion

    #region AddPermissionToRole

    [TestMethod]
    [DataRow(UserRoleId, PermissionId1)]
    public async Task AddPermissionToRole_Success_AlsoAddsToAdminAndReturnsSuccess(long roleId, long permissionId)
    {
        // Arrange
        _mockRoleService.Setup(roleService => roleService.RoleExists("id", roleId.ToString()))
            .ReturnsAsync(new RoleExistsResult.Exists());
        _mockPermissionService.Setup(permissionService => permissionService.PermissionExists("id", permissionId.ToString()))
            .ReturnsAsync(new PermissionExistsResult.Exists());
        _mockRolePermissionsRepo.Setup(rolePermissionRepo => rolePermissionRepo.RoleHasPermission(roleId, permissionId))
            .ReturnsAsync(false);

        _mockRolePermissionsRepo.Setup(rolePermissionRepo => rolePermissionRepo.AddPermissionToRole(
            It.Is<RolePermissionModel>(rolePermission => rolePermission.RoleId == roleId && rolePermission.PermissionId == permissionId))).ReturnsAsync(true);
        _mockRolePermissionsRepo.Setup(rolePermissionRepo => rolePermissionRepo.AddPermissionToRole(
            It.Is<RolePermissionModel>(rolePermission => rolePermission.RoleId == AdminRoleId && rolePermission.PermissionId == permissionId))).ReturnsAsync(true);

        // Act
        var result = await _rolePermissionService.AddPermissionToRole(roleId, permissionId);

        // Assert
        Assert.IsInstanceOfType(result, typeof(AddPermissionToRoleResult.Success));
        _mockRolePermissionsRepo.Verify(rolePermissionRepo => rolePermissionRepo.AddPermissionToRole(It.Is<RolePermissionModel>(rolePermission => rolePermission.RoleId == roleId)), Times.Once);
        _mockRolePermissionsRepo.Verify(rolePermissionRepo => rolePermissionRepo.AddPermissionToRole(It.Is<RolePermissionModel>(rolePermission => rolePermission.RoleId == AdminRoleId)), Times.Once);
    }

    [TestMethod]
    [DataRow(AdminRoleId, PermissionId1)]
    public async Task AddPermissionToRole_AddingToAdminDirectly_ReturnsSuccess(long roleId, long permissionId)
    {
        // Arrange
        _mockRoleService.Setup(roleService => roleService.RoleExists("id", roleId.ToString()))
            .ReturnsAsync(new RoleExistsResult.Exists());
        _mockPermissionService.Setup(permissionService => permissionService.PermissionExists("id", permissionId.ToString()))
            .ReturnsAsync(new PermissionExistsResult.Exists());
        _mockRolePermissionsRepo.Setup(rolePermissionRepo => rolePermissionRepo.RoleHasPermission(roleId, permissionId))
            .ReturnsAsync(false);
        _mockRolePermissionsRepo.Setup(rolePermissionRepo => rolePermissionRepo.AddPermissionToRole(
                It.Is<RolePermissionModel>(rolePermission => rolePermission.RoleId == roleId && rolePermission.PermissionId == permissionId))).ReturnsAsync(true);

        // Act
        var result = await _rolePermissionService.AddPermissionToRole(roleId, permissionId);

        // Assert
        Assert.IsInstanceOfType(result, typeof(AddPermissionToRoleResult.Success));
        _mockRolePermissionsRepo.Verify(rolePermissionRepo => rolePermissionRepo.AddPermissionToRole(It.Is<RolePermissionModel>(rolePermission => rolePermission.RoleId == roleId)), Times.Once);
        _mockRolePermissionsRepo.Verify(rolePermissionRepo => rolePermissionRepo.AddPermissionToRole(It.Is<RolePermissionModel>(rolePermission => rolePermission.RoleId == AdminRoleId && rolePermission.PermissionId != permissionId)), Times.Never);
    }

    [TestMethod]
    [DataRow(99L, PermissionId1)]
    public async Task AddPermissionToRole_RoleNotFound_ReturnsRoleNotFound(long roleId, long permissionId)
    {
        // Arrange
        _mockRoleService.Setup(roleService => roleService.RoleExists("id", roleId.ToString()))
            .ReturnsAsync(new RoleExistsResult.NotExists());

        // Act
        var result = await _rolePermissionService.AddPermissionToRole(roleId, permissionId);

        // Assert
        Assert.IsInstanceOfType(result, typeof(AddPermissionToRoleResult.RoleNotFound));
    }

    [TestMethod]
    [DataRow(UserRoleId, 999L)]
    public async Task AddPermissionToRole_PermissionNotFound_ReturnsPermissionNotFound(long roleId, long permissionId)
    {
        // Arrange
        _mockRoleService.Setup(roleService => roleService.RoleExists("id", roleId.ToString()))
            .ReturnsAsync(new RoleExistsResult.Exists());
        _mockPermissionService.Setup(permissionService => permissionService.PermissionExists("id", permissionId.ToString()))
            .ReturnsAsync(new PermissionExistsResult.NotExists());

        // Act
        var result = await _rolePermissionService.AddPermissionToRole(roleId, permissionId);

        // Assert
        Assert.IsInstanceOfType(result, typeof(AddPermissionToRoleResult.PermissionNotFound));
    }

    [TestMethod]
    [DataRow(UserRoleId, PermissionId1)]
    public async Task AddPermissionToRole_AlreadyAssigned_ReturnsAlreadyAssigned(long roleId, long permissionId)
    {
        // Arrange
        _mockRoleService.Setup(roleService => roleService.RoleExists("id", roleId.ToString()))
            .ReturnsAsync(new RoleExistsResult.Exists());
        _mockPermissionService.Setup(permissionService => permissionService.PermissionExists("id", permissionId.ToString()))
            .ReturnsAsync(new PermissionExistsResult.Exists());
        _mockRolePermissionsRepo.Setup(rolePermissionRepo => rolePermissionRepo.RoleHasPermission(roleId, permissionId))
            .ReturnsAsync(true);

        // Act
        var result = await _rolePermissionService.AddPermissionToRole(roleId, permissionId);

        // Assert
        Assert.IsInstanceOfType(result, typeof(AddPermissionToRoleResult.AlreadyAssigned));
        _mockRolePermissionsRepo.Verify(rolePermissionRepo => rolePermissionRepo.AddPermissionToRole(It.IsAny<RolePermissionModel>()), Times.Never);
    }

    [TestMethod]
    [DataRow(UserRoleId, PermissionId1)]
    public async Task AddPermissionToRole_DbAddFails_ReturnsError(long roleId, long permissionId)
    {
        // Arrange
        _mockRoleService.Setup(roleService => roleService.RoleExists("id", roleId.ToString()))
            .ReturnsAsync(new RoleExistsResult.Exists());
        _mockPermissionService.Setup(permissionService => permissionService.PermissionExists("id", permissionId.ToString()))
            .ReturnsAsync(new PermissionExistsResult.Exists());
        _mockRolePermissionsRepo.Setup(rolePermissionRepo => rolePermissionRepo.RoleHasPermission(roleId, permissionId))
            .ReturnsAsync(false);
        _mockRolePermissionsRepo.Setup(rolePermissionRepo => rolePermissionRepo.AddPermissionToRole(
                It.Is<RolePermissionModel>(rolePermission => rolePermission.RoleId == AdminRoleId && rolePermission.PermissionId == permissionId))).ReturnsAsync(true);
        _mockRolePermissionsRepo.Setup(rolePermissionRepo => rolePermissionRepo.AddPermissionToRole(
                It.Is<RolePermissionModel>(rolePermission => rolePermission.RoleId == roleId && rolePermission.PermissionId == permissionId))).ReturnsAsync(false);

        // Act
        var result = await _rolePermissionService.AddPermissionToRole(roleId, permissionId);

        // Assert
        Assert.IsInstanceOfType(result, typeof(AddPermissionToRoleResult.Error));
        StringAssert.Contains(((AddPermissionToRoleResult.Error)result).Message, "Database operation failed to add permission");
    }

    [TestMethod]
    [DataRow(UserRoleId, PermissionId1)]
    public async Task AddPermissionToRole_AutoAdminAddFails_ReturnsError(long roleId, long permissionId)
    {
        // Arrange
        _mockRoleService.Setup(roleService => roleService.RoleExists("id", roleId.ToString()))
            .ReturnsAsync(new RoleExistsResult.Exists());
        _mockPermissionService.Setup(permissionService => permissionService.PermissionExists("id", permissionId.ToString())).ReturnsAsync(new PermissionExistsResult.Exists());
        _mockRolePermissionsRepo.Setup(rolePermissionRepo => rolePermissionRepo.RoleHasPermission(roleId, permissionId)).ReturnsAsync(false);
        _mockRolePermissionsRepo.Setup(rolePermissionRepo => rolePermissionRepo.AddPermissionToRole(
                It.Is<RolePermissionModel>(rolePermission => rolePermission.RoleId == AdminRoleId && rolePermission.PermissionId == permissionId))).ReturnsAsync(false);

        // Act
        var result = await _rolePermissionService.AddPermissionToRole(roleId, permissionId);

        // Assert
        Assert.IsInstanceOfType(result, typeof(AddPermissionToRoleResult.Error));
        StringAssert.Contains(((AddPermissionToRoleResult.Error)result).Message, "Failed to automatically assign permission to ADMIN role");
        _mockRolePermissionsRepo.Verify(rolePermissionRepo => rolePermissionRepo.AddPermissionToRole(
            It.Is<RolePermissionModel>(rolePermission => rolePermission.RoleId == roleId)), Times.Never);
    }

    #endregion

    #region RemovePermissionFromRole

    [TestMethod]
    [DataRow(UserRoleId, PermissionId1)]
    public async Task RemovePermissionFromRole_Success_ReturnsSuccess(long roleId, long permissionId)
    {
        // Arrange
        _mockRolePermissionsRepo.Setup(rolePermissionRepo => rolePermissionRepo.RoleHasPermission(roleId, permissionId))
            .ReturnsAsync(true);
        _mockRolePermissionsRepo.Setup(rolePermissionRepo => rolePermissionRepo.RemovePermissionFromRole(
                It.Is<RolePermissionModel>(rolePermission => rolePermission.RoleId == roleId && rolePermission.PermissionId == permissionId))).ReturnsAsync(true);

        // Act
        var result = await _rolePermissionService.RemovePermissionFromRole(roleId, permissionId);

        // Assert
        Assert.IsInstanceOfType(result, typeof(RemovePermissionFromRoleResult.Success));
        _mockRolePermissionsRepo.Verify(rolePermissionRepo => rolePermissionRepo.RemovePermissionFromRole(It.IsAny<RolePermissionModel>()), Times.Once);
    }

    [TestMethod]
    [DataRow(AdminRoleId, PermissionId1)]
    public async Task RemovePermissionFromRole_RemovingFromAdmin_ReturnsForbidden(long roleId, long permissionId)
    {
        // Arrange

        // Act
        var result = await _rolePermissionService.RemovePermissionFromRole(roleId, permissionId);

        // Assert
        Assert.IsInstanceOfType(result, typeof(RemovePermissionFromRoleResult.Forbidden));
        _mockRolePermissionsRepo.Verify(rolePermissionRepo => rolePermissionRepo.RoleHasPermission(It.IsAny<long>(), It.IsAny<long>()), Times.Never);
        _mockRolePermissionsRepo.Verify(rolePermissionRepo => rolePermissionRepo.RemovePermissionFromRole(It.IsAny<RolePermissionModel>()), Times.Never);
    }

    [TestMethod]
    [DataRow(UserRoleId, 999L)]
    public async Task RemovePermissionFromRole_LinkNotFound_ReturnsNotFound(long roleId, long permissionId)
    {
        // Arrange
        _mockRolePermissionsRepo.Setup(rolePermissionRepo => rolePermissionRepo.RoleHasPermission(roleId, permissionId))
            .ReturnsAsync(false);

        // Act
        var result = await _rolePermissionService.RemovePermissionFromRole(roleId, permissionId);

        // Assert
        Assert.IsInstanceOfType(result, typeof(RemovePermissionFromRoleResult.NotFound));
        _mockRolePermissionsRepo.Verify(rolePermissionRepo => rolePermissionRepo.RemovePermissionFromRole(It.IsAny<RolePermissionModel>()), Times.Never);
    }

    [TestMethod]
    [DataRow(UserRoleId, PermissionId1)]
    public async Task RemovePermissionFromRole_DbRemoveFails_ReturnsError(long roleId, long permissionId)
    {
        // Arrange
        _mockRolePermissionsRepo.Setup(rolePermissionRepo => rolePermissionRepo.RoleHasPermission(roleId, permissionId))
            .ReturnsAsync(true);
        _mockRolePermissionsRepo.Setup(rolePermissionRepo => rolePermissionRepo.RemovePermissionFromRole(It.IsAny<RolePermissionModel>()))
            .ReturnsAsync(false);

        // Act
        var result = await _rolePermissionService.RemovePermissionFromRole(roleId, permissionId);

        // Assert
        Assert.IsInstanceOfType(result, typeof(RemovePermissionFromRoleResult.Error));
        StringAssert.Contains(((RemovePermissionFromRoleResult.Error)result).Message, "Database operation failed");
    }

    [TestMethod]
    [DataRow(UserRoleId, PermissionId1)]
    public async Task RemovePermissionFromRole_RepositoryThrows_ReturnsError(long roleId, long permissionId)
    {
        // Arrange
        _mockRolePermissionsRepo.Setup(rolePermissionRepo => rolePermissionRepo.RoleHasPermission(roleId, permissionId))
            .ReturnsAsync(true);
        _mockRolePermissionsRepo.Setup(rolePermissionRepo => rolePermissionRepo.RemovePermissionFromRole(It.IsAny<RolePermissionModel>()))
            .ThrowsAsync(new InvalidOperationException("DB error"));

        // Act
        var result = await _rolePermissionService.RemovePermissionFromRole(roleId, permissionId);

        // Assert
        Assert.IsInstanceOfType(result, typeof(RemovePermissionFromRoleResult.Error));
        StringAssert.Contains(((RemovePermissionFromRoleResult.Error)result).Message, "DB error");
    }

    #endregion
}
