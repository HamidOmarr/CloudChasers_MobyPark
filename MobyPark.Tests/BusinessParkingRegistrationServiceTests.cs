using System.Linq.Expressions;

using MobyPark.DTOs.Business;
using MobyPark.Models;
using MobyPark.Models.Repositories.Interfaces;
using MobyPark.Services;
using MobyPark.Services.Results;

using Moq;

namespace MobyPark.Tests;

[TestClass]
public sealed class BusinessParkingRegistrationServiceTests
{
    #region Setup
    private Mock<IRepository<BusinessModel>> _mockBusinessRepo = null!;
    private Mock<IRepository<BusinessParkingRegistrationModel>> _mockRegistrationRepo = null!;
    private Mock<IRepository<UserModel>> _mockUserRepo = null!;
    private BusinessParkingRegistrationService _registrationService = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _mockBusinessRepo = new Mock<IRepository<BusinessModel>>();
        _mockUserRepo = new Mock<IRepository<UserModel>>();
        _mockRegistrationRepo = new Mock<IRepository<BusinessParkingRegistrationModel>>();

        _registrationService = new BusinessParkingRegistrationService(
            _mockRegistrationRepo.Object,
            _mockUserRepo.Object,
            _mockBusinessRepo.Object);
    }
    #endregion

    #region CreateBusinessRegistrationAdminAsync

    [TestMethod]
    public async Task CreateBusinessRegistrationAdminAsync_BusinessNotFound_ReturnsNotFound()
    {
        var dto = new CreateBusinessRegAdminDto
        {
            BusinessId = 10,
            LicensePlateNumber = "AB-12-CD",
            Active = true
        };

        _mockBusinessRepo.Setup(r => r.FindByIdAsync(dto.BusinessId)).ReturnsAsync((BusinessModel?)null);

        var result = await _registrationService.CreateBusinessRegistrationAdminAsync(dto);

        Assert.AreEqual(ServiceStatus.NotFound, result.Status);
    }

    [TestMethod]
    public async Task CreateBusinessRegistrationAdminAsync_AlreadyMade_ReturnsConflict()
    {
        var dto = new CreateBusinessRegAdminDto
        {
            BusinessId = 10,
            LicensePlateNumber = "AB-12-CD",
            Active = true
        };

        _mockBusinessRepo.Setup(r => r.FindByIdAsync(dto.BusinessId))
            .ReturnsAsync(new BusinessModel { Id = dto.BusinessId });

        _mockRegistrationRepo
            .Setup(r => r.GetByAsync(It.IsAny<Expression<Func<BusinessParkingRegistrationModel, bool>>>()))
            .ReturnsAsync(new List<BusinessParkingRegistrationModel>
            {
                new BusinessParkingRegistrationModel
                {
                    Id = 1,
                    BusinessId = dto.BusinessId,
                    LicensePlateNumber = dto.LicensePlateNumber,
                    Active = false,
                    LastSinceActive = DateTimeOffset.MinValue
                }
            });

        var result = await _registrationService.CreateBusinessRegistrationAdminAsync(dto);

        Assert.AreEqual(ServiceStatus.Conflict, result.Status);
    }

    [TestMethod]
    public async Task CreateBusinessRegistrationAdminAsync_AlreadyActive_ReturnsConflict()
    {
        var dto = new CreateBusinessRegAdminDto
        {
            BusinessId = 10,
            LicensePlateNumber = "AB-12-CD",
            Active = true
        };

        _mockBusinessRepo.Setup(r => r.FindByIdAsync(dto.BusinessId))
            .ReturnsAsync(new BusinessModel { Id = dto.BusinessId });

        _mockRegistrationRepo
            .SetupSequence(r => r.GetByAsync(It.IsAny<Expression<Func<BusinessParkingRegistrationModel, bool>>>()))
            .ReturnsAsync(new List<BusinessParkingRegistrationModel>())
            .ReturnsAsync(new List<BusinessParkingRegistrationModel>
            {
                new BusinessParkingRegistrationModel
                {
                    Id = 2,
                    BusinessId = 999,
                    LicensePlateNumber = dto.LicensePlateNumber,
                    Active = true,
                    LastSinceActive = DateTimeOffset.MinValue
                }
            });

        var result = await _registrationService.CreateBusinessRegistrationAdminAsync(dto);

        Assert.AreEqual(ServiceStatus.Conflict, result.Status);
    }

    [TestMethod]
    public async Task CreateBusinessRegistrationAdminAsync_Success_ReturnsOk()
    {
        var dto = new CreateBusinessRegAdminDto
        {
            BusinessId = 10,
            LicensePlateNumber = "AB-12-CD",
            Active = true
        };

        _mockBusinessRepo.Setup(r => r.FindByIdAsync(dto.BusinessId))
            .ReturnsAsync(new BusinessModel { Id = dto.BusinessId });

        _mockRegistrationRepo
            .SetupSequence(r => r.GetByAsync(It.IsAny<Expression<Func<BusinessParkingRegistrationModel, bool>>>()))
            .ReturnsAsync(new List<BusinessParkingRegistrationModel>())
            .ReturnsAsync(new List<BusinessParkingRegistrationModel>());

        BusinessParkingRegistrationModel? added = null;
        _mockRegistrationRepo
            .Setup(r => r.Add(It.IsAny<BusinessParkingRegistrationModel>()))
            .Callback<BusinessParkingRegistrationModel>(m =>
            {
                added = m;
                m.Id = 123;
            });

        _mockRegistrationRepo.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);

        var result = await _registrationService.CreateBusinessRegistrationAdminAsync(dto);

        Assert.AreEqual(ServiceStatus.Success, result.Status);
        Assert.IsNotNull(result.Data);
        Assert.AreEqual(123, result.Data.Id);
        Assert.AreEqual(dto.BusinessId, result.Data.BusinessId);
        Assert.AreEqual(dto.LicensePlateNumber, result.Data.LicensePlateNumber);
        Assert.AreEqual(dto.Active, result.Data.Active);

        _mockRegistrationRepo.Verify(r => r.Add(It.IsAny<BusinessParkingRegistrationModel>()), Times.Once);
        _mockRegistrationRepo.Verify(r => r.SaveChangesAsync(), Times.Once);
        Assert.IsNotNull(added);
    }

    #endregion

    #region CreateBusinessRegistrationAsync

    [TestMethod]
    public async Task CreateBusinessRegistrationAsync_UserNotFound_ReturnsNotFound()
    {
        var dto = new CreateBusinessRegDto { LicensePlateNumber = "AB-12-CD", Active = true };
        long userId = 1;

        _mockUserRepo.Setup(r => r.FindByIdAsync(userId)).ReturnsAsync((UserModel?)null);

        var result = await _registrationService.CreateBusinessRegistrationAsync(dto, userId);

        Assert.AreEqual(ServiceStatus.NotFound, result.Status);
    }

    [TestMethod]
    public async Task CreateBusinessRegistrationAsync_UserNoBusiness_ReturnsConflict()
    {
        var dto = new CreateBusinessRegDto { LicensePlateNumber = "AB-12-CD", Active = true };
        long userId = 1;

        _mockUserRepo.Setup(r => r.FindByIdAsync(userId))
            .ReturnsAsync(new UserModel { Id = userId, BusinessId = null });

        var result = await _registrationService.CreateBusinessRegistrationAsync(dto, userId);

        Assert.AreEqual(ServiceStatus.Conflict, result.Status);
    }

    [TestMethod]
    public async Task CreateBusinessRegistrationAsync_BusinessNotFound_ReturnsNotFound()
    {
        var dto = new CreateBusinessRegDto { LicensePlateNumber = "AB-12-CD", Active = true };
        long userId = 1;
        long businessId = 10;

        _mockUserRepo.Setup(r => r.FindByIdAsync(userId))
            .ReturnsAsync(new UserModel { Id = userId, BusinessId = businessId });

        _mockBusinessRepo.Setup(r => r.FindByIdAsync(businessId)).ReturnsAsync((BusinessModel?)null);

        var result = await _registrationService.CreateBusinessRegistrationAsync(dto, userId);

        Assert.AreEqual(ServiceStatus.NotFound, result.Status);
    }

    [TestMethod]
    public async Task CreateBusinessRegistrationAsync_AlreadyActive_ReturnsConflict()
    {
        var dto = new CreateBusinessRegDto { LicensePlateNumber = "AB-12-CD", Active = true };
        long userId = 1;
        long businessId = 10;

        _mockUserRepo.Setup(r => r.FindByIdAsync(userId))
            .ReturnsAsync(new UserModel { Id = userId, BusinessId = businessId });

        _mockBusinessRepo.Setup(r => r.FindByIdAsync(businessId))
            .ReturnsAsync(new BusinessModel { Id = businessId });

        _mockRegistrationRepo
            .Setup(r => r.GetByAsync(It.IsAny<Expression<Func<BusinessParkingRegistrationModel, bool>>>()))
            .ReturnsAsync(new List<BusinessParkingRegistrationModel>
            {
                new BusinessParkingRegistrationModel
                {
                    Id = 2,
                    BusinessId = 999,
                    LicensePlateNumber = dto.LicensePlateNumber,
                    Active = true,
                    LastSinceActive = DateTimeOffset.MinValue
                }
            });

        var result = await _registrationService.CreateBusinessRegistrationAsync(dto, userId);

        Assert.AreEqual(ServiceStatus.Conflict, result.Status);
    }

    [TestMethod]
    public async Task CreateBusinessRegistrationAsync_Success_ReturnsOk()
    {
        var dto = new CreateBusinessRegDto { LicensePlateNumber = "AB-12-CD", Active = true };
        long userId = 1;
        long businessId = 10;

        _mockUserRepo.Setup(r => r.FindByIdAsync(userId))
            .ReturnsAsync(new UserModel { Id = userId, BusinessId = businessId });

        _mockBusinessRepo.Setup(r => r.FindByIdAsync(businessId))
            .ReturnsAsync(new BusinessModel { Id = businessId });

        _mockRegistrationRepo
            .Setup(r => r.GetByAsync(It.IsAny<Expression<Func<BusinessParkingRegistrationModel, bool>>>()))
            .ReturnsAsync(new List<BusinessParkingRegistrationModel>());

        _mockRegistrationRepo
            .Setup(r => r.Add(It.IsAny<BusinessParkingRegistrationModel>()))
            .Callback<BusinessParkingRegistrationModel>(m => m.Id = 123);

        _mockRegistrationRepo.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);

        var result = await _registrationService.CreateBusinessRegistrationAsync(dto, userId);

        Assert.AreEqual(ServiceStatus.Success, result.Status);
        Assert.IsNotNull(result.Data);
        Assert.AreEqual(123, result.Data.Id);
        Assert.AreEqual(businessId, result.Data.BusinessId);
        Assert.AreEqual(dto.LicensePlateNumber, result.Data.LicensePlateNumber);
        Assert.AreEqual(dto.Active, result.Data.Active);

        _mockRegistrationRepo.Verify(r => r.Add(It.IsAny<BusinessParkingRegistrationModel>()), Times.Once);
        _mockRegistrationRepo.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    #endregion

    #region SetBusinessRegistrationActiveAdminAsync

    [TestMethod]
    public async Task SetBusinessRegistrationActiveAdminAsync_NotFound_ReturnsNotFound()
    {
        var dto = new PatchBusinessRegDto { Id = 1, Active = false };

        _mockRegistrationRepo.Setup(r => r.FindByIdAsync(dto.Id)).ReturnsAsync((BusinessParkingRegistrationModel?)null);

        var result = await _registrationService.SetBusinessRegistrationActiveAdminAsync(dto);

        Assert.AreEqual(ServiceStatus.NotFound, result.Status);
    }

    [TestMethod]
    public async Task SetBusinessRegistrationActiveAdminAsync_Success_ReturnsOk()
    {
        var dto = new PatchBusinessRegDto { Id = 1, Active = false };

        var reg = new BusinessParkingRegistrationModel
        {
            Id = dto.Id,
            BusinessId = 10,
            LicensePlateNumber = "AB-12-CD",
            Active = true,
            LastSinceActive = DateTimeOffset.MinValue
        };

        _mockRegistrationRepo.Setup(r => r.FindByIdAsync(dto.Id)).ReturnsAsync(reg);
        _mockRegistrationRepo.Setup(r => r.Update(reg));
        _mockRegistrationRepo.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);

        var result = await _registrationService.SetBusinessRegistrationActiveAdminAsync(dto);

        Assert.AreEqual(ServiceStatus.Success, result.Status);
        Assert.IsNotNull(result.Data);
        Assert.AreEqual(false, result.Data.Active);

        _mockRegistrationRepo.Verify(r => r.Update(It.IsAny<BusinessParkingRegistrationModel>()), Times.Once);
        _mockRegistrationRepo.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    #endregion

    #region SetBusinessRegistrationActiveAsync

    [TestMethod]
    public async Task SetBusinessRegistrationActiveAsync_UserNotFound_ReturnsNotFound()
    {
        var dto = new PatchBusinessRegDto { Id = 1, Active = false };
        long userId = 1;

        _mockUserRepo.Setup(r => r.FindByIdAsync(userId)).ReturnsAsync((UserModel?)null);

        var result = await _registrationService.SetBusinessRegistrationActiveAsync(dto, userId);

        Assert.AreEqual(ServiceStatus.NotFound, result.Status);
    }

    [TestMethod]
    public async Task SetBusinessRegistrationActiveAsync_UserNoBusiness_ReturnsConflict()
    {
        var dto = new PatchBusinessRegDto { Id = 1, Active = false };
        long userId = 1;

        _mockUserRepo.Setup(r => r.FindByIdAsync(userId))
            .ReturnsAsync(new UserModel { Id = userId, BusinessId = null });

        var result = await _registrationService.SetBusinessRegistrationActiveAsync(dto, userId);

        Assert.AreEqual(ServiceStatus.Conflict, result.Status);
    }

    [TestMethod]
    public async Task SetBusinessRegistrationActiveAsync_BusinessNotFound_ReturnsNotFound()
    {
        var dto = new PatchBusinessRegDto { Id = 1, Active = false };
        long userId = 1;
        long businessId = 10;

        _mockUserRepo.Setup(r => r.FindByIdAsync(userId))
            .ReturnsAsync(new UserModel { Id = userId, BusinessId = businessId });

        _mockBusinessRepo.Setup(r => r.FindByIdAsync(businessId)).ReturnsAsync((BusinessModel?)null);

        var result = await _registrationService.SetBusinessRegistrationActiveAsync(dto, userId);

        Assert.AreEqual(ServiceStatus.NotFound, result.Status);
    }

    [TestMethod]
    public async Task SetBusinessRegistrationActiveAsync_RegNotFound_ReturnsNotFound()
    {
        var dto = new PatchBusinessRegDto { Id = 1, Active = false };
        long userId = 1;
        long businessId = 10;

        _mockUserRepo.Setup(r => r.FindByIdAsync(userId))
            .ReturnsAsync(new UserModel { Id = userId, BusinessId = businessId });

        _mockBusinessRepo.Setup(r => r.FindByIdAsync(businessId))
            .ReturnsAsync(new BusinessModel { Id = businessId });

        _mockRegistrationRepo.Setup(r => r.FindByIdAsync(dto.Id)).ReturnsAsync((BusinessParkingRegistrationModel?)null);

        var result = await _registrationService.SetBusinessRegistrationActiveAsync(dto, userId);

        Assert.AreEqual(ServiceStatus.NotFound, result.Status);
    }

    [TestMethod]
    public async Task SetBusinessRegistrationActiveAsync_WrongBusiness_ReturnsConflict()
    {
        var dto = new PatchBusinessRegDto { Id = 1, Active = false };
        long userId = 1;
        long businessId = 10;

        _mockUserRepo.Setup(r => r.FindByIdAsync(userId))
            .ReturnsAsync(new UserModel { Id = userId, BusinessId = businessId });

        _mockBusinessRepo.Setup(r => r.FindByIdAsync(businessId))
            .ReturnsAsync(new BusinessModel { Id = businessId });

        _mockRegistrationRepo.Setup(r => r.FindByIdAsync(dto.Id))
            .ReturnsAsync(new BusinessParkingRegistrationModel
            {
                Id = dto.Id,
                BusinessId = 999,
                LicensePlateNumber = "AB-12-CD",
                Active = true,
                LastSinceActive = DateTimeOffset.MinValue
            });

        var result = await _registrationService.SetBusinessRegistrationActiveAsync(dto, userId);

        Assert.AreEqual(ServiceStatus.Conflict, result.Status);
    }

    [TestMethod]
    public async Task SetBusinessRegistrationActiveAsync_Success_ReturnsOk()
    {
        var dto = new PatchBusinessRegDto { Id = 1, Active = false };
        long userId = 1;
        long businessId = 10;

        _mockUserRepo.Setup(r => r.FindByIdAsync(userId))
            .ReturnsAsync(new UserModel { Id = userId, BusinessId = businessId });

        _mockBusinessRepo.Setup(r => r.FindByIdAsync(businessId))
            .ReturnsAsync(new BusinessModel { Id = businessId });

        var reg = new BusinessParkingRegistrationModel
        {
            Id = dto.Id,
            BusinessId = businessId,
            LicensePlateNumber = "AB-12-CD",
            Active = true,
            LastSinceActive = DateTimeOffset.MinValue
        };

        _mockRegistrationRepo.Setup(r => r.FindByIdAsync(dto.Id)).ReturnsAsync(reg);
        _mockRegistrationRepo.Setup(r => r.Update(reg));
        _mockRegistrationRepo.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);

        var result = await _registrationService.SetBusinessRegistrationActiveAsync(dto, userId);

        Assert.AreEqual(ServiceStatus.Success, result.Status);
        Assert.IsNotNull(result.Data);
        Assert.AreEqual(false, result.Data.Active);

        _mockRegistrationRepo.Verify(r => r.Update(It.IsAny<BusinessParkingRegistrationModel>()), Times.Once);
        _mockRegistrationRepo.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    #endregion

    #region AdminDeleteBusinessRegistrationAsync

    [TestMethod]
    public async Task AdminDeleteBusinessRegistrationAsync_NotFound_ReturnsNotFound()
    {
        long id = 1;

        _mockRegistrationRepo.Setup(r => r.FindByIdAsync(id)).ReturnsAsync((BusinessParkingRegistrationModel?)null);

        var result = await _registrationService.AdminDeleteBusinessRegistrationAsync(id);

        Assert.AreEqual(ServiceStatus.NotFound, result.Status);
    }

    [TestMethod]
    public async Task AdminDeleteBusinessRegistrationAsync_Success_ReturnsOk()
    {
        long id = 1;
        var reg = new BusinessParkingRegistrationModel
        {
            Id = id,
            BusinessId = 10,
            LicensePlateNumber = "AB-12-CD",
            Active = true,
            LastSinceActive = DateTimeOffset.MinValue
        };

        _mockRegistrationRepo.Setup(r => r.FindByIdAsync(id)).ReturnsAsync(reg);
        _mockRegistrationRepo.Setup(r => r.Deletee(reg));
        _mockRegistrationRepo.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);

        var result = await _registrationService.AdminDeleteBusinessRegistrationAsync(id);

        Assert.AreEqual(ServiceStatus.Success, result.Status);
        Assert.AreEqual(true, result.Data);

        _mockRegistrationRepo.Verify(r => r.Deletee(reg), Times.Once);
        _mockRegistrationRepo.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    #endregion

    #region GetBusinessRegistrationByIdAsync

    [TestMethod]
    public async Task GetBusinessRegistrationByIdAsync_NotFound_ReturnsNotFound()
    {
        long id = 1;

        _mockRegistrationRepo.Setup(r => r.FindByIdAsync(id)).ReturnsAsync((BusinessParkingRegistrationModel?)null);

        var result = await _registrationService.GetBusinessRegistrationByIdAsync(id);

        Assert.AreEqual(ServiceStatus.NotFound, result.Status);
    }

    [TestMethod]
    public async Task GetBusinessRegistrationByIdAsync_Success_ReturnsOk()
    {
        long id = 1;
        var reg = new BusinessParkingRegistrationModel
        {
            Id = id,
            BusinessId = 10,
            LicensePlateNumber = "AB-12-CD",
            Active = true,
            LastSinceActive = DateTimeOffset.MinValue
        };

        _mockRegistrationRepo.Setup(r => r.FindByIdAsync(id)).ReturnsAsync(reg);

        var result = await _registrationService.GetBusinessRegistrationByIdAsync(id);

        Assert.AreEqual(ServiceStatus.Success, result.Status);
        Assert.IsNotNull(result.Data);
        Assert.AreEqual(id, result.Data.Id);
    }

    #endregion

    #region GetBusinessRegistrationsByBusinessAsync

    [TestMethod]
    public async Task GetBusinessRegistrationsByBusinessAsync_Empty_ReturnsNotFound()
    {
        long businessId = 10;

        _mockRegistrationRepo.Setup(r => r.GetByAsync(It.IsAny<Expression<Func<BusinessParkingRegistrationModel, bool>>>()))
            .ReturnsAsync(new List<BusinessParkingRegistrationModel>());

        var result = await _registrationService.GetBusinessRegistrationsByBusinessAsync(businessId);

        Assert.AreEqual(ServiceStatus.NotFound, result.Status);
    }

    [TestMethod]
    public async Task GetBusinessRegistrationsByBusinessAsync_Success_ReturnsOk()
    {
        long businessId = 10;

        _mockRegistrationRepo.Setup(r => r.GetByAsync(It.IsAny<Expression<Func<BusinessParkingRegistrationModel, bool>>>()))
            .ReturnsAsync(new List<BusinessParkingRegistrationModel>
            {
                new BusinessParkingRegistrationModel
                {
                    Id = 1,
                    BusinessId = businessId,
                    LicensePlateNumber = "AB-12-CD",
                    Active = true,
                    LastSinceActive = DateTimeOffset.MinValue
                }
            });

        var result = await _registrationService.GetBusinessRegistrationsByBusinessAsync(businessId);

        Assert.AreEqual(ServiceStatus.Success, result.Status);
        Assert.IsNotNull(result.Data);
        Assert.AreEqual(1, result.Data.Count);
        Assert.AreEqual(businessId, result.Data[0].BusinessId);
    }

    #endregion

    #region GetBusinessRegistrationByLicensePlateAsync

    [TestMethod]
    public async Task GetBusinessRegistrationByLicensePlateAsync_Empty_ReturnsNotFound()
    {
        var plate = "AB-12-CD";

        _mockRegistrationRepo.Setup(r => r.GetByAsync(It.IsAny<Expression<Func<BusinessParkingRegistrationModel, bool>>>()))
            .ReturnsAsync(new List<BusinessParkingRegistrationModel>());

        var result = await _registrationService.GetBusinessRegistrationByLicensePlateAsync(plate);

        Assert.AreEqual(ServiceStatus.NotFound, result.Status);
    }

    [TestMethod]
    public async Task GetBusinessRegistrationByLicensePlateAsync_Success_ReturnsOk()
    {
        var plate = "AB-12-CD";

        _mockRegistrationRepo.Setup(r => r.GetByAsync(It.IsAny<Expression<Func<BusinessParkingRegistrationModel, bool>>>()))
            .ReturnsAsync(new List<BusinessParkingRegistrationModel>
            {
                new BusinessParkingRegistrationModel
                {
                    Id = 1,
                    BusinessId = 10,
                    LicensePlateNumber = plate,
                    Active = true,
                    LastSinceActive = DateTimeOffset.MinValue
                }
            });

        var result = await _registrationService.GetBusinessRegistrationByLicensePlateAsync(plate);

        Assert.AreEqual(ServiceStatus.Success, result.Status);
        Assert.IsNotNull(result.Data);
        Assert.AreEqual(1, result.Data.Count);
        Assert.AreEqual(plate, result.Data[0].LicensePlateNumber);
    }

    #endregion

    #region GetActiveBusinessRegistrationByLicencePlateAsync

    [TestMethod]
    public async Task GetActiveBusinessRegistrationByLicencePlateAsync_NotFound_ReturnsNotFound()
    {
        var plate = "AB-12-CD";

        _mockRegistrationRepo
            .Setup(r => r.GetByAsync(It.IsAny<Expression<Func<BusinessParkingRegistrationModel, bool>>>()))
            .ReturnsAsync(new List<BusinessParkingRegistrationModel>());

        var result = await _registrationService.GetActiveBusinessRegistrationByLicencePlateAsync(plate);

        Assert.AreEqual(ServiceStatus.NotFound, result.Status);
    }

    [TestMethod]
    public async Task GetActiveBusinessRegistrationByLicencePlateAsync_Success_ReturnsOk()
    {
        var plate = "AB-12-CD";

        _mockRegistrationRepo
            .Setup(r => r.GetByAsync(It.IsAny<Expression<Func<BusinessParkingRegistrationModel, bool>>>()))
            .ReturnsAsync(new List<BusinessParkingRegistrationModel>
            {
                new BusinessParkingRegistrationModel
                {
                    Id = 1,
                    BusinessId = 10,
                    LicensePlateNumber = plate,
                    Active = true,
                    LastSinceActive = DateTimeOffset.MinValue
                }
            });

        var result = await _registrationService.GetActiveBusinessRegistrationByLicencePlateAsync(plate);

        Assert.AreEqual(ServiceStatus.Success, result.Status);
        Assert.IsNotNull(result.Data);
        Assert.AreEqual(1, result.Data.Id);
    }

    #endregion
}