using MobyPark.DTOs.LicensePlate.Request;
using MobyPark.Models;
using MobyPark.Models.Repositories.Interfaces;
using MobyPark.Services;
using MobyPark.Services.Interfaces;
using MobyPark.Services.Results;
using MobyPark.Services.Results.LicensePlate;
using Moq;
using System.Linq.Expressions;
using MobyPark.DTOs.Hotel;

namespace MobyPark.Tests;

[TestClass]
public class HotelServiceTests
{
    #region Setup

    private Mock<IRepository<HotelPassModel>> _mockHotelRepo = null;
    private Mock<IParkingLotService> _mockLotService = null;
    private HotelPassService _hotelService = null;

    [TestInitialize]
    public void TestInitialize()
    {
        _mockHotelRepo = new Mock<IRepository<HotelPassModel>>();
        _mockLotService = new Mock<IParkingLotService>();

        _hotelService = new HotelPassService(
            _mockHotelRepo.Object, 
            _mockLotService.Object);
    }
    #endregion
    
    #region Get
    [TestMethod]
    public async Task GetHotelPassByIdAsync_PassExists_ReturnsOkWithDto()
    {
        // Arrange
        var id = 42L;
        var model = new HotelPassModel
        {
            Id = id,
            LicensePlateNumber = "AB-123-CD",
            ParkingLotId = 5,
            Start = DateTime.UtcNow,
            End = DateTime.UtcNow.AddHours(2),
            ExtraTime = TimeSpan.FromMinutes(30)
        };

        _mockHotelRepo
            .Setup(r => r.FindByIdAsync(id))
            .ReturnsAsync(model);

        // Act
        var result = await _hotelService.GetHotelPassByIdAsync(id);

        // Assert
        Assert.AreEqual(ServiceStatus.Success, result.Status);
        Assert.IsNotNull(result.Data);
        Assert.AreEqual(id, result.Data.Id);
        Assert.AreEqual(model.LicensePlateNumber, result.Data.LicensePlate);
        Assert.AreEqual(model.ParkingLotId, result.Data.ParkingLotId);
        Assert.AreEqual(model.Start, result.Data.Start);
        Assert.AreEqual(model.End, result.Data.End);
        Assert.AreEqual(model.ExtraTime, result.Data.ExtraTime);
    }

    [TestMethod]
    public async Task GetHotelPassByIdAsync_PassDoesNotExist_ReturnsNotFound()
    {
        // Arrange
        var id = 42L;

        _mockHotelRepo
            .Setup(r => r.FindByIdAsync(id))
            .ReturnsAsync((HotelPassModel)null);

        // Act
        var result = await _hotelService.GetHotelPassByIdAsync(id);

        // Assert
        Assert.AreEqual(ServiceStatus.NotFound, result.Status);
        Assert.IsNull(result.Data);
        Assert.IsTrue(result.Error?.Contains("No hotel pass with that id") ?? false);
    }
    
    [TestMethod]
    public async Task GetHotelPassesByLicensePlateAsync_PassesExist_ReturnsOk()
    {
        // Arrange
        string plate = "AA-123-BB";
        var passes = new List<HotelPassModel>
        {
            new HotelPassModel
            {
                Id = 1,
                LicensePlateNumber = plate,
                ParkingLotId = 5,
                Start = DateTime.UtcNow,
                End = DateTime.UtcNow.AddHours(1),
                ExtraTime = TimeSpan.Zero
            }
        };

        _mockHotelRepo
            .Setup(r => r.GetByAsync(It.IsAny<Expression<Func<HotelPassModel, bool>>>()))
            .ReturnsAsync(passes);

        // Act
        var result = await _hotelService.GetHotelPassesByLicensePlateAsync(plate);

        // Assert
        Assert.AreEqual(ServiceStatus.Success, result.Status);
        Assert.IsNotNull(result.Data);
        Assert.AreEqual(1, result.Data.Count);
        Assert.AreEqual(plate, result.Data[0].LicensePlate);
    }

    [TestMethod]
    public async Task GetHotelPassesByLicensePlateAsync_NoPasses_ReturnsNotFound()
    {
        // Arrange
        string plate = "AA-123-BB";

        _mockHotelRepo
            .Setup(r => r.GetByAsync(It.IsAny<Expression<Func<HotelPassModel, bool>>>()))
            .ReturnsAsync(new List<HotelPassModel>());

        // Act
        var result = await _hotelService.GetHotelPassesByLicensePlateAsync(plate);

        // Assert
        Assert.AreEqual(ServiceStatus.NotFound, result.Status);
        Assert.IsNull(result.Data);
        Assert.IsTrue(result.Error?.Contains(plate) ?? false);
    }
    
    [TestMethod]
    public async Task GetActiveHotelPassByLicensePlateAndLotIdAsync_NoActivePass_ReturnsNotFound()
    {
        // Arrange
        long lotId = 5;
        string plate = "AA-123-BB";

        _mockHotelRepo
            .Setup(r => r.GetByAsync(It.IsAny<Expression<Func<HotelPassModel, bool>>>()))
            .ReturnsAsync(new List<HotelPassModel>());

        // Act
        var result =
            await _hotelService.GetActiveHotelPassByLicensePlateAndLotIdAsync(lotId, plate);

        // Assert
        Assert.AreEqual(ServiceStatus.NotFound, result.Status);
        Assert.IsNull(result.Data);
        Assert.IsTrue(result.Error?.Contains("No active hotel pass") ?? false);
    }
    
    [TestMethod]
    public async Task GetHotelPassesByParkingLotIdAsync_PassesExist_ReturnsOkWithList()
    {
        // Arrange
        long lotId = 5;
        var passes = new List<HotelPassModel>
        {
            new HotelPassModel
            {
                Id = 1,
                LicensePlateNumber = "AAA",
                ParkingLotId = lotId,
                Start = DateTime.UtcNow,
                End = DateTime.UtcNow.AddHours(1),
                ExtraTime = TimeSpan.Zero
            },
            new HotelPassModel
            {
                Id = 2,
                LicensePlateNumber = "BBB",
                ParkingLotId = lotId,
                Start = DateTime.UtcNow.AddHours(2),
                End = DateTime.UtcNow.AddHours(3),
                ExtraTime = TimeSpan.FromMinutes(15)
            }
        };

        _mockHotelRepo
            .Setup(r => r.GetByAsync(It.IsAny<Expression<Func<HotelPassModel, bool>>>()))
            .ReturnsAsync(passes);

        // Act
        var result = await _hotelService.GetHotelPassesByParkingLotIdAsync(lotId);

        // Assert
        Assert.AreEqual(ServiceStatus.Success, result.Status);
        Assert.IsNotNull(result.Data);
        Assert.AreEqual(2, result.Data.Count);
        Assert.IsTrue(result.Data.All(x => x.ParkingLotId == lotId));
    }

    [TestMethod]
    public async Task GetHotelPassesByParkingLotIdAsync_NoPasses_ReturnsNotFound()
    {
        // Arrange
        long lotId = 5;

        _mockHotelRepo
            .Setup(r => r.GetByAsync(It.IsAny<Expression<Func<HotelPassModel, bool>>>()))
            .ReturnsAsync(new List<HotelPassModel>());

        // Act
        var result = await _hotelService.GetHotelPassesByParkingLotIdAsync(lotId);

        // Assert
        Assert.AreEqual(ServiceStatus.NotFound, result.Status);
        Assert.IsNull(result.Data);
        Assert.IsTrue(result.Error?.Contains("has no hotel passes") ?? false);
    }
    #endregion
    
    #region Create

    [TestMethod]
    public async Task CreateHotelPass_ValidDto_ReturnsOkAndSaves()
    {
        // Arrange
        var dto = new CreateHotelPassDto
        {
            LicensePlate = " ab-123-cd ",
            ParkingLotId = 5, // int on DTO
            Start = DateTime.SpecifyKind(DateTime.Now.AddHours(1), DateTimeKind.Local),
            End = DateTime.SpecifyKind(DateTime.Now.AddHours(3), DateTimeKind.Local),
            ExtraTime = TimeSpan.FromMinutes(30)
        };

        _mockHotelRepo
            .Setup(r => r.GetByAsync(It.IsAny<Expression<Func<HotelPassModel, bool>>>()))
            .ReturnsAsync(new List<HotelPassModel>());

        _mockLotService
            .Setup(l => l.GetAvailableSpotsForPeriodAsync(
                dto.ParkingLotId,
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>()))
            .ReturnsAsync(ServiceResult<int>.Ok(10));

        _mockHotelRepo
            .Setup(r => r.Add(It.IsAny<HotelPassModel>()))
            .Callback<HotelPassModel>(m =>
            {
                m.Id = 123;
            });

        // Act
        var result = await _hotelService.CreateHotelPassAsync(dto);

        // Assert
        Assert.AreEqual(ServiceStatus.Success, result.Status);
        Assert.IsNotNull(result.Data);
        Assert.AreEqual(123, result.Data.Id);
        Assert.AreEqual("AB-123-CD", result.Data.LicensePlate);
        Assert.AreEqual(dto.ParkingLotId, result.Data.ParkingLotId);

        _mockHotelRepo.Verify(r => r.Add(It.IsAny<HotelPassModel>()), Times.Once);
        _mockHotelRepo.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [TestMethod]
    public async Task CreateHotelPass_DefaultExtraTime_IsUsedWhenNotOverridden()
    {
        // Arrange
        var dto = new CreateHotelPassDto
        {
            LicensePlate = " AA-111-BB ",
            ParkingLotId = 1,
            Start = DateTime.SpecifyKind(DateTime.Now.AddHours(1), DateTimeKind.Local),
            End = DateTime.SpecifyKind(DateTime.Now.AddHours(3), DateTimeKind.Local),
            // ExtraTime not set explicitly -> should be default 00:30:00
        };

        HotelPassModel capturedModel = null;

        _mockHotelRepo
            .Setup(r => r.GetByAsync(It.IsAny<Expression<Func<HotelPassModel, bool>>>()))
            .ReturnsAsync(new List<HotelPassModel>());

        _mockLotService
            .Setup(l => l.GetAvailableSpotsForPeriodAsync(
                dto.ParkingLotId,
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>()))
            .ReturnsAsync(ServiceResult<int>.Ok(5));

        _mockHotelRepo
            .Setup(r => r.Add(It.IsAny<HotelPassModel>()))
            .Callback<HotelPassModel>(m => capturedModel = m);

        // Act
        var result = await _hotelService.CreateHotelPassAsync(dto);

        // Assert
        Assert.AreEqual(ServiceStatus.Success, result.Status);
        Assert.IsNotNull(capturedModel);
        Assert.AreEqual(TimeSpan.FromMinutes(30), capturedModel.ExtraTime);   // default applied
    }

    [TestMethod]
    public async Task CreateHotelPass_EndBeforeOrEqualStart_ReturnsBadRequest()
    {
        // Arrange
        var now = DateTime.Now;
        var dto = new CreateHotelPassDto
        {
            LicensePlate = "AA",
            ParkingLotId = 1,
            Start = now,
            End = now,    // equal
            ExtraTime = TimeSpan.Zero
        };

        // Act
        var result = await _hotelService.CreateHotelPassAsync(dto);

        // Assert
        Assert.AreEqual(ServiceStatus.BadRequest, result.Status);
        Assert.IsTrue(result.Error?.Contains("End must be after Start") ?? false);

        _mockHotelRepo.Verify(r => r.Add(It.IsAny<HotelPassModel>()), Times.Never);
        _mockHotelRepo.Verify(r => r.SaveChangesAsync(), Times.Never);
        _mockLotService.Verify(l => l.GetAvailableSpotsForPeriodAsync(
                It.IsAny<long>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()),
            Times.Never);
    }

    [TestMethod]
    public async Task CreateHotelPass_OverlappingPassExists_ReturnsConflict()
    {
        // Arrange
        var dto = new CreateHotelPassDto
        {
            LicensePlate = "AA",
            ParkingLotId = 1,
            Start = DateTime.SpecifyKind(DateTime.Now.AddHours(1), DateTimeKind.Local),
            End = DateTime.SpecifyKind(DateTime.Now.AddHours(3), DateTimeKind.Local),
            ExtraTime = TimeSpan.Zero
        };

        var existing = new HotelPassModel
        {
            Id = 99,
            LicensePlateNumber = "AA",
            ParkingLotId = 1,
            Start = DateTime.UtcNow.AddHours(1),
            End = DateTime.UtcNow.AddHours(2),
            ExtraTime = TimeSpan.Zero
        };

        _mockHotelRepo
            .Setup(r => r.GetByAsync(It.IsAny<Expression<Func<HotelPassModel, bool>>>()))
            .ReturnsAsync(new List<HotelPassModel> { existing });

        // Act
        var result = await _hotelService.CreateHotelPassAsync(dto);

        // Assert
        Assert.AreEqual(ServiceStatus.Conflict, result.Status);
        Assert.IsTrue(result.Error?.Contains("already a hotel pass") ?? false);

        _mockLotService.Verify(l => l.GetAvailableSpotsForPeriodAsync(
                It.IsAny<long>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()),
            Times.Never);

        _mockHotelRepo.Verify(r => r.Add(It.IsAny<HotelPassModel>()), Times.Never);
    }

    [TestMethod]
    public async Task CreateHotelPass_AvailabilityServiceFails_ReturnsFail()
    {
        // Arrange
        var dto = new CreateHotelPassDto
        {
            LicensePlate = "AA",
            ParkingLotId = 1,
            Start = DateTime.SpecifyKind(DateTime.Now.AddHours(1), DateTimeKind.Local),
            End = DateTime.SpecifyKind(DateTime.Now.AddHours(3), DateTimeKind.Local),
            ExtraTime = TimeSpan.Zero
        };

        _mockHotelRepo
            .Setup(r => r.GetByAsync(It.IsAny<Expression<Func<HotelPassModel, bool>>>()))
            .ReturnsAsync(new List<HotelPassModel>());

        _mockLotService
            .Setup(l => l.GetAvailableSpotsForPeriodAsync(
                dto.ParkingLotId,
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>()))
            .ReturnsAsync(ServiceResult<int>.Fail("availability error"));

        // Act
        var result = await _hotelService.CreateHotelPassAsync(dto);

        // Assert
        Assert.AreEqual(ServiceStatus.Fail, result.Status);
        Assert.IsTrue(result.Error?.Contains("availability error") ?? false);
        _mockHotelRepo.Verify(r => r.Add(It.IsAny<HotelPassModel>()), Times.Never);
    }

    [TestMethod]
    public async Task CreateHotelPass_NoAvailableSpots_ReturnsBadRequest()
    {
        // Arrange
        var dto = new CreateHotelPassDto
        {
            LicensePlate = "AA",
            ParkingLotId = 1,
            Start = DateTime.SpecifyKind(DateTime.Now.AddHours(1), DateTimeKind.Local),
            End = DateTime.SpecifyKind(DateTime.Now.AddHours(3), DateTimeKind.Local),
            ExtraTime = TimeSpan.Zero
        };

        _mockHotelRepo
            .Setup(r => r.GetByAsync(It.IsAny<Expression<Func<HotelPassModel, bool>>>()))
            .ReturnsAsync(new List<HotelPassModel>());

        _mockLotService
            .Setup(l => l.GetAvailableSpotsForPeriodAsync(
                dto.ParkingLotId,
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>()))
            .ReturnsAsync(ServiceResult<int>.Ok(0));

        // Act
        var result = await _hotelService.CreateHotelPassAsync(dto);

        // Assert
        Assert.AreEqual(ServiceStatus.BadRequest, result.Status);
        Assert.IsTrue(result.Error?.Contains("No available spots") ?? false);
        _mockHotelRepo.Verify(r => r.Add(It.IsAny<HotelPassModel>()), Times.Never);
    }

    #endregion
    
   #region Update

    [TestMethod]
    public async Task PatchHotelPass_PassNotFound_ReturnsNotFound()
    {
        // Arrange
        var dto = new PatchHotelPassDto { Id = 1 };

        _mockHotelRepo
            .Setup(r => r.FindByIdAsync(dto.Id))
            .ReturnsAsync((HotelPassModel)null);

        // Act
        var result = await _hotelService.PatchHotelPassAsync(dto);

        // Assert
        Assert.AreEqual(ServiceStatus.NotFound, result.Status);
        Assert.IsTrue(result.Error?.Contains("No pass with id") ?? false);
    }

    [TestMethod]
    public async Task PatchHotelPass_EndBeforeStart_ReturnsBadRequest()
    {
        // Arrange
        var existing = new HotelPassModel
        {
            Id = 1,
            LicensePlateNumber = "AA",
            ParkingLotId = 1,
            Start = DateTime.UtcNow.AddHours(1),
            End = DateTime.UtcNow.AddHours(3),
            ExtraTime = TimeSpan.Zero
        };

        var dto = new PatchHotelPassDto
        {
            Id = 1,
            Start = existing.Start,
            End = existing.Start
        };

        _mockHotelRepo
            .Setup(r => r.FindByIdAsync(existing.Id))
            .ReturnsAsync(existing);

        // Act
        var result = await _hotelService.PatchHotelPassAsync(dto);

        // Assert
        Assert.AreEqual(ServiceStatus.BadRequest, result.Status);
        Assert.IsTrue(result.Error?.Contains("End must be after Start") ?? false);
        _mockHotelRepo.Verify(r => r.Update(It.IsAny<HotelPassModel>()), Times.Never);
    }

    [TestMethod]
    public async Task PatchHotelPass_OverlappingExists_ReturnsConflict()
    {
        // Arrange
        var existing = new HotelPassModel
        {
            Id = 1,
            LicensePlateNumber = "AA",
            ParkingLotId = 1,
            Start = DateTime.UtcNow.AddHours(1),
            End = DateTime.UtcNow.AddHours(3),
            ExtraTime = TimeSpan.Zero
        };

        var overlapping = new HotelPassModel
        {
            Id = 2,
            LicensePlateNumber = "AA",
            ParkingLotId = 1,
            Start = existing.Start.AddMinutes(30),
            End = existing.End.AddMinutes(30),
            ExtraTime = TimeSpan.Zero
        };

        var dto = new PatchHotelPassDto
        {
            Id = 1,
            End = existing.End.AddHours(1)
        };

        _mockHotelRepo
            .Setup(r => r.FindByIdAsync(existing.Id))
            .ReturnsAsync(existing);

        _mockHotelRepo
            .Setup(r => r.GetByAsync(It.IsAny<Expression<Func<HotelPassModel, bool>>>()))
            .ReturnsAsync(new List<HotelPassModel> { overlapping });

        // Act
        var result = await _hotelService.PatchHotelPassAsync(dto);

        // Assert
        Assert.AreEqual(ServiceStatus.Conflict, result.Status);
        Assert.IsTrue(result.Error?.Contains("overlaps") ?? false);
        _mockHotelRepo.Verify(r => r.Update(It.IsAny<HotelPassModel>()), Times.Never);
    }

    [TestMethod]
    public async Task PatchHotelPass_AvailabilityFails_ReturnsFail()
    {
        // Arrange
        var existing = new HotelPassModel
        {
            Id = 1,
            LicensePlateNumber = "AA",
            ParkingLotId = 1,
            Start = DateTime.UtcNow.AddHours(1),
            End = DateTime.UtcNow.AddHours(3),
            ExtraTime = TimeSpan.Zero
        };

        var dto = new PatchHotelPassDto
        {
            Id = 1,
            End = existing.End.AddHours(1)
        };

        _mockHotelRepo
            .Setup(r => r.FindByIdAsync(existing.Id))
            .ReturnsAsync(existing);

        _mockHotelRepo
            .Setup(r => r.GetByAsync(It.IsAny<Expression<Func<HotelPassModel, bool>>>()))
            .ReturnsAsync(new List<HotelPassModel>());

        _mockLotService
            .Setup(l => l.GetAvailableSpotsForPeriodAsync(
                existing.ParkingLotId,
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>()))
            .ReturnsAsync(ServiceResult<int>.Fail("availability error"));

        // Act
        var result = await _hotelService.PatchHotelPassAsync(dto);

        // Assert
        Assert.AreEqual(ServiceStatus.Fail, result.Status);
        Assert.IsTrue(result.Error?.Contains("availability error") ?? false);
        _mockHotelRepo.Verify(r => r.Update(It.IsAny<HotelPassModel>()), Times.Never);
    }

    [TestMethod]
    public async Task PatchHotelPass_NoAvailableSpots_ReturnsBadRequest()
    {
        // Arrange
        var existing = new HotelPassModel
        {
            Id = 1,
            LicensePlateNumber = "AA",
            ParkingLotId = 1,
            Start = DateTime.UtcNow.AddHours(1),
            End = DateTime.UtcNow.AddHours(3),
            ExtraTime = TimeSpan.Zero
        };

        var dto = new PatchHotelPassDto
        {
            Id = 1,
            End = existing.End.AddHours(1)
        };

        _mockHotelRepo
            .Setup(r => r.FindByIdAsync(existing.Id))
            .ReturnsAsync(existing);

        _mockHotelRepo
            .Setup(r => r.GetByAsync(It.IsAny<Expression<Func<HotelPassModel, bool>>>()))
            .ReturnsAsync(new List<HotelPassModel>());

        _mockLotService
            .Setup(l => l.GetAvailableSpotsForPeriodAsync(
                existing.ParkingLotId,
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>()))
            .ReturnsAsync(ServiceResult<int>.Ok(0));

        // Act
        var result = await _hotelService.PatchHotelPassAsync(dto);

        // Assert
        Assert.AreEqual(ServiceStatus.BadRequest, result.Status);
        Assert.IsTrue(result.Error?.Contains("No available spots") ?? false);
        _mockHotelRepo.Verify(r => r.Update(It.IsAny<HotelPassModel>()), Times.Never);
    }

    [TestMethod]
    public async Task PatchHotelPass_ValidUpdate_UpdatesAndReturnsOk()
    {
        // Arrange
        var existing = new HotelPassModel
        {
            Id = 1,
            LicensePlateNumber = "AA",
            ParkingLotId = 1,
            Start = DateTime.UtcNow.AddHours(1),
            End = DateTime.UtcNow.AddHours(3),
            ExtraTime = TimeSpan.Zero
        };

        var newEnd = existing.End.AddHours(1);
        var dto = new PatchHotelPassDto
        {
            Id = 1,
            LicensePlate = " bb-123-cc ",
            End = newEnd,
            ExtraTime = TimeSpan.FromMinutes(15)
        };

        _mockHotelRepo
            .Setup(r => r.FindByIdAsync(existing.Id))
            .ReturnsAsync(existing);

        _mockHotelRepo
            .Setup(r => r.GetByAsync(It.IsAny<Expression<Func<HotelPassModel, bool>>>()))
            .ReturnsAsync(new List<HotelPassModel>());

        _mockLotService
            .Setup(l => l.GetAvailableSpotsForPeriodAsync(
                existing.ParkingLotId,
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>()))
            .ReturnsAsync(ServiceResult<int>.Ok(5));

        // Act
        var result = await _hotelService.PatchHotelPassAsync(dto);

        // Assert
        Assert.AreEqual(ServiceStatus.Success, result.Status);
        Assert.IsNotNull(result.Data);
        Assert.AreEqual(1, result.Data.Id);
        Assert.AreEqual("BB-123-CC", result.Data.LicensePlate);
        Assert.AreEqual(newEnd.ToUniversalTime(), result.Data.End);
        Assert.AreEqual(TimeSpan.FromMinutes(15), result.Data.ExtraTime);

        _mockHotelRepo.Verify(r => r.Update(existing), Times.Once);
        _mockHotelRepo.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    #endregion
    
    #region Delete
    [TestMethod]
    public async Task DeleteHotelPassById_PassNotFound_ReturnsNotFound()
    {
        // Arrange
        long id = 1;
        _mockHotelRepo
            .Setup(r => r.FindByIdAsync(id))
            .ReturnsAsync((HotelPassModel)null);

        // Act
        var result = await _hotelService.DeleteHotelPassByIdAsync(id);

        // Assert
        Assert.AreEqual(ServiceStatus.NotFound, result.Status);
        Assert.IsFalse(result.Data);
        _mockHotelRepo.Verify(r => r.Deletee(It.IsAny<HotelPassModel>()), Times.Never);
    }

    [TestMethod]
    public async Task DeleteHotelPassById_PassFound_DeletesAndReturnsTrue()
    {
        // Arrange
        long id = 1;
        var existing = new HotelPassModel { Id = id };

        _mockHotelRepo
            .Setup(r => r.FindByIdAsync(id))
            .ReturnsAsync(existing);

        // Act
        var result = await _hotelService.DeleteHotelPassByIdAsync(id);

        // Assert
        Assert.AreEqual(ServiceStatus.Success, result.Status);
        Assert.IsTrue(result.Data);
        _mockHotelRepo.Verify(r => r.Deletee(existing), Times.Once);
        _mockHotelRepo.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    #endregion
}
