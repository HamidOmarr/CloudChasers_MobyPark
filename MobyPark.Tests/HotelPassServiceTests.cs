using System.Linq.Expressions;

using MobyPark.DTOs.Hotel;
using MobyPark.Models;
using MobyPark.Models.Repositories.Interfaces;
using MobyPark.Services;
using MobyPark.Services.Interfaces;
using MobyPark.Services.Results;

using Moq;

namespace MobyPark.Tests;

[TestClass]
public class HotelPassServiceTests
{
    #region Setup

    private Mock<IRepository<HotelPassModel>> _mockPassRepo = null!;
    private Mock<IParkingLotService> _mockLotService = null!;
    private Mock<IRepository<UserModel>> _mockUserRepo = null!;
    private Mock<IRepository<HotelModel>> _mockHotelRepo = null!;
    private Mock<IRepository<ParkingLotModel>> _mockLotRepo = null!;

    private HotelPassService _hotelService = null!;


    [TestInitialize]
    public void TestInitialize()
    {
        _mockPassRepo = new Mock<IRepository<HotelPassModel>>();
        _mockLotService = new Mock<IParkingLotService>();
        _mockUserRepo = new Mock<IRepository<UserModel>>();
        _mockHotelRepo = new Mock<IRepository<HotelModel>>();
        _mockLotRepo = new Mock<IRepository<ParkingLotModel>>();

        _hotelService = new HotelPassService(
            _mockPassRepo.Object,
            _mockLotService.Object, _mockUserRepo.Object, _mockHotelRepo.Object, _mockLotRepo.Object);
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

        _mockPassRepo
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

        _mockPassRepo
            .Setup(r => r.FindByIdAsync(id))
            .ReturnsAsync((HotelPassModel)null!);

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

        _mockPassRepo
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

        _mockPassRepo
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

        _mockPassRepo
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

        _mockPassRepo
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

        _mockPassRepo
            .Setup(r => r.GetByAsync(It.IsAny<Expression<Func<HotelPassModel, bool>>>()))
            .ReturnsAsync(new List<HotelPassModel>());

        // Act
        var result = await _hotelService.GetHotelPassesByParkingLotIdAsync(lotId);

        // Assert
        Assert.AreEqual(ServiceStatus.NotFound, result.Status);
        Assert.IsNull(result.Data);
        Assert.IsTrue(result.Error?.Contains("has no hotel passes") ?? false);
    }

    [TestMethod]
    public async Task GetHotelPassesByLicensePlateAndLotIdAsync_PassesExist_ReturnsOkWithList()
    {
        long lotId = 5;
        string plate = "AA-123-BB";

        var passes = new List<HotelPassModel>
        {
            new HotelPassModel
            {
                Id = 1,
                LicensePlateNumber = plate,
                ParkingLotId = lotId,
                Start = DateTime.UtcNow,
                End = DateTime.UtcNow.AddHours(1),
                ExtraTime = TimeSpan.Zero
            }
        };

        _mockPassRepo
            .Setup(r => r.GetByAsync(It.IsAny<Expression<Func<HotelPassModel, bool>>>()))
            .ReturnsAsync(passes);

        var result = await _hotelService.GetHotelPassesByLicensePlateAndLotIdAsync(lotId, plate);

        Assert.AreEqual(ServiceStatus.Success, result.Status);
        Assert.IsNotNull(result.Data);
        Assert.AreEqual(1, result.Data.Count);
        Assert.AreEqual(lotId, result.Data[0].ParkingLotId);
        Assert.AreEqual(plate, result.Data[0].LicensePlate);
    }

    [TestMethod]
    public async Task GetHotelPassesByLicensePlateAndLotIdAsync_NoPasses_ReturnsNotFound()
    {
        long lotId = 5;
        string plate = "AA-123-BB";

        _mockPassRepo
            .Setup(r => r.GetByAsync(It.IsAny<Expression<Func<HotelPassModel, bool>>>()))
            .ReturnsAsync(new List<HotelPassModel>());

        var result = await _hotelService.GetHotelPassesByLicensePlateAndLotIdAsync(lotId, plate);

        Assert.AreEqual(ServiceStatus.NotFound, result.Status);
        Assert.IsNull(result.Data);
        Assert.IsTrue(result.Error?.Contains("No hotel pass found") ?? false);
    }

    [TestMethod]
    public async Task GetActiveHotelPassByLicensePlateAndLotIdAsync_ActivePassExists_ReturnsOkWithDto()
    {
        long lotId = 5;
        string plate = "AA-123-BB";

        var pass = new HotelPassModel
        {
            Id = 10,
            LicensePlateNumber = plate,
            ParkingLotId = lotId,
            Start = DateTime.UtcNow.AddHours(-1),
            End = DateTime.UtcNow.AddHours(1),
            ExtraTime = TimeSpan.FromMinutes(15)
        };

        _mockPassRepo
            .Setup(r => r.GetByAsync(It.IsAny<Expression<Func<HotelPassModel, bool>>>()))
            .ReturnsAsync(new List<HotelPassModel> { pass });

        var result = await _hotelService.GetActiveHotelPassByLicensePlateAndLotIdAsync(lotId, plate);

        Assert.AreEqual(ServiceStatus.Success, result.Status);
        Assert.IsNotNull(result.Data);
        Assert.AreEqual(pass.Id, result.Data.Id);
        Assert.AreEqual(plate, result.Data.LicensePlate);
        Assert.AreEqual(lotId, result.Data.ParkingLotId);
        Assert.AreEqual(pass.Start, result.Data.Start);
        Assert.AreEqual(pass.End, result.Data.End);
        Assert.AreEqual(pass.ExtraTime, result.Data.ExtraTime);
    }

    [TestMethod]
    public async Task GetHotelPassByIdAsync_RepoThrows_ReturnsException()
    {
        long id = 42;

        _mockPassRepo
            .Setup(r => r.FindByIdAsync(id))
            .ThrowsAsync(new Exception("db"));

        var result = await _hotelService.GetHotelPassByIdAsync(id);

        Assert.AreEqual(ServiceStatus.Exception, result.Status);
        Assert.IsNull(result.Data);
        Assert.IsTrue(result.Error?.Contains("Unexpected error occurred") ?? false);
    }

    [TestMethod]
    public async Task GetHotelPassesByParkingLotIdAsync_RepoThrows_ReturnsException()
    {
        long lotId = 5;

        _mockPassRepo
            .Setup(r => r.GetByAsync(It.IsAny<Expression<Func<HotelPassModel, bool>>>()))
            .ThrowsAsync(new Exception("db"));

        var result = await _hotelService.GetHotelPassesByParkingLotIdAsync(lotId);

        Assert.AreEqual(ServiceStatus.Exception, result.Status);
        Assert.IsNull(result.Data);
        Assert.IsTrue(result.Error?.Contains("Unexpected error occurred") ?? false);
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
            Start = DateTime.SpecifyKind(DateTime.Now.AddHours(1), DateTimeKind.Local),
            End = DateTime.SpecifyKind(DateTime.Now.AddHours(3), DateTimeKind.Local),
            ExtraTime = TimeSpan.FromMinutes(30)
        };

        long userId = 1;
        long parkingLotId = 5;
        var user = new UserModel { Id = userId, HotelId = 10 };
        var hotel = new HotelModel { Id = 10, HotelParkingLotId = parkingLotId };
        var lot = new ParkingLotModel { Id = parkingLotId };

        _mockUserRepo
            .Setup(r => r.FindByIdAsync(userId))
            .ReturnsAsync(user);

        _mockHotelRepo
            .Setup(r => r.FindByIdAsync(user.HotelId))
            .ReturnsAsync(hotel);

        _mockLotRepo
            .Setup(r => r.FindByIdAsync(hotel.HotelParkingLotId))
            .ReturnsAsync(lot);

        _mockPassRepo
            .Setup(r => r.GetByAsync(It.IsAny<Expression<Func<HotelPassModel, bool>>>()))
            .ReturnsAsync(new List<HotelPassModel>());

        _mockLotService
            .Setup(l => l.GetAvailableSpotsForPeriodAsync(
                parkingLotId,
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>()))
            .ReturnsAsync(ServiceResult<int>.Ok(10));

        _mockPassRepo
            .Setup(r => r.Add(It.IsAny<HotelPassModel>()))
            .Callback<HotelPassModel>(m =>
            {
                m.Id = 123;
            });

        // Act
        var result = await _hotelService.CreateHotelPassAsync(dto, userId);

        // Assert
        Assert.AreEqual(ServiceStatus.Success, result.Status);
        Assert.IsNotNull(result.Data);
        Assert.AreEqual(123, result.Data.Id);
        Assert.AreEqual("AB-123-CD", result.Data.LicensePlate);
        Assert.AreEqual(parkingLotId, result.Data.ParkingLotId);

        _mockPassRepo.Verify(r => r.Add(It.IsAny<HotelPassModel>()), Times.Once);
        _mockPassRepo.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [TestMethod]
    public async Task CreateHotelPass_UserNotFound_ReturnsNotFound()
    {
        var dto = new CreateHotelPassDto
        {
            LicensePlate = "AA",
            Start = DateTime.SpecifyKind(DateTime.Now.AddHours(1), DateTimeKind.Local),
            End = DateTime.SpecifyKind(DateTime.Now.AddHours(2), DateTimeKind.Local),
            ExtraTime = TimeSpan.Zero
        };

        long userId = 1;

        _mockUserRepo
            .Setup(r => r.FindByIdAsync(userId))
            .ReturnsAsync((UserModel)null!);

        var result = await _hotelService.CreateHotelPassAsync(dto, userId);

        Assert.AreEqual(ServiceStatus.NotFound, result.Status);
        Assert.IsTrue(result.Error?.Contains("user not found") ?? false);
    }

    [TestMethod]
    public async Task CreateHotelPass_UserHasNoHotelId_ReturnsConflict()
    {
        var dto = new CreateHotelPassDto
        {
            LicensePlate = "AA",
            Start = DateTime.SpecifyKind(DateTime.Now.AddHours(1), DateTimeKind.Local),
            End = DateTime.SpecifyKind(DateTime.Now.AddHours(2), DateTimeKind.Local),
            ExtraTime = TimeSpan.Zero
        };

        long userId = 1;
        var user = new UserModel { Id = userId, HotelId = null };

        _mockUserRepo
            .Setup(r => r.FindByIdAsync(userId))
            .ReturnsAsync(user);

        var result = await _hotelService.CreateHotelPassAsync(dto, userId);

        Assert.AreEqual(ServiceStatus.Conflict, result.Status);
        Assert.IsTrue(result.Error?.Contains("not authorized") ?? false);
    }

    [TestMethod]
    public async Task CreateHotelPass_HotelNotFound_ReturnsNotFound()
    {
        var dto = new CreateHotelPassDto
        {
            LicensePlate = "AA",
            Start = DateTime.SpecifyKind(DateTime.Now.AddHours(1), DateTimeKind.Local),
            End = DateTime.SpecifyKind(DateTime.Now.AddHours(2), DateTimeKind.Local),
            ExtraTime = TimeSpan.Zero
        };

        long userId = 1;
        var user = new UserModel { Id = userId, HotelId = 10 };

        _mockUserRepo
            .Setup(r => r.FindByIdAsync(userId))
            .ReturnsAsync(user);

        _mockHotelRepo
            .Setup(r => r.FindByIdAsync(user.HotelId))
            .ReturnsAsync((HotelModel)null!);

        var result = await _hotelService.CreateHotelPassAsync(dto, userId);

        Assert.AreEqual(ServiceStatus.NotFound, result.Status);
        Assert.IsTrue(result.Error?.Contains("hotel not found") ?? false);
    }

    [TestMethod]
    public async Task CreateHotelPass_ParkingLotNotFound_ReturnsNotFound()
    {
        var dto = new CreateHotelPassDto
        {
            LicensePlate = "AA",
            Start = DateTime.SpecifyKind(DateTime.Now.AddHours(1), DateTimeKind.Local),
            End = DateTime.SpecifyKind(DateTime.Now.AddHours(2), DateTimeKind.Local),
            ExtraTime = TimeSpan.Zero
        };

        long userId = 1;
        var user = new UserModel { Id = userId, HotelId = 10 };
        var hotel = new HotelModel { Id = 10, HotelParkingLotId = 5 };

        _mockUserRepo
            .Setup(r => r.FindByIdAsync(userId))
            .ReturnsAsync(user);

        _mockHotelRepo
            .Setup(r => r.FindByIdAsync(user.HotelId))
            .ReturnsAsync(hotel);

        _mockLotRepo
            .Setup(r => r.FindByIdAsync(hotel.HotelParkingLotId))
            .ReturnsAsync((ParkingLotModel)null!);

        var result = await _hotelService.CreateHotelPassAsync(dto, userId);

        Assert.AreEqual(ServiceStatus.NotFound, result.Status);
        Assert.IsTrue(result.Error?.Contains("parking lot not found") ?? false);
    }

    [TestMethod]
    public async Task CreateHotelPassAdmin_ValidDto_ReturnsOkAndSaves()
    {
        var dto = new AdminCreateHotelPassDto
        {
            LicensePlate = " ab-123-cd ",
            ParkingLotId = 5,
            Start = DateTime.SpecifyKind(DateTime.Now.AddHours(1), DateTimeKind.Local),
            End = DateTime.SpecifyKind(DateTime.Now.AddHours(3), DateTimeKind.Local),
            ExtraTime = TimeSpan.FromMinutes(30)
        };

        _mockPassRepo
            .Setup(r => r.GetByAsync(It.IsAny<Expression<Func<HotelPassModel, bool>>>()))
            .ReturnsAsync(new List<HotelPassModel>());

        _mockLotService
            .Setup(l => l.GetAvailableSpotsForPeriodAsync(
                dto.ParkingLotId,
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>()))
            .ReturnsAsync(ServiceResult<int>.Ok(10));

        _mockPassRepo
            .Setup(r => r.Add(It.IsAny<HotelPassModel>()))
            .Callback<HotelPassModel>(m => m.Id = 123);

        var result = await _hotelService.CreateHotelPassAsync(dto);

        Assert.AreEqual(ServiceStatus.Success, result.Status);
        Assert.IsNotNull(result.Data);
        Assert.AreEqual(123, result.Data.Id);
        Assert.AreEqual("AB-123-CD", result.Data.LicensePlate);
        Assert.AreEqual(dto.ParkingLotId, result.Data.ParkingLotId);

        _mockPassRepo.Verify(r => r.Add(It.IsAny<HotelPassModel>()), Times.Once);
        _mockPassRepo.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [TestMethod]
    public async Task CreateHotelPassAdmin_OverlappingPassExists_ReturnsConflict()
    {
        var dto = new AdminCreateHotelPassDto
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
            ParkingLotId = dto.ParkingLotId,
            Start = DateTime.UtcNow.AddHours(1),
            End = DateTime.UtcNow.AddHours(2),
            ExtraTime = TimeSpan.Zero
        };

        _mockPassRepo
            .Setup(r => r.GetByAsync(It.IsAny<Expression<Func<HotelPassModel, bool>>>()))
            .ReturnsAsync(new List<HotelPassModel> { existing });

        var result = await _hotelService.CreateHotelPassAsync(dto);

        Assert.AreEqual(ServiceStatus.Conflict, result.Status);
        Assert.IsTrue(result.Error?.Contains("already a hotel pass") ?? false);

        _mockLotService.Verify(l => l.GetAvailableSpotsForPeriodAsync(
                It.IsAny<long>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()),
            Times.Never);

        _mockPassRepo.Verify(r => r.Add(It.IsAny<HotelPassModel>()), Times.Never);
    }

    [TestMethod]
    public async Task CreateHotelPassAdmin_AvailabilityServiceFails_ReturnsFail()
    {
        var dto = new AdminCreateHotelPassDto
        {
            LicensePlate = "AA",
            ParkingLotId = 1,
            Start = DateTime.SpecifyKind(DateTime.Now.AddHours(1), DateTimeKind.Local),
            End = DateTime.SpecifyKind(DateTime.Now.AddHours(3), DateTimeKind.Local),
            ExtraTime = TimeSpan.Zero
        };

        _mockPassRepo
            .Setup(r => r.GetByAsync(It.IsAny<Expression<Func<HotelPassModel, bool>>>()))
            .ReturnsAsync(new List<HotelPassModel>());

        _mockLotService
            .Setup(l => l.GetAvailableSpotsForPeriodAsync(
                dto.ParkingLotId,
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>()))
            .ReturnsAsync(ServiceResult<int>.Fail("availability error"));

        var result = await _hotelService.CreateHotelPassAsync(dto);

        Assert.AreEqual(ServiceStatus.Fail, result.Status);
        Assert.IsTrue(result.Error?.Contains("availability error") ?? false);
        _mockPassRepo.Verify(r => r.Add(It.IsAny<HotelPassModel>()), Times.Never);
    }

    [TestMethod]
    public async Task CreateHotelPassAdmin_NoAvailableSpots_ReturnsBadRequest()
    {
        var dto = new AdminCreateHotelPassDto
        {
            LicensePlate = "AA",
            ParkingLotId = 1,
            Start = DateTime.SpecifyKind(DateTime.Now.AddHours(1), DateTimeKind.Local),
            End = DateTime.SpecifyKind(DateTime.Now.AddHours(3), DateTimeKind.Local),
            ExtraTime = TimeSpan.Zero
        };

        _mockPassRepo
            .Setup(r => r.GetByAsync(It.IsAny<Expression<Func<HotelPassModel, bool>>>()))
            .ReturnsAsync(new List<HotelPassModel>());

        _mockLotService
            .Setup(l => l.GetAvailableSpotsForPeriodAsync(
                dto.ParkingLotId,
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>()))
            .ReturnsAsync(ServiceResult<int>.Ok(0));

        var result = await _hotelService.CreateHotelPassAsync(dto);

        Assert.AreEqual(ServiceStatus.BadRequest, result.Status);
        Assert.IsTrue(result.Error?.Contains("No available spots") ?? false);
        _mockPassRepo.Verify(r => r.Add(It.IsAny<HotelPassModel>()), Times.Never);
    }

    [TestMethod]
    public async Task CreateHotelPassAdmin_EndBeforeOrEqualStart_ReturnsBadRequest()
    {
        var now = DateTime.Now;
        var dto = new AdminCreateHotelPassDto
        {
            LicensePlate = "AA",
            ParkingLotId = 1,
            Start = now,
            End = now,
            ExtraTime = TimeSpan.Zero
        };

        var result = await _hotelService.CreateHotelPassAsync(dto);

        Assert.AreEqual(ServiceStatus.BadRequest, result.Status);
        Assert.IsTrue(result.Error?.Contains("End must be after Start") ?? false);

        _mockPassRepo.Verify(r => r.Add(It.IsAny<HotelPassModel>()), Times.Never);
        _mockPassRepo.Verify(r => r.SaveChangesAsync(), Times.Never);
        _mockLotService.Verify(l => l.GetAvailableSpotsForPeriodAsync(
                It.IsAny<long>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()),
            Times.Never);
    }

    [TestMethod]
    public async Task CreateHotelPass_DefaultExtraTime_IsUsedWhenNotOverridden()
    {
        // Arrange
        var dto = new CreateHotelPassDto
        {
            LicensePlate = " AA-111-BB ",
            Start = DateTime.SpecifyKind(DateTime.Now.AddHours(1), DateTimeKind.Local),
            End = DateTime.SpecifyKind(DateTime.Now.AddHours(3), DateTimeKind.Local),
        };

        long userId = 1;
        long parkingLotId = 1;
        var user = new UserModel { Id = userId, HotelId = 10 };
        var hotel = new HotelModel { Id = 10, HotelParkingLotId = parkingLotId };
        var lot = new ParkingLotModel { Id = parkingLotId };

        _mockUserRepo
            .Setup(r => r.FindByIdAsync(userId))
            .ReturnsAsync(user);

        _mockHotelRepo
            .Setup(r => r.FindByIdAsync(user.HotelId))
            .ReturnsAsync(hotel);

        _mockLotRepo
            .Setup(r => r.FindByIdAsync(hotel.HotelParkingLotId))
            .ReturnsAsync(lot);

        HotelPassModel capturedModel = null!;

        _mockPassRepo
            .Setup(r => r.GetByAsync(It.IsAny<Expression<Func<HotelPassModel, bool>>>()))
            .ReturnsAsync(new List<HotelPassModel>());

        _mockLotService
            .Setup(l => l.GetAvailableSpotsForPeriodAsync(
                parkingLotId,
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>()))
            .ReturnsAsync(ServiceResult<int>.Ok(5));

        _mockPassRepo
            .Setup(r => r.Add(It.IsAny<HotelPassModel>()))
            .Callback<HotelPassModel>(m => capturedModel = m);

        // Act
        var result = await _hotelService.CreateHotelPassAsync(dto, userId);

        // Assert
        Assert.AreEqual(ServiceStatus.Success, result.Status);
        Assert.IsNotNull(capturedModel);
        Assert.AreEqual(TimeSpan.FromMinutes(30), capturedModel.ExtraTime);
    }

    [TestMethod]
    public async Task CreateHotelPass_EndBeforeOrEqualStart_ReturnsBadRequest()
    {
        // Arrange
        var now = DateTime.Now;
        var dto = new CreateHotelPassDto
        {
            LicensePlate = "AA",
            Start = now,
            End = now,
            ExtraTime = TimeSpan.Zero
        };

        long userId = 1;
        long parkingLotId = 1;
        var user = new UserModel { Id = userId, HotelId = 10 };
        var hotel = new HotelModel { Id = 10, HotelParkingLotId = parkingLotId };
        var lot = new ParkingLotModel { Id = parkingLotId };

        _mockUserRepo
            .Setup(r => r.FindByIdAsync(userId))
            .ReturnsAsync(user);

        _mockHotelRepo
            .Setup(r => r.FindByIdAsync(user.HotelId))
            .ReturnsAsync(hotel);

        _mockLotRepo
            .Setup(r => r.FindByIdAsync(hotel.HotelParkingLotId))
            .ReturnsAsync(lot);

        // Act
        var result = await _hotelService.CreateHotelPassAsync(dto, userId);

        // Assert
        Assert.AreEqual(ServiceStatus.BadRequest, result.Status);
        Assert.IsTrue(result.Error?.Contains("End must be after Start") ?? false);

        _mockPassRepo.Verify(r => r.Add(It.IsAny<HotelPassModel>()), Times.Never);
        _mockPassRepo.Verify(r => r.SaveChangesAsync(), Times.Never);
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
            Start = DateTime.SpecifyKind(DateTime.Now.AddHours(1), DateTimeKind.Local),
            End = DateTime.SpecifyKind(DateTime.Now.AddHours(3), DateTimeKind.Local),
            ExtraTime = TimeSpan.Zero
        };

        long userId = 1;
        long parkingLotId = 1;
        var user = new UserModel { Id = userId, HotelId = 10 };
        var hotel = new HotelModel { Id = 10, HotelParkingLotId = parkingLotId };
        var lot = new ParkingLotModel { Id = parkingLotId };

        _mockUserRepo
            .Setup(r => r.FindByIdAsync(userId))
            .ReturnsAsync(user);

        _mockHotelRepo
            .Setup(r => r.FindByIdAsync(user.HotelId))
            .ReturnsAsync(hotel);

        _mockLotRepo
            .Setup(r => r.FindByIdAsync(hotel.HotelParkingLotId))
            .ReturnsAsync(lot);

        var existing = new HotelPassModel
        {
            Id = 99,
            LicensePlateNumber = "AA",
            ParkingLotId = parkingLotId,
            Start = DateTime.UtcNow.AddHours(1),
            End = DateTime.UtcNow.AddHours(2),
            ExtraTime = TimeSpan.Zero
        };

        _mockPassRepo
            .Setup(r => r.GetByAsync(It.IsAny<Expression<Func<HotelPassModel, bool>>>()))
            .ReturnsAsync(new List<HotelPassModel> { existing });

        // Act
        var result = await _hotelService.CreateHotelPassAsync(dto, userId);

        // Assert
        Assert.AreEqual(ServiceStatus.Conflict, result.Status);
        Assert.IsTrue(result.Error?.Contains("already a hotel pass") ?? false);

        _mockLotService.Verify(l => l.GetAvailableSpotsForPeriodAsync(
                It.IsAny<long>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()),
            Times.Never);

        _mockPassRepo.Verify(r => r.Add(It.IsAny<HotelPassModel>()), Times.Never);
    }

    [TestMethod]
    public async Task CreateHotelPass_AvailabilityServiceFails_ReturnsFail()
    {
        // Arrange
        var dto = new CreateHotelPassDto
        {
            LicensePlate = "AA",
            Start = DateTime.SpecifyKind(DateTime.Now.AddHours(1), DateTimeKind.Local),
            End = DateTime.SpecifyKind(DateTime.Now.AddHours(3), DateTimeKind.Local),
            ExtraTime = TimeSpan.Zero
        };

        long userId = 1;
        long parkingLotId = 1;
        var user = new UserModel { Id = userId, HotelId = 10 };
        var hotel = new HotelModel { Id = 10, HotelParkingLotId = parkingLotId };
        var lot = new ParkingLotModel { Id = parkingLotId };

        _mockUserRepo
            .Setup(r => r.FindByIdAsync(userId))
            .ReturnsAsync(user);

        _mockHotelRepo
            .Setup(r => r.FindByIdAsync(user.HotelId))
            .ReturnsAsync(hotel);

        _mockLotRepo
            .Setup(r => r.FindByIdAsync(hotel.HotelParkingLotId))
            .ReturnsAsync(lot);

        _mockPassRepo
            .Setup(r => r.GetByAsync(It.IsAny<Expression<Func<HotelPassModel, bool>>>()))
            .ReturnsAsync(new List<HotelPassModel>());

        _mockLotService
            .Setup(l => l.GetAvailableSpotsForPeriodAsync(
                parkingLotId,
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>()))
            .ReturnsAsync(ServiceResult<int>.Fail("availability error"));

        // Act
        var result = await _hotelService.CreateHotelPassAsync(dto, userId);

        // Assert
        Assert.AreEqual(ServiceStatus.Fail, result.Status);
        Assert.IsTrue(result.Error?.Contains("availability error") ?? false);
        _mockPassRepo.Verify(r => r.Add(It.IsAny<HotelPassModel>()), Times.Never);
    }

    [TestMethod]
    public async Task CreateHotelPass_NoAvailableSpots_ReturnsBadRequest()
    {
        // Arrange
        var dto = new CreateHotelPassDto
        {
            LicensePlate = "AA",
            Start = DateTime.SpecifyKind(DateTime.Now.AddHours(1), DateTimeKind.Local),
            End = DateTime.SpecifyKind(DateTime.Now.AddHours(3), DateTimeKind.Local),
            ExtraTime = TimeSpan.Zero
        };

        long userId = 1;
        long parkingLotId = 1;
        var user = new UserModel { Id = userId, HotelId = 10 };
        var hotel = new HotelModel { Id = 10, HotelParkingLotId = parkingLotId };
        var lot = new ParkingLotModel { Id = parkingLotId };

        _mockUserRepo
            .Setup(r => r.FindByIdAsync(userId))
            .ReturnsAsync(user);

        _mockHotelRepo
            .Setup(r => r.FindByIdAsync(user.HotelId))
            .ReturnsAsync(hotel);

        _mockLotRepo
            .Setup(r => r.FindByIdAsync(hotel.HotelParkingLotId))
            .ReturnsAsync(lot);

        _mockPassRepo
            .Setup(r => r.GetByAsync(It.IsAny<Expression<Func<HotelPassModel, bool>>>()))
            .ReturnsAsync(new List<HotelPassModel>());

        _mockLotService
            .Setup(l => l.GetAvailableSpotsForPeriodAsync(
                parkingLotId,
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>()))
            .ReturnsAsync(ServiceResult<int>.Ok(0));

        // Act
        var result = await _hotelService.CreateHotelPassAsync(dto, userId);

        // Assert
        Assert.AreEqual(ServiceStatus.BadRequest, result.Status);
        Assert.IsTrue(result.Error?.Contains("No available spots") ?? false);
        _mockPassRepo.Verify(r => r.Add(It.IsAny<HotelPassModel>()), Times.Never);
    }

    #endregion

    #region Update

    [TestMethod]
    public async Task PatchHotelPass_PassNotFound_ReturnsNotFound()
    {
        // Arrange
        var dto = new PatchHotelPassDto { Id = 1 };

        _mockPassRepo
            .Setup(r => r.FindByIdAsync(dto.Id))
            .ReturnsAsync((HotelPassModel)null!);

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

        _mockPassRepo
            .Setup(r => r.FindByIdAsync(existing.Id))
            .ReturnsAsync(existing);

        // Act
        var result = await _hotelService.PatchHotelPassAsync(dto);

        // Assert
        Assert.AreEqual(ServiceStatus.BadRequest, result.Status);
        Assert.IsTrue(result.Error?.Contains("End must be after Start") ?? false);
        _mockPassRepo.Verify(r => r.Update(It.IsAny<HotelPassModel>()), Times.Never);
    }

    [TestMethod]
    public async Task PatchHotelPassWithUser_UserNotFound_ReturnsNotFound()
    {
        var dto = new PatchHotelPassDto { Id = 1 };
        long userId = 1;

        _mockUserRepo
            .Setup(r => r.FindByIdAsync(userId))
            .ReturnsAsync((UserModel)null!);

        var result = await _hotelService.PatchHotelPassAsync(dto, userId);

        Assert.AreEqual(ServiceStatus.NotFound, result.Status);
        Assert.IsTrue(result.Error?.Contains("user not found") ?? false);
    }

    [TestMethod]
    public async Task PatchHotelPassWithUser_UserHasNoHotelId_ReturnsConflict()
    {
        var dto = new PatchHotelPassDto { Id = 1 };
        long userId = 1;

        _mockUserRepo
            .Setup(r => r.FindByIdAsync(userId))
            .ReturnsAsync(new UserModel { Id = userId, HotelId = null });

        var result = await _hotelService.PatchHotelPassAsync(dto, userId);

        Assert.AreEqual(ServiceStatus.Conflict, result.Status);
        Assert.IsTrue(result.Error?.Contains("not authorized") ?? false);
    }

    [TestMethod]
    public async Task PatchHotelPassWithUser_PassForOtherLot_ReturnsForbidden()
    {
        long userId = 1;
        var user = new UserModel { Id = userId, HotelId = 10 };
        var hotel = new HotelModel { Id = 10, HotelParkingLotId = 1 };
        var lot = new ParkingLotModel { Id = 1 };

        var existing = new HotelPassModel
        {
            Id = 5,
            LicensePlateNumber = "AA",
            ParkingLotId = 2,
            Start = DateTime.UtcNow.AddHours(1),
            End = DateTime.UtcNow.AddHours(2),
            ExtraTime = TimeSpan.Zero
        };

        var dto = new PatchHotelPassDto { Id = existing.Id, End = existing.End.AddHours(1) };

        _mockUserRepo
            .Setup(r => r.FindByIdAsync(userId))
            .ReturnsAsync(user);

        _mockHotelRepo
            .Setup(r => r.FindByIdAsync(user.HotelId))
            .ReturnsAsync(hotel);

        _mockLotRepo
            .Setup(r => r.FindByIdAsync(hotel.HotelParkingLotId))
            .ReturnsAsync(lot);

        _mockPassRepo
            .Setup(r => r.FindByIdAsync(existing.Id))
            .ReturnsAsync(existing);

        var result = await _hotelService.PatchHotelPassAsync(dto, userId);

        Assert.AreEqual(ServiceStatus.Forbidden, result.Status);
        Assert.IsTrue(result.Error?.Contains("Can only update a pass") ?? false);
        _mockPassRepo.Verify(r => r.Update(It.IsAny<HotelPassModel>()), Times.Never);
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

        _mockPassRepo
            .Setup(r => r.FindByIdAsync(existing.Id))
            .ReturnsAsync(existing);

        _mockPassRepo
            .Setup(r => r.GetByAsync(It.IsAny<Expression<Func<HotelPassModel, bool>>>()))
            .ReturnsAsync(new List<HotelPassModel> { overlapping });

        // Act
        var result = await _hotelService.PatchHotelPassAsync(dto);

        // Assert
        Assert.AreEqual(ServiceStatus.Conflict, result.Status);
        Assert.IsTrue(result.Error?.Contains("overlaps") ?? false);
        _mockPassRepo.Verify(r => r.Update(It.IsAny<HotelPassModel>()), Times.Never);
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

        _mockPassRepo
            .Setup(r => r.FindByIdAsync(existing.Id))
            .ReturnsAsync(existing);

        _mockPassRepo
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
        _mockPassRepo.Verify(r => r.Update(It.IsAny<HotelPassModel>()), Times.Never);
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

        _mockPassRepo
            .Setup(r => r.FindByIdAsync(existing.Id))
            .ReturnsAsync(existing);

        _mockPassRepo
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
        _mockPassRepo.Verify(r => r.Update(It.IsAny<HotelPassModel>()), Times.Never);
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

        _mockPassRepo
            .Setup(r => r.FindByIdAsync(existing.Id))
            .ReturnsAsync(existing);

        _mockPassRepo
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

        _mockPassRepo.Verify(r => r.Update(existing), Times.Once);
        _mockPassRepo.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    #endregion

    #region Delete

    [TestMethod]
    public async Task DeleteHotelPassByIdWithUser_UserNotFound_ReturnsNotFound()
    {
        long id = 1;
        long userId = 1;

        _mockUserRepo
            .Setup(r => r.FindByIdAsync(userId))
            .ReturnsAsync((UserModel)null!);

        var result = await _hotelService.DeleteHotelPassByIdAsync(id, userId);

        Assert.AreEqual(ServiceStatus.NotFound, result.Status);
        Assert.IsFalse(result.Data);
        Assert.IsTrue(result.Error?.Contains("user not found") ?? false);
    }

    [TestMethod]
    public async Task DeleteHotelPassByIdWithUser_UserHasNoHotelId_ReturnsConflict()
    {
        long id = 1;
        long userId = 1;

        _mockUserRepo
            .Setup(r => r.FindByIdAsync(userId))
            .ReturnsAsync(new UserModel { Id = userId, HotelId = null });

        var result = await _hotelService.DeleteHotelPassByIdAsync(id, userId);

        Assert.AreEqual(ServiceStatus.Conflict, result.Status);
        Assert.IsFalse(result.Data);
        Assert.IsTrue(result.Error?.Contains("not authorized") ?? false);
    }

    [TestMethod]
    public async Task DeleteHotelPassByIdWithUser_PassNotFound_ReturnsNotFound()
    {
        long id = 1;
        long userId = 1;

        var user = new UserModel { Id = userId, HotelId = 10 };
        var hotel = new HotelModel { Id = 10, HotelParkingLotId = 1 };
        var lot = new ParkingLotModel { Id = 1 };

        _mockUserRepo
            .Setup(r => r.FindByIdAsync(userId))
            .ReturnsAsync(user);

        _mockHotelRepo
            .Setup(r => r.FindByIdAsync(user.HotelId))
            .ReturnsAsync(hotel);

        _mockLotRepo
            .Setup(r => r.FindByIdAsync(hotel.HotelParkingLotId))
            .ReturnsAsync(lot);

        _mockPassRepo
            .Setup(r => r.FindByIdAsync(id))
            .ReturnsAsync((HotelPassModel)null!);

        var result = await _hotelService.DeleteHotelPassByIdAsync(id, userId);

        Assert.AreEqual(ServiceStatus.NotFound, result.Status);
        Assert.IsFalse(result.Data);
        Assert.IsTrue(result.Error?.Contains($"No hotel pass with id {id} found") ?? false);
        _mockPassRepo.Verify(r => r.Deletee(It.IsAny<HotelPassModel>()), Times.Never);
    }

    [TestMethod]
    public async Task DeleteHotelPassByIdWithUser_PassForOtherLot_ReturnsForbidden()
    {
        long id = 1;
        long userId = 1;

        var user = new UserModel { Id = userId, HotelId = 10 };
        var hotel = new HotelModel { Id = 10, HotelParkingLotId = 1 };
        var lot = new ParkingLotModel { Id = 1 };

        var pass = new HotelPassModel
        {
            Id = id,
            ParkingLot = new ParkingLotModel { Id = 2 }
        };

        _mockUserRepo
            .Setup(r => r.FindByIdAsync(userId))
            .ReturnsAsync(user);

        _mockHotelRepo
            .Setup(r => r.FindByIdAsync(user.HotelId))
            .ReturnsAsync(hotel);

        _mockLotRepo
            .Setup(r => r.FindByIdAsync(hotel.HotelParkingLotId))
            .ReturnsAsync(lot);

        _mockPassRepo
            .Setup(r => r.FindByIdAsync(id))
            .ReturnsAsync(pass);

        var result = await _hotelService.DeleteHotelPassByIdAsync(id, userId);

        Assert.AreEqual(ServiceStatus.Forbidden, result.Status);
        Assert.IsFalse(result.Data);
        Assert.IsTrue(result.Error?.Contains("only authorized to delete its own") ?? false);
        _mockPassRepo.Verify(r => r.Deletee(It.IsAny<HotelPassModel>()), Times.Never);
    }

    [TestMethod]
    public async Task DeleteHotelPassByIdWithUser_PassFound_DeletesAndReturnsTrue()
    {
        long id = 1;
        long userId = 1;

        var user = new UserModel { Id = userId, HotelId = 10 };
        var hotel = new HotelModel { Id = 10, HotelParkingLotId = 1 };
        var lot = new ParkingLotModel { Id = 1 };

        var pass = new HotelPassModel
        {
            Id = id,
            ParkingLot = new ParkingLotModel { Id = 1 }
        };

        _mockUserRepo
            .Setup(r => r.FindByIdAsync(userId))
            .ReturnsAsync(user);

        _mockHotelRepo
            .Setup(r => r.FindByIdAsync(user.HotelId))
            .ReturnsAsync(hotel);

        _mockLotRepo
            .Setup(r => r.FindByIdAsync(hotel.HotelParkingLotId))
            .ReturnsAsync(lot);

        _mockPassRepo
            .Setup(r => r.FindByIdAsync(id))
            .ReturnsAsync(pass);

        var result = await _hotelService.DeleteHotelPassByIdAsync(id, userId);

        Assert.AreEqual(ServiceStatus.Success, result.Status);
        Assert.IsTrue(result.Data);
        _mockPassRepo.Verify(r => r.Deletee(pass), Times.Once);
        _mockPassRepo.Verify(r => r.SaveChangesAsync(), Times.Once);
    }
    [TestMethod]
    public async Task DeleteHotelPassById_PassNotFound_ReturnsNotFound()
    {
        // Arrange
        long id = 1;
        _mockPassRepo
            .Setup(r => r.FindByIdAsync(id))
            .ReturnsAsync((HotelPassModel)null!);

        // Act
        var result = await _hotelService.DeleteHotelPassByIdAsync(id);

        // Assert
        Assert.AreEqual(ServiceStatus.NotFound, result.Status);
        Assert.IsFalse(result.Data);
        _mockPassRepo.Verify(r => r.Deletee(It.IsAny<HotelPassModel>()), Times.Never);
    }

    [TestMethod]
    public async Task DeleteHotelPassById_PassFound_DeletesAndReturnsTrue()
    {
        // Arrange
        long id = 1;
        var existing = new HotelPassModel { Id = id };

        _mockPassRepo
            .Setup(r => r.FindByIdAsync(id))
            .ReturnsAsync(existing);

        // Act
        var result = await _hotelService.DeleteHotelPassByIdAsync(id);

        // Assert
        Assert.AreEqual(ServiceStatus.Success, result.Status);
        Assert.IsTrue(result.Data);
        _mockPassRepo.Verify(r => r.Deletee(existing), Times.Once);
        _mockPassRepo.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    #endregion
}