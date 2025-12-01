using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MobyPark.DTOs.Hotel;
using Moq;
using MobyPark.DTOs.ParkingLot.Request;
using MobyPark.DTOs.ParkingSession.Request;
using MobyPark.DTOs.PreAuth.Response;
using MobyPark.Models;
using MobyPark.Models.Repositories.Interfaces;
using MobyPark.Services;
using MobyPark.Services.Interfaces;
using MobyPark.Services.Results;
using MobyPark.Services.Results.ParkingLot;
using MobyPark.Services.Results.ParkingSession;
using MobyPark.Services.Results.Price;
using MobyPark.Services.Results.UserPlate;

namespace MobyPark.Tests;

[TestClass]
public sealed class ParkingSessionServiceTests
{
    #region Setup

    private Mock<IParkingSessionRepository> _mockSessionsRepo = null!;
    private Mock<IUserPlateService> _mockUserPlateService = null!;
    private Mock<IPricingService> _mockPricingService = null!;
    private ParkingSessionService _sessionService = null!;
    private Mock<IGateService> _mockGateService = null!;
    private Mock<IPreAuthService> _mockPreAuthService = null!;
    private Mock<IParkingLotService> _mockParkingLotService = null!;
    private Mock<IHotelPassService> _mockHotelPassService = null;
    
    

    [TestInitialize]
    public void TestInitialize()
    {
        _mockSessionsRepo = new Mock<IParkingSessionRepository>();
        _mockParkingLotService = new Mock<IParkingLotService>();
        _mockUserPlateService = new Mock<IUserPlateService>();
        _mockPricingService = new Mock<IPricingService>();
        _mockGateService = new Mock<IGateService>();
        _mockPreAuthService = new Mock<IPreAuthService>();
        _mockHotelPassService = new Mock<IHotelPassService>();
        

        _sessionService = new ParkingSessionService(
            _mockSessionsRepo.Object,
            _mockParkingLotService.Object,
            _mockUserPlateService.Object,
            _mockPricingService.Object,
            _mockGateService.Object,
            _mockPreAuthService.Object,
            _mockHotelPassService.Object
        );
    }

    #endregion

    #region Create

    [TestMethod]
    [DataRow("AB-12-CD", 1, 2)]
    [DataRow("WX-99-YZ", 5, 10)]
    public async Task CreateParkingSession_ValidDto_ReturnsSuccess(string plate, long lotId, long expectedId)
    {
        // Arrange
        var dto = new CreateParkingSessionDto
        {
            LicensePlate = plate,
            ParkingLotId = lotId,
            Started = DateTime.UtcNow
        };
        string expectedPlate = plate.ToUpper();

        _mockSessionsRepo.Setup(sessionRepo => sessionRepo.GetActiveSessionByLicensePlate(expectedPlate))
            .ReturnsAsync((ParkingSessionModel?)null);

        _mockSessionsRepo.Setup(sessionRepo => sessionRepo.CreateWithId(It.Is<ParkingSessionModel>(
            sessionModel => sessionModel.LicensePlateNumber == expectedPlate &&
                            sessionModel.ParkingLotId == dto.ParkingLotId)))
            .ReturnsAsync((true, expectedId));

        // Act
        var result = await _sessionService.CreateParkingSession(dto);

        // Assert
        Assert.IsInstanceOfType(result, typeof(CreateSessionResult.Success));
        var successResult = (CreateSessionResult.Success)result;
        Assert.AreEqual(expectedId, successResult.Session.Id);
        Assert.AreEqual(expectedPlate, successResult.Session.LicensePlateNumber);
        _mockSessionsRepo.Verify(sessionRepo => sessionRepo.GetActiveSessionByLicensePlate(expectedPlate), Times.Once);
        _mockSessionsRepo.Verify(sessionRepo => sessionRepo.CreateWithId(It.IsAny<ParkingSessionModel>()), Times.Once);
    }

    [TestMethod]
    [DataRow("AB-12-CD", 1, 2)]
    [DataRow("WX-99-YZ", 5, 10)]
    public async Task CreateParkingSession_ActiveSessionExists_ReturnsAlreadyExists(string plate, long lotId, long existingId)
    {
        // Arrange
        var dto = new CreateParkingSessionDto { LicensePlate = plate, ParkingLotId = lotId };
        string expectedPlate = plate.ToUpper();
        var existingSession = new ParkingSessionModel { Id = existingId, LicensePlateNumber = expectedPlate };

        _mockSessionsRepo.Setup(sessionRepo => sessionRepo.GetActiveSessionByLicensePlate(expectedPlate))
            .ReturnsAsync(existingSession);

        // Act
        var result = await _sessionService.CreateParkingSession(dto);

        // Assert
        Assert.IsInstanceOfType(result, typeof(CreateSessionResult.AlreadyExists));
        _mockSessionsRepo.Verify(sessionRepo => sessionRepo.GetActiveSessionByLicensePlate(expectedPlate), Times.Once);
        _mockSessionsRepo.Verify(sessionRepo => sessionRepo.CreateWithId(It.IsAny<ParkingSessionModel>()), Times.Never);
    }

    [TestMethod]
    [DataRow("AB-12-CD", 1)]
    [DataRow("WX-99-YZ", 5)]
    public async Task CreateParkingSession_DatabaseInsertionFails_ReturnsError(string plate, long lotId)
    {
        // Arrange
        var dto = new CreateParkingSessionDto { LicensePlate = plate, ParkingLotId = lotId };
        string expectedPlate = plate.ToUpper();

        _mockSessionsRepo.Setup(sessionRepo => sessionRepo.GetActiveSessionByLicensePlate(expectedPlate))
            .ReturnsAsync((ParkingSessionModel?)null);

        _mockSessionsRepo.Setup(sessionRepo => sessionRepo.CreateWithId(It.IsAny<ParkingSessionModel>()))
            .ReturnsAsync((false, 0L));

        // Act
        var result = await _sessionService.CreateParkingSession(dto);

        // Assert
        Assert.IsInstanceOfType(result, typeof(CreateSessionResult.Error));
        StringAssert.Contains(((CreateSessionResult.Error)result).Message, "Database insertion failed");
    }

    [TestMethod]
    [DataRow("AB-12-CD", 1)]
    [DataRow("WX-99-YZ", 5)]
    public async Task CreateParkingSession_RepositoryThrows_ReturnsError(string plate, long lotId)
    {
        // Arrange
        var dto = new CreateParkingSessionDto { LicensePlate = plate, ParkingLotId = lotId };
        string expectedPlate = plate.ToUpper();

        _mockSessionsRepo.Setup(sessionRepo => sessionRepo.GetActiveSessionByLicensePlate(expectedPlate))
            .ReturnsAsync((ParkingSessionModel?)null);

        _mockSessionsRepo.Setup(sessionRepo => sessionRepo.CreateWithId(It.IsAny<ParkingSessionModel>()))
            .ThrowsAsync(new InvalidOperationException("DB Boom!"));

        // Act
        var result = await _sessionService.CreateParkingSession(dto);

        // Assert
        Assert.IsInstanceOfType(result, typeof(CreateSessionResult.Error));
        StringAssert.Contains(((CreateSessionResult.Error)result).Message, "DB Boom!");
    }

    #endregion

    #region GetById

    [TestMethod]
    [DataRow(1, "AB-12-CD")]
    [DataRow(5, "WX-99-YZ")]
    public async Task GetParkingSessionById_ValidId_ReturnsSuccess(long id, string plate)
    {
        // Arrange
        var expectedSession = new ParkingSessionModel { Id = id, LicensePlateNumber = plate };
        _mockSessionsRepo.Setup(sessionRepo => sessionRepo.GetById<ParkingSessionModel>(id))
            .ReturnsAsync(expectedSession);

        // Act
        var result = await _sessionService.GetParkingSessionById(id);

        // Assert
        Assert.IsInstanceOfType(result, typeof(GetSessionResult.Success));
        Assert.AreEqual(expectedSession, ((GetSessionResult.Success)result).Session);
    }

    [TestMethod]
    [DataRow(99)]
    [DataRow(404)]
    [DataRow(-1)]
    public async Task GetParkingSessionById_InvalidId_ReturnsNotFound(long id)
    {
        // Arrange
        _mockSessionsRepo.Setup(sessionRepo => sessionRepo.GetById<ParkingSessionModel>(id))
            .ReturnsAsync((ParkingSessionModel?)null);

        // Act
        var result = await _sessionService.GetParkingSessionById(id);

        // Assert
        Assert.IsInstanceOfType(result, typeof(GetSessionResult.NotFound));
    }

    #endregion

    #region Update

    [TestMethod]
    [DataRow(1, 10, -2, 5.0, 10.0, 120, 2, 0)]
    [DataRow(2, 20, -1, 7.5, 7.5, 60, 1, 0)]
    public async Task UpdateParkingSession_StopChanged_RecalculatesCostAndReturnsSuccess(
        long id, long lotId, int hoursAgo, double tariff, double cost, int duration,
        int billableHours, int billableDays)
    {
        var stopTime = DateTime.UtcNow;
        var startTime = stopTime.AddHours(hoursAgo);
        var dto = new UpdateParkingSessionDto { Stopped = stopTime };
        var expectedCost = (decimal)cost;
        var expectedDuration = duration;

        var existingSession = new ParkingSessionModel
        {
            Id = id,
            ParkingLotId = lotId,
            LicensePlateNumber = "ABC-123",
            Started = startTime,
            Stopped = null
        };

        var lotModel = new ParkingLotModel
        {
            Id = lotId,
            Name = "Test lot",
            Location = "Somewhere",
            Address = "Teststreet 1",
            Reserved = 0,
            Capacity = 100,
            Tariff = (decimal)tariff,
            DayTariff = 0m
        };

        _mockSessionsRepo
            .Setup(sessionRepo => sessionRepo.GetById<ParkingSessionModel>(id))
            .ReturnsAsync(existingSession);

        var lotDto = new ReadParkingLotDto
        {
            Id = lotModel.Id,
            Name = lotModel.Name,
            Location = lotModel.Location,
            Address = lotModel.Address,
            Reserved = lotModel.Reserved,
            Capacity = lotModel.Capacity,
            Tariff = lotModel.Tariff,
            DayTariff = lotModel.DayTariff
        };

        _mockParkingLotService
            .Setup(x => x.GetParkingLotByIdAsync(lotId))
            .ReturnsAsync(ServiceResult<ReadParkingLotDto>.Ok(lotDto));

        _mockPricingService
            .Setup(ps => ps.CalculateParkingCost(
                It.Is<ParkingLotModel>(pl =>
                    pl.Id == lotId &&
                    pl.Tariff == (decimal)tariff),
                startTime,
                stopTime))
            .Returns(new CalculatePriceResult.Success(expectedCost, billableHours, billableDays));

        _mockSessionsRepo
            .Setup(sessionRepo => sessionRepo.Update(existingSession, dto))
            .ReturnsAsync(true);

        var result = await _sessionService.UpdateParkingSession(id, dto);

        Assert.IsInstanceOfType(result, typeof(UpdateSessionResult.Success));
        var successResult = (UpdateSessionResult.Success)result;
        var updatedSession = successResult.Session;

        Assert.AreEqual(stopTime, updatedSession.Stopped);
        Assert.AreEqual(expectedCost, updatedSession.Cost);

        _mockPricingService.Verify(ps =>
            ps.CalculateParkingCost(
                It.IsAny<ParkingLotModel>(),
                startTime,
                stopTime),
            Times.Once);

        _mockSessionsRepo.Verify(sessionRepo => sessionRepo.Update(existingSession, dto), Times.Once);
    }

    [TestMethod]
    [DataRow(1, ParkingSessionStatus.Paid, ParkingSessionStatus.PreAuthorized)]
    [DataRow(2, ParkingSessionStatus.Failed, ParkingSessionStatus.Paid)]
    public async Task UpdateParkingSession_StatusChangedOnly_ReturnsSuccess(long id, ParkingSessionStatus newStatus, ParkingSessionStatus oldStatus)
    {
        // Arrange
        var dto = new UpdateParkingSessionDto { PaymentStatus = newStatus };
        var existingSession = new ParkingSessionModel
        {
            Id = id,
            Started = DateTime.UtcNow.AddHours(-1),
            Stopped = DateTime.UtcNow,
            Cost = 5,
            PaymentStatus = oldStatus
        };

        _mockSessionsRepo.Setup(sessionRepo => sessionRepo.GetById<ParkingSessionModel>(id))
            .ReturnsAsync(existingSession);

        _mockSessionsRepo.Setup(sessionRepo => sessionRepo.Update(existingSession, dto))
            .ReturnsAsync(true);

        // Act
        var result = await _sessionService.UpdateParkingSession(id, dto);

        // Assert
        Assert.IsInstanceOfType(result, typeof(UpdateSessionResult.Success));
        Assert.AreEqual(newStatus, ((UpdateSessionResult.Success)result).Session.PaymentStatus);

        _mockPricingService.Verify(pricingService => pricingService.CalculateParkingCost(It.IsAny<ParkingLotModel>(), It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>()), Times.Never);
        _mockSessionsRepo.Verify(sessionRepo => sessionRepo.Update(existingSession, dto), Times.Once);
    }

    [TestMethod]
    [DataRow(99)]
    [DataRow(404)]
    public async Task UpdateParkingSession_SessionNotFound_ReturnsNotFound(long id)
    {
        // Arrange
        var dto = new UpdateParkingSessionDto { Stopped = DateTime.UtcNow };
        _mockSessionsRepo.Setup(sessionRepo => sessionRepo.GetById<ParkingSessionModel>(id))
            .ReturnsAsync((ParkingSessionModel?)null);

        // Act
        var result = await _sessionService.UpdateParkingSession(id, dto);

        // Assert
        Assert.IsInstanceOfType(result, typeof(UpdateSessionResult.NotFound));
    }

    [TestMethod]
    [DataRow(1, ParkingSessionStatus.Paid)]
    [DataRow(2, ParkingSessionStatus.Failed)]
    public async Task UpdateParkingSession_NoChanges_ReturnsNoChanges(long id, ParkingSessionStatus status)
    {
        // Arrange
        var dto = new UpdateParkingSessionDto { PaymentStatus = status };
        var existingSession = new ParkingSessionModel { Id = id, PaymentStatus = status };

        _mockSessionsRepo.Setup(sessionRepo => sessionRepo.GetById<ParkingSessionModel>(id))
            .ReturnsAsync(existingSession);

        // Act
        var result = await _sessionService.UpdateParkingSession(id, dto);

        // Assert
        Assert.IsInstanceOfType(result, typeof(UpdateSessionResult.NoChanges));
    }

    [TestMethod]
    [DataRow(1, -1)]
    [DataRow(2, -60)]
    public async Task UpdateParkingSession_StoppedBeforeStarted_ReturnsError(long id, int minutesOffset)
    {
        // Arrange
        var startTime = DateTime.UtcNow;
        var invalidStopTime = DateTime.UtcNow.AddMinutes(minutesOffset);
        var dto = new UpdateParkingSessionDto { Stopped = invalidStopTime };
        var existingSession = new ParkingSessionModel { Id = id, Started = startTime };

        _mockSessionsRepo.Setup(sessionRepo => sessionRepo.GetById<ParkingSessionModel>(id))
            .ReturnsAsync(existingSession);

        // Act
        var result = await _sessionService.UpdateParkingSession(id, dto);

        // Assert
        Assert.IsInstanceOfType(result, typeof(UpdateSessionResult.Error));
        StringAssert.Contains(((UpdateSessionResult.Error)result).Message, "Stopped time cannot be before started time");
    }

    [TestMethod]
    [DataRow(1, 10, 1)]
    [DataRow(5, 55, 3)]
    public async Task UpdateParkingSession_StopChangedPricingFails_ReturnsError(long id, long lotId, int hoursAgo)
    {
        var started = DateTime.UtcNow.AddHours(-hoursAgo);

        var existing = new ParkingSessionModel
        {
            Id = id,
            ParkingLotId = lotId,
            Started = started,
            PaymentStatus = ParkingSessionStatus.PreAuthorized
        };

        var lotDto = new ReadParkingLotDto
        {
            Id = lotId,
            Name = "Test lot",
            Location = "Somewhere",
            Address = "Teststreet 1",
            Reserved = 0,
            Capacity = 100,
            Tariff = 0m,
            DayTariff = 0m
        };

        _mockSessionsRepo.Setup(r => r.GetById<ParkingSessionModel>(id))
            .ReturnsAsync(existing);

        _mockParkingLotService.Setup(p => p.GetParkingLotByIdAsync(lotId))
            .ReturnsAsync(ServiceResult<ReadParkingLotDto>.Ok(lotDto));

        _mockPricingService.Setup(p =>
                p.CalculateParkingCost(
                    It.IsAny<ParkingLotModel>(),
                    It.IsAny<DateTimeOffset>(),
                    It.IsAny<DateTimeOffset>()))
            .Returns(new CalculatePriceResult.Error("Failed to recalculate cost during update."));

        var dto = new UpdateParkingSessionDto
        {
            Stopped = DateTime.UtcNow
        };

        var result = await _sessionService.UpdateParkingSession(id, dto);

        Assert.IsInstanceOfType(result, typeof(UpdateSessionResult.Error));
        StringAssert.Contains(
            ((UpdateSessionResult.Error)result).Message,
            "Failed to recalculate cost during update.");
    }
    
    [TestMethod]
    [DataRow(1, 10, -2)]
    public async Task UpdateParkingSession_LotNotFoundDuringCostRecalc_ReturnsError(long id, long lotId, int hoursAgo)
    {
        var stopTime = DateTime.UtcNow;
        var startTime = stopTime.AddHours(hoursAgo);
        var dto = new UpdateParkingSessionDto { Stopped = stopTime };

        var existingSession = new ParkingSessionModel
        {
            Id = id,
            ParkingLotId = lotId,
            Started = startTime
        };

        _mockSessionsRepo
            .Setup(r => r.GetById<ParkingSessionModel>(id))
            .ReturnsAsync(existingSession);

        _mockParkingLotService
            .Setup(r => r.GetParkingLotByIdAsync(lotId))
            .ReturnsAsync(ServiceResult<ReadParkingLotDto>.NotFound("Parking lot not found"));

        var result = await _sessionService.UpdateParkingSession(id, dto);

        Assert.IsInstanceOfType(result, typeof(UpdateSessionResult.Error));
        StringAssert.Contains(((UpdateSessionResult.Error)result).Message, "Failed to retrieve parking lot for cost recalculation.");
    }
    

    [TestMethod]
    [DataRow(1, ParkingSessionStatus.Paid)]
    public async Task UpdateParkingSession_DatabaseUpdateFails_ReturnsError(long id, ParkingSessionStatus newStatus)
    {
        // Arrange
        var dto = new UpdateParkingSessionDto { PaymentStatus = newStatus };
        var existingSession = new ParkingSessionModel { Id = id, PaymentStatus = ParkingSessionStatus.PreAuthorized };

        _mockSessionsRepo.Setup(sessionRepo => sessionRepo.GetById<ParkingSessionModel>(id))
            .ReturnsAsync(existingSession);

        _mockSessionsRepo.Setup(sessionRepo => sessionRepo.Update(existingSession, dto))
            .ReturnsAsync(false);

        // Act
        var result = await _sessionService.UpdateParkingSession(id, dto);

        // Assert
        Assert.IsInstanceOfType(result, typeof(UpdateSessionResult.Error));
        StringAssert.Contains(((UpdateSessionResult.Error)result).Message, "Session failed to update");
    }

    [TestMethod]
    [DataRow(1, ParkingSessionStatus.Paid)]
    public async Task UpdateParkingSession_RepositoryThrows_ReturnsError(long id, ParkingSessionStatus newStatus)
    {
        // Arrange
        var dto = new UpdateParkingSessionDto { PaymentStatus = newStatus };
        var existingSession = new ParkingSessionModel { Id = id, PaymentStatus = ParkingSessionStatus.PreAuthorized };

        _mockSessionsRepo.Setup(sessionRepo => sessionRepo.GetById<ParkingSessionModel>(id))
            .ReturnsAsync(existingSession);

        _mockSessionsRepo.Setup(sessionRepo => sessionRepo.Update(existingSession, dto))
            .ThrowsAsync(new InvalidOperationException("DB Boom!"));

        // Act
        var result = await _sessionService.UpdateParkingSession(id, dto);

        // Assert
        Assert.IsInstanceOfType(result, typeof(UpdateSessionResult.Error));
        StringAssert.Contains(((UpdateSessionResult.Error)result).Message, "DB Boom!");
    }

    #endregion

    #region Delete

    [TestMethod]
    [DataRow(1)]
    [DataRow(50)]
    public async Task DeleteParkingSession_ValidId_ReturnsSuccess(long id)
    {
        // Arrange
        var session = new ParkingSessionModel { Id = id };
        _mockSessionsRepo.Setup(sessionRepo => sessionRepo.GetById<ParkingSessionModel>(id))
            .ReturnsAsync(session);

        _mockSessionsRepo.Setup(sessionRepo => sessionRepo.Delete(session))
            .ReturnsAsync(true);

        // Act
        var result = await _sessionService.DeleteParkingSession(id);

        // Assert
        Assert.IsInstanceOfType(result, typeof(DeleteSessionResult.Success));
        _mockSessionsRepo.Verify(sessionRepo => sessionRepo.Delete(session), Times.Once);
    }

    [TestMethod]
    [DataRow(99)]
    [DataRow(404)]
    public async Task DeleteParkingSession_NotFound_ReturnsNotFound(long id)
    {
        // Arrange
        _mockSessionsRepo.Setup(sessionRepo => sessionRepo.GetById<ParkingSessionModel>(id))
            .ReturnsAsync((ParkingSessionModel?)null);

        // Act
        var result = await _sessionService.DeleteParkingSession(id);

        // Assert
        Assert.IsInstanceOfType(result, typeof(DeleteSessionResult.NotFound));
        _mockSessionsRepo.Verify(sessionRepo => sessionRepo.Delete(It.IsAny<ParkingSessionModel>()), Times.Never);
    }

    [TestMethod]
    [DataRow(1)]
    [DataRow(10)]
    public async Task DeleteParkingSession_DatabaseDeleteFails_ReturnsError(long id)
    {
        // Arrange
        var session = new ParkingSessionModel { Id = id };
        _mockSessionsRepo.Setup(sessionRepo => sessionRepo.GetById<ParkingSessionModel>(id))
            .ReturnsAsync(session);

        _mockSessionsRepo.Setup(sessionRepo => sessionRepo.Delete(session))
            .ReturnsAsync(false);

        // Act
        var result = await _sessionService.DeleteParkingSession(id);

        // Assert
        Assert.IsInstanceOfType(result, typeof(DeleteSessionResult.Error));
        StringAssert.Contains(((DeleteSessionResult.Error)result).Message, "Database delete failed");
    }

    [TestMethod]
    [DataRow(1)]
    public async Task DeleteParkingSession_RepositoryThrows_ReturnsError(long id)
    {
        // Arrange
        var session = new ParkingSessionModel { Id = id };

        _mockSessionsRepo.Setup(sessionRepo => sessionRepo.GetById<ParkingSessionModel>(id))
            .ReturnsAsync(session);

        _mockSessionsRepo.Setup(sessionRepo => sessionRepo.Delete(session))
            .ThrowsAsync(new InvalidOperationException("DB Boom!"));

        // Act
        var result = await _sessionService.DeleteParkingSession(id);

        // Assert
        Assert.IsInstanceOfType(result, typeof(DeleteSessionResult.Error));
        StringAssert.Contains(((DeleteSessionResult.Error)result).Message, "DB Boom!");
    }

    #endregion

    #region GetByVariousCriteria

    #region GetByParkingLotId

    [TestMethod]
    [DataRow(1, 5)]
    [DataRow(5, 2)]
    public async Task GetParkingSessionsByParkingLotId_ReturnsTotalSessions(long lotId, int totalSessions)
    {
        // Arrange
        var sessions = Enumerable
            .Range(1, totalSessions)
            .Select(i => new ParkingSessionModel { Id = i, ParkingLotId = lotId })
            .ToList();

        _mockSessionsRepo.Setup(sessionRepo => sessionRepo.GetByParkingLotId(lotId))
            .ReturnsAsync(sessions);

        // Act
        var result = await _sessionService.GetParkingSessionsByParkingLotId(lotId);

        // Assert
        Assert.IsInstanceOfType(result, typeof(GetSessionListResult.Success));
        Assert.AreEqual(totalSessions, ((GetSessionListResult.Success)result).Sessions.Count);
    }

    [TestMethod]
    [DataRow(99)]
    public async Task GetParkingSessionsByParkingLotId_NoSessionsFound_ReturnsNotFound(long lotId)
    {
        // Arrange
        _mockSessionsRepo.Setup(sessionRepo => sessionRepo.GetByParkingLotId(lotId))
            .ReturnsAsync(new System.Collections.Generic.List<ParkingSessionModel>());

        // Act
        var result = await _sessionService.GetParkingSessionsByParkingLotId(lotId);

        // Assert
        Assert.IsInstanceOfType(result, typeof(GetSessionListResult.NotFound));
    }

    #endregion

    #region GetByLicensePlate

    [TestMethod]
    [DataRow("AB-12-CD", 1)]
    [DataRow("WX-99-YZ", 5)]
    public async Task GetParkingSessionsByLicensePlate_SessionsFound_ReturnsSuccessList(string plate, int totalSessions)
    {
        // Arrange
        var sessions = Enumerable
            .Range(1, totalSessions)
            .Select(i => new ParkingSessionModel { Id = i, LicensePlateNumber = plate })
            .ToList();

        _mockSessionsRepo.Setup(sessionRepo => sessionRepo.GetByLicensePlateNumber(plate))
            .ReturnsAsync(sessions);

        // Act
        var result = await _sessionService.GetParkingSessionsByLicensePlate(plate);

        // Assert
        Assert.IsInstanceOfType(result, typeof(GetSessionListResult.Success));
        var successResult = (GetSessionListResult.Success)result;
        Assert.AreEqual(totalSessions, successResult.Sessions.Count);
    }

    [TestMethod]
    [DataRow("ZZ-99-YY")]
    [DataRow("00-SE-SS")]
    public async Task GetParkingSessionsByLicensePlate_NoSessionsFound_ReturnsNotFound(string plate)
    {
        // Arrange
        _mockSessionsRepo.Setup(sessionRepo => sessionRepo.GetByLicensePlateNumber(plate))
            .ReturnsAsync(new System.Collections.Generic.List<ParkingSessionModel>());

        // Act
        var result = await _sessionService.GetParkingSessionsByLicensePlate(plate);

        // Assert
        Assert.IsInstanceOfType(result, typeof(GetSessionListResult.NotFound));
    }

    #endregion

    #region GetByPaymentStatus

    [TestMethod]
    [DataRow("Paid", ParkingSessionStatus.Paid, 2)]
    [DataRow("preauthorized", ParkingSessionStatus.PreAuthorized, 1)]
    [DataRow("Failed", ParkingSessionStatus.Failed, 10)]
    public async Task GetParkingSessionsByPaymentStatus_SessionsFound_ReturnsSuccessList(string statusString, ParkingSessionStatus parsedStatus, int totalSessions)
    {
        // Arrange
        var sessions = Enumerable
            .Range(1, totalSessions)
            .Select(i => new ParkingSessionModel { Id = i, PaymentStatus = parsedStatus })
            .ToList();

        _mockSessionsRepo.Setup(sessionRepo => sessionRepo.GetByPaymentStatus(parsedStatus))
            .ReturnsAsync(sessions);

        // Act
        var result = await _sessionService.GetParkingSessionsByPaymentStatus(statusString);

        // Assert
        Assert.IsInstanceOfType(result, typeof(GetSessionListResult.Success));
        var successResult = (GetSessionListResult.Success)result;
        Assert.AreEqual(totalSessions, successResult.Sessions.Count);
    }

    [TestMethod]
    [DataRow("Paid", ParkingSessionStatus.Paid)]
    [DataRow("PreAuthorized", ParkingSessionStatus.PreAuthorized)]
    public async Task GetParkingSessionsByPaymentStatus_NoSessionsFound_ReturnsNotFound(string statusString, ParkingSessionStatus parsedStatus)
    {
        // Arrange
        _mockSessionsRepo.Setup(sessionRepo => sessionRepo.GetByPaymentStatus(parsedStatus))
            .ReturnsAsync(new System.Collections.Generic.List<ParkingSessionModel>());

        // Act
        var result = await _sessionService.GetParkingSessionsByPaymentStatus(statusString);

        // Assert
        Assert.IsInstanceOfType(result, typeof(GetSessionListResult.NotFound));
    }

    [TestMethod]
    [DataRow("NotARealStatus")]
    [DataRow(null)]
    [DataRow(" ")]
    public async Task GetParkingSessionsByPaymentStatus_InvalidStatusString_ReturnsInvalidInput(string statusString)
    {
        // Act
        var result = await _sessionService.GetParkingSessionsByPaymentStatus(statusString);

        // Assert
        Assert.IsInstanceOfType(result, typeof(GetSessionListResult.InvalidInput));
        _mockSessionsRepo.Verify(sessionRepo => sessionRepo.GetByPaymentStatus(It.IsAny<ParkingSessionStatus>()), Times.Never);
    }

    #endregion

    #region GetAll

    [TestMethod]
    [DataRow(1)]
    [DataRow(50)]
    public async Task GetAllParkingSessions_SessionsFound_ReturnsSuccessList(int totalSessions)
    {
        // Arrange
        var sessions = Enumerable
            .Range(1, totalSessions)
            .Select(i => new ParkingSessionModel { Id = i })
            .ToList();

        _mockSessionsRepo.Setup(sessionRepo => sessionRepo.GetAll())
            .ReturnsAsync(sessions);

        // Act
        var result = await _sessionService.GetAllParkingSessions();

        // Assert
        Assert.IsInstanceOfType(result, typeof(GetSessionListResult.Success));
        var successResult = (GetSessionListResult.Success)result;
        Assert.AreEqual(totalSessions, successResult.Sessions.Count);
    }

    [TestMethod]
    public async Task GetAllParkingSessions_NoSessionsFound_ReturnsNotFound()
    {
        // Arrange
        _mockSessionsRepo.Setup(sessionRepo => sessionRepo.GetAll())
            .ReturnsAsync(new System.Collections.Generic.List<ParkingSessionModel>());

        // Act
        var result = await _sessionService.GetAllParkingSessions();

        // Assert
        Assert.IsInstanceOfType(result, typeof(GetSessionListResult.NotFound));
    }

    #endregion

    #region GetActive

    [TestMethod]
    [DataRow(1)]
    [DataRow(20)]
    public async Task GetActiveParkingSessions_SessionsFound_ReturnsSuccessList(int totalSessions)
    {
        // Arrange
        var sessions = Enumerable
            .Range(1, totalSessions)
            .Select(i => new ParkingSessionModel { Id = i, Stopped = null })
            .ToList();

        _mockSessionsRepo.Setup(sessionRepo => sessionRepo.GetActiveSessions())
            .ReturnsAsync(sessions);

        // Act
        var result = await _sessionService.GetActiveParkingSessions();

        // Assert
        Assert.IsInstanceOfType(result, typeof(GetSessionListResult.Success));
        var successResult = (GetSessionListResult.Success)result;
        Assert.AreEqual(totalSessions, successResult.Sessions.Count);
    }

    [TestMethod]
    public async Task GetActiveParkingSessions_NoSessionsFound_ReturnsNotFound()
    {
        // Arrange
        _mockSessionsRepo.Setup(sessionRepo => sessionRepo.GetActiveSessions())
            .ReturnsAsync(new System.Collections.Generic.List<ParkingSessionModel>());

        // Act
        var result = await _sessionService.GetActiveParkingSessions();

        // Assert
        Assert.IsInstanceOfType(result, typeof(GetSessionListResult.NotFound));
    }

    [TestMethod]
    [DataRow("AC-71-VE")]
    [DataRow("AB-12-CD")]
    public async Task GetActiveParkingSessionByLicensePlate_SessionFound_ReturnsSuccess(string plate)
    {
        // Arrange
        var session = new ParkingSessionModel { Id = 1, LicensePlateNumber = plate, Stopped = null };
        _mockSessionsRepo.Setup(sessionRepo => sessionRepo.GetActiveSessionByLicensePlate(plate))
            .ReturnsAsync(session);

        // Act
        var result = await _sessionService.GetActiveParkingSessionByLicensePlate(plate);

        // Assert
        Assert.IsInstanceOfType(result, typeof(GetSessionResult.Success));
        var successResult = (GetSessionResult.Success)result;
        Assert.AreEqual(session.Id, successResult.Session.Id);
    }

    [TestMethod]
    [DataRow("NO-AC-71")]
    [DataRow("00-AC-99")]
    public async Task GetActiveParkingSessionByLicensePlate_NoSessionFound_ReturnsNotFound(string plate)
    {
        // Arrange
        _mockSessionsRepo.Setup(sessionRepo => sessionRepo.GetActiveSessionByLicensePlate(plate))
            .ReturnsAsync((ParkingSessionModel?)null);

        // Act
        var result = await _sessionService.GetActiveParkingSessionByLicensePlate(plate);

        // Assert
        Assert.IsInstanceOfType(result, typeof(GetSessionResult.NotFound));
    }

    #endregion

    #region GetRecent

    [TestMethod]
    [DataRow("RE-CE-57", 1)]
    [DataRow("04-SE-SS", 4)]
    public async Task GetAllRecentParkingSessionsByLicensePlate_SessionsFound_ReturnsSuccessList(string plate, int totalSessions)
    {
        // Arrange
        TimeSpan duration = TimeSpan.FromHours(1);
        string normalizedPlate = plate.ToUpper();
        var sessions = Enumerable
            .Range(1, totalSessions)
            .Select(i => new ParkingSessionModel { Id = i, LicensePlateNumber = normalizedPlate })
            .ToList();

        _mockSessionsRepo.Setup(sessionRepo => sessionRepo.GetAllRecentSessionsByLicensePlate(normalizedPlate, duration))
            .ReturnsAsync(sessions);

        // Act
        var result = await _sessionService.GetAllRecentParkingSessionsByLicensePlate(plate, duration);

        // Assert
        Assert.IsInstanceOfType(result, typeof(GetSessionListResult.Success));
        var successResult = (GetSessionListResult.Success)result;
        Assert.AreEqual(totalSessions, successResult.Sessions.Count);
    }

    [TestMethod]
    [DataRow("NO-RE-57")]
    [DataRow("00-SE-00")]
    public async Task GetAllRecentParkingSessionsByLicensePlate_NoSessionsFound_ReturnsNotFound(string plate)
    {
        // Arrange
        TimeSpan duration = TimeSpan.FromHours(1);
        string normalizedPlate = plate.ToUpper();

        _mockSessionsRepo.Setup(sessionRepo => sessionRepo.GetAllRecentSessionsByLicensePlate(normalizedPlate, duration))
            .ReturnsAsync(new System.Collections.Generic.List<ParkingSessionModel>());

        // Act
        var result = await _sessionService.GetAllRecentParkingSessionsByLicensePlate(plate, duration);

        // Assert
        Assert.IsInstanceOfType(result, typeof(GetSessionListResult.NotFound));
    }

    #endregion

    #region GetAuthSession(s)

    [TestMethod]
    [DataRow(1, 10, 5)]
    [DataRow(99, 1, 100)]
    public async Task GetAuthorizedSessionAsync_AsAdmin_ReturnsSession(long userId, int lotId, int sessionId)
    {
        // Arrange
        var session = new ParkingSessionModel { Id = sessionId, ParkingLotId = lotId };
        _mockSessionsRepo.Setup(sessionRepo => sessionRepo.GetById<ParkingSessionModel>(sessionId))
            .ReturnsAsync(session);

        // Act
        var result = await _sessionService.GetAuthorizedSessionAsync(userId, lotId, sessionId, true);

        // Assert
        Assert.IsInstanceOfType(result, typeof(GetSessionResult.Success));
        _mockUserPlateService.Verify(uPlateService => uPlateService.GetUserPlatesByUserId(It.IsAny<long>()), Times.Never);
    }

    [TestMethod]
    [DataRow(1, 10, 5, 99)]
    [DataRow(2, 20, 6, 1)]
    public async Task GetAuthorizedSessionAsync_AsAdminWrongLot_ReturnsNotFound(long userId, int lotId, int sessionId, int sessionLotId)
    {
        // Arrange
        var session = new ParkingSessionModel { Id = sessionId, ParkingLotId = sessionLotId };
        _mockSessionsRepo.Setup(sessionRepo => sessionRepo.GetById<ParkingSessionModel>(sessionId))
            .ReturnsAsync(session);

        // Act
        var result = await _sessionService.GetAuthorizedSessionAsync(userId, lotId, sessionId, true);

        // Assert
        Assert.IsInstanceOfType(result, typeof(GetSessionResult.NotFound));
    }

    [TestMethod]
    [DataRow(1, 10, 5, "AB-12-CD", -2, -1)]
    [DataRow(99, 1, 50, "XY-98-ZW", -10, -5)]
    public async Task GetAuthorizedSessionAsync_AsUserOwnsSession_ReturnsSuccess(
        long userId, int lotId, int sessionId, string plate, int plateAddedDaysAgo, int sessionStartedDaysAgo)
    {
        // Arrange
        var plateAddedTime = DateTime.UtcNow.AddDays(plateAddedDaysAgo);
        var sessionStartTime = DateTime.UtcNow.AddDays(sessionStartedDaysAgo);

        var session = new ParkingSessionModel
        {
            Id = sessionId,
            ParkingLotId = lotId,
            LicensePlateNumber = plate,
            Started = sessionStartTime
        };
        var userPlates = new System.Collections.Generic.List<UserPlateModel>
        {
            new UserPlateModel { UserId = userId, LicensePlateNumber = plate, CreatedAt = new DateTimeOffset(plateAddedTime, TimeSpan.Zero) }
        };

        _mockSessionsRepo.Setup(sessionRepo => sessionRepo.GetById<ParkingSessionModel>(sessionId))
            .ReturnsAsync(session);

        _mockUserPlateService.Setup(s => s.GetUserPlatesByUserId(userId))
            .ReturnsAsync(new GetUserPlateListResult.Success(userPlates));

        // Act
        var result = await _sessionService.GetAuthorizedSessionAsync(userId, lotId, sessionId, false);

        // Assert
        Assert.IsInstanceOfType(result, typeof(GetSessionResult.Success));
        Assert.AreEqual(session, ((GetSessionResult.Success)result).Session);
    }

    [TestMethod]
    [DataRow(1, 10, 5, "XX-YY-99", "AB-CD-12")]
    [DataRow(2, 20, 6, "AA-BB-11", "CC-DD-22")]
    public async Task GetAuthorizedSessionAsync_AsUserDoesNotOwn_ReturnsForbidden(
        long userId, int lotId, int sessionId, string sessionPlate, string userPlate)
    {
        // Arrange
        var session = new ParkingSessionModel
        {
            Id = sessionId,
            ParkingLotId = lotId,
            LicensePlateNumber = sessionPlate,
            Started = DateTime.UtcNow.AddDays(-1)
        };
        var userPlates = new System.Collections.Generic.List<UserPlateModel>
        {
            new UserPlateModel
            {
                UserId = userId,
                LicensePlateNumber = userPlate,
                CreatedAt = DateTimeOffset.UtcNow.AddDays(-2)
            }
        };

        _mockSessionsRepo.Setup(sessionRepo => sessionRepo.GetById<ParkingSessionModel>(sessionId))
            .ReturnsAsync(session);

        _mockUserPlateService.Setup(s => s.GetUserPlatesByUserId(userId))
            .ReturnsAsync(new GetUserPlateListResult.Success(userPlates));

        // Act
        var result = await _sessionService.GetAuthorizedSessionAsync(userId, lotId, sessionId, false);

        // Assert
        Assert.IsInstanceOfType(result, typeof(GetSessionResult.Forbidden));
    }

    [TestMethod]
    [DataRow(1, 10, 5, "AB-12-CD", -1, -2)]
    [DataRow(99, 1, 50, "XY-98-ZW", -5, -10)]
    public async Task GetAuthorizedSessionAsync_AsUserSessionTooOld_ReturnsForbidden(
        long userId, int lotId, int sessionId, string plate, int plateAddedDaysAgo, int sessionStartedDaysAgo)
    {
        // Arrange
        var plateAddedTime = DateTime.UtcNow.AddDays(plateAddedDaysAgo);
        var sessionStartTime = DateTime.UtcNow.AddDays(sessionStartedDaysAgo); // session started BEFORE plate was added

        var session = new ParkingSessionModel
        {
            Id = sessionId,
            ParkingLotId = lotId,
            LicensePlateNumber = plate,
            Started = sessionStartTime
        };
        var userPlates = new System.Collections.Generic.List<UserPlateModel>
        {
            new UserPlateModel
            {
                UserId = userId,
                LicensePlateNumber = plate,
                CreatedAt = new DateTimeOffset(plateAddedTime, TimeSpan.Zero)
            }
        };

        _mockSessionsRepo.Setup(sessionRepo => sessionRepo.GetById<ParkingSessionModel>(sessionId))
            .ReturnsAsync(session);

        _mockUserPlateService.Setup(uPlateService => uPlateService.GetUserPlatesByUserId(userId))
            .ReturnsAsync(new GetUserPlateListResult.Success(userPlates));

        // Act
        var result = await _sessionService.GetAuthorizedSessionAsync(userId, lotId, sessionId, false);

        // Assert
        Assert.IsInstanceOfType(result, typeof(GetSessionResult.Forbidden));
    }

    [TestMethod]
    [DataRow(1, 10, 5)]
    public async Task GetAuthorizedSessionsAsync_AsAdmin_ReturnsAllSessions(long userId, int lotId, int totalSessions)
    {
        // Arrange
        var sessions = Enumerable
            .Range(1, totalSessions)
            .Select(i => new ParkingSessionModel { Id = i, ParkingLotId = lotId })
            .ToList();

        _mockSessionsRepo.Setup(sessionRepo => sessionRepo.GetByParkingLotId(lotId))
            .ReturnsAsync(sessions);

        // Act
        var result = await _sessionService.GetAuthorizedSessionsAsync(userId, lotId, true);

        // Assert
        Assert.AreEqual(totalSessions, result.Count);
        _mockUserPlateService.Verify(u => u.GetUserPlatesByUserId(It.IsAny<long>()), Times.Never);
    }

    [TestMethod]
    [DataRow(1, 10, "AB-12-CD", -5, -2, "NO-01-LP", -6)]
    [DataRow(5, 20, "WX-99-YZ", -30, -10, "XX-99-XX", -40)]
    public async Task GetAuthorizedSessionsAsync_AsUser_ReturnsOnlyOwnedAndValidSessions(
        long userId, int lotId, string ownedPlate, int plateAddedDaysAgo,
        int ownedSessionStartedDaysAgo, string randomPlate, int tooOldSessionStartedDaysAgo)
    {
        // Arrange
        var now = DateTime.UtcNow;
        var plateAddedTime = now.AddDays(plateAddedDaysAgo);

        var ownedSession = new ParkingSessionModel
        {
            Id = 1,
            ParkingLotId = lotId,
            LicensePlateNumber = ownedPlate,
            Started = now.AddDays(ownedSessionStartedDaysAgo)
        };

        var randomSession = new ParkingSessionModel
        {
            Id = 2,
            ParkingLotId = lotId,
            LicensePlateNumber = randomPlate,
            Started = now.AddDays(ownedSessionStartedDaysAgo)
        };

        var tooOldSession = new ParkingSessionModel
        {
            Id = 3,
            ParkingLotId = lotId,
            LicensePlateNumber = ownedPlate,
            Started = now.AddDays(tooOldSessionStartedDaysAgo)
        };

        _mockSessionsRepo.Setup(sessionRepo => sessionRepo.GetByParkingLotId(lotId))
            .ReturnsAsync(new System.Collections.Generic.List<ParkingSessionModel> { ownedSession, randomSession, tooOldSession });

        var userPlates = new System.Collections.Generic.List<UserPlateModel>
        {
            new UserPlateModel
            {
                UserId = userId,
                LicensePlateNumber = ownedPlate,
                CreatedAt = new DateTimeOffset(plateAddedTime, TimeSpan.Zero)
            }
        };
        _mockUserPlateService.Setup(uPlateService => uPlateService.GetUserPlatesByUserId(userId))
            .ReturnsAsync(new GetUserPlateListResult.Success(userPlates));

        // Act
        var result = await _sessionService.GetAuthorizedSessionsAsync(userId, lotId, false);

        // Assert
        Assert.AreEqual(1, result.Count);
        Assert.AreEqual(ownedSession.Id, result.First().Id);
    }

    [TestMethod]
    [DataRow(1, 10)]
    public async Task GetAuthorizedSessionsAsync_NoSessionsFound_ReturnsEmptyList(long userId, int lotId)
    {
        // Arrange
        _mockSessionsRepo.Setup(r => r.GetByParkingLotId(lotId))
            .ReturnsAsync(new System.Collections.Generic.List<ParkingSessionModel>());

        // Act
        var result = await _sessionService.GetAuthorizedSessionsAsync(userId, lotId, false);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(0, result.Count);
    }

    #endregion

    #endregion

    #region Count

    [TestMethod]
    [DataRow(0)]
    [DataRow(1)]
    [DataRow(123)]
    public async Task CountParkingSessions_ReturnsCountFromRepository(int expectedCount)
    {
        // Arrange
        _mockSessionsRepo.Setup(r => r.Count()).ReturnsAsync(expectedCount);

        // Act
        var result = await _sessionService.CountParkingSessions();

        // Assert
        Assert.AreEqual(expectedCount, result);
        _mockSessionsRepo.Verify(r => r.Count(), Times.Once);
    }

    #endregion

    #region GenerateHash

    [TestMethod]
    [DataRow("123", "AB-12-CD", "c0615f4282c3284b18dc2ee5b52c4602")]
    [DataRow("456", "WX-99-YZ", "fc2c4c948b5601a81aa88a713ab82e27")]
    [DataRow("789", "DA-00-TA", "0ebed8ede8f65676344f76c980d6de52")]
    public void GeneratePaymentHash_ValidInputs_ReturnsCorrectMd5Hash(string sessionId, string licensePlate, string expectedHash)
    {
        // Act
        var hash = _sessionService.GeneratePaymentHash(sessionId, licensePlate);

        // Assert
        Assert.AreEqual(expectedHash, hash);
    }

    [TestMethod]
    public void GenerateTransactionValidationHash_ReturnsValidGuidString()
    {
        // Act
        var hash = _sessionService.GenerateTransactionValidationHash();

        // Assert
        Assert.IsNotNull(hash);
        Assert.AreEqual(32, hash.Length);
        Assert.IsTrue(Guid.TryParse(hash, out _));
    }

    #endregion

    #region StartSession

    [TestMethod]
    [DataRow(99, "AB-12-CD", "token", 10, "user")]
    [DataRow(404, "WX-99YZ", "token2", 15.5, "user2")]
    public async Task StartSession_LotNotFound_ReturnsLotNotFound(
        long lotId, string plate, string token, double amount, string user)
    {
        var dto = new CreateParkingSessionDto { ParkingLotId = lotId, LicensePlate = plate };

        _mockParkingLotService
            .Setup(r => r.GetParkingLotByIdAsync(lotId))
            .ReturnsAsync(ServiceResult<ReadParkingLotDto>.NotFound("Parking lot not found"));

        var result = await _sessionService.StartSession(dto, token, (decimal)amount, user);

        Assert.IsInstanceOfType(result, typeof(StartSessionResult.LotNotFound));
    }

    [TestMethod]
    [DataRow(1, 50, 50, "AB-12-CD", "token", 10, "user")]
    [DataRow(2, 100, 100, "WX-99-YZ", "token2", 20, "user2")]
    public async Task StartSession_LotFull_ReturnsLotFull(
        long lotId, int capacity, int reserved, string plate, string token, double amount, string user)
    {
        var dto = new CreateParkingSessionDto { ParkingLotId = lotId, LicensePlate = plate };
        var lot = new ParkingLotModel { Id = lotId, Capacity = capacity, Reserved = reserved };

        var lotDto = new ReadParkingLotDto
        {
            Id = lot.Id,
            Name = "Test lot",
            Location = "Somewhere",
            Address = "Teststreet 1",
            Reserved = lot.Reserved,
            Capacity = lot.Capacity,
            Tariff = 0m,
            DayTariff = 0m
        };

        _mockParkingLotService
            .Setup(r => r.GetParkingLotByIdAsync(lotId))
            .ReturnsAsync(ServiceResult<ReadParkingLotDto>.Ok(lotDto));

        var result = await _sessionService.StartSession(dto, token, (decimal)amount, user);

        Assert.IsInstanceOfType(result, typeof(StartSessionResult.LotFull));
    }

    [TestMethod]
    [DataRow(1, 50, 10, "AB-12-CD", 5, "token", 10, "user")]
    public async Task StartSession_SessionAlreadyActive_ReturnsAlreadyActive(
        long lotId, int capacity, int reserved, string plate, long activeSessionId,
        string token, double amount, string user)
    {
        var dto = new CreateParkingSessionDto { ParkingLotId = lotId, LicensePlate = plate };
        var lot = new ParkingLotModel { Id = lotId, Capacity = capacity, Reserved = reserved };
        string expectedPlate = plate.ToUpper();
        var active = new ParkingSessionModel { Id = activeSessionId, LicensePlateNumber = expectedPlate };

        var lotDto = new ReadParkingLotDto
        {
            Id = lot.Id,
            Name = "Test lot",
            Location = "Somewhere",
            Address = "Teststreet 1",
            Reserved = lot.Reserved,
            Capacity = lot.Capacity,
            Tariff = 0m,
            DayTariff = 0m
        };

        _mockParkingLotService
            .Setup(r => r.GetParkingLotByIdAsync(lotId))
            .ReturnsAsync(ServiceResult<ReadParkingLotDto>.Ok(lotDto));

        _mockSessionsRepo
            .Setup(r => r.GetActiveSessionByLicensePlate(expectedPlate))
            .ReturnsAsync(active);

        var result = await _sessionService.StartSession(dto, token, (decimal)amount, user);

        Assert.IsInstanceOfType(result, typeof(StartSessionResult.AlreadyActive));
    }

    [TestMethod]
    [DataRow(1, 50, 10, "AB-12-CD", "token", 10, "user")]
    public async Task StartSession_PreAuthFails_ReturnsPreAuthFailed(
        long lotId, int capacity, int reserved, string plate,
        string token, double amount, string user)
    {
        var dto = new CreateParkingSessionDto { ParkingLotId = lotId, LicensePlate = plate };
        var lot = new ParkingLotModel { Id = lotId, Capacity = capacity, Reserved = reserved };
        string p = plate.ToUpper();
        var m = (decimal)amount;

        var lotDto = new ReadParkingLotDto
        {
            Id = lot.Id,
            Name = "Test lot",
            Location = "Somewhere",
            Address = "Teststreet 1",
            Reserved = lot.Reserved,
            Capacity = lot.Capacity,
            Tariff = 0m,
            DayTariff = 0m
        };

        _mockParkingLotService
            .Setup(r => r.GetParkingLotByIdAsync(lotId))
            .ReturnsAsync(ServiceResult<ReadParkingLotDto>.Ok(lotDto));

        _mockSessionsRepo
            .Setup(r => r.GetActiveSessionByLicensePlate(p))
            .ReturnsAsync((ParkingSessionModel?)null);

        _mockHotelPassService
            .Setup(s => s.GetActiveHotelPassByLicensePlateAndLotIdAsync(lotId, p))
            .ReturnsAsync(ServiceResult<ReadHotelPassDto>.NotFound("No active pass"));

        _mockPreAuthService
            .Setup(pra => pra.PreauthorizeAsync(token, m, false))
            .ReturnsAsync(new PreAuthDto { Approved = false });

        var result = await _sessionService.StartSession(dto, token, m, user);

        Assert.IsInstanceOfType(result, typeof(StartSessionResult.PreAuthFailed));
    }

    [TestMethod]
    [DataRow(1, 50, 10, "AB-12-CD", "token", 10, "user")]
    public async Task StartSession_PersistenceFails_ReturnsError(
        long lotId, int capacity, int reserved, string plate,
        string token, double amount, string user)
    {
        var dto = new CreateParkingSessionDto { ParkingLotId = lotId, LicensePlate = plate };
        var lot = new ParkingLotModel { Id = lotId, Capacity = capacity, Reserved = reserved };
        string p = plate.ToUpper();
        var m = (decimal)amount;

        var lotDto = new ReadParkingLotDto
        {
            Id = lot.Id,
            Name = "Test lot",
            Location = "Somewhere",
            Address = "Teststreet 1",
            Reserved = lot.Reserved,
            Capacity = lot.Capacity,
            Tariff = 0m,
            DayTariff = 0m
        };

        _mockParkingLotService
            .Setup(r => r.GetParkingLotByIdAsync(lotId))
            .ReturnsAsync(ServiceResult<ReadParkingLotDto>.Ok(lotDto));

        _mockSessionsRepo
            .Setup(r => r.GetActiveSessionByLicensePlate(p))
            .ReturnsAsync((ParkingSessionModel?)null);

        _mockHotelPassService
            .Setup(s => s.GetActiveHotelPassByLicensePlateAndLotIdAsync(lotId, p))
            .ReturnsAsync(ServiceResult<ReadHotelPassDto>.NotFound("No active pass"));

        _mockPreAuthService
            .Setup(pr => pr.PreauthorizeAsync(token, m, false))
            .ReturnsAsync(new PreAuthDto { Approved = true });

        _mockParkingLotService
            .Setup(s => s.PatchParkingLotByIdAsync(
                lotId,
                It.IsAny<PatchParkingLotDto>()))
            .ReturnsAsync(ServiceResult<ReadParkingLotDto>.Ok(lotDto));

        _mockSessionsRepo
            .Setup(r => r.CreateWithId(It.IsAny<ParkingSessionModel>()))
            .ReturnsAsync((false, 0L));

        var result = await _sessionService.StartSession(dto, token, m, user);

        Assert.IsInstanceOfType(result, typeof(StartSessionResult.Error));
    }

    [TestMethod]
    [DataRow(1, 50, 10, "AB-12-CD", "token", 10, "user", 123L)]
    public async Task StartSession_Success_ReturnsSuccess(
        long lotId, int capacity, int reserved, string plate,
        string token, double amount, string user, long newId)
    {
        var dto = new CreateParkingSessionDto { ParkingLotId = lotId, LicensePlate = plate };
        var lot = new ParkingLotModel { Id = lotId, Capacity = capacity, Reserved = reserved };
        string p = plate.ToUpper();
        var m = (decimal)amount;

        var lotDto = new ReadParkingLotDto
        {
            Id = lot.Id,
            Name = "Test lot",
            Location = "Somewhere",
            Address = "Teststreet 1",
            Reserved = lot.Reserved,
            Capacity = lot.Capacity,
            Tariff = 0m,
            DayTariff = 0m
        };

        _mockParkingLotService
            .Setup(r => r.GetParkingLotByIdAsync(lotId))
            .ReturnsAsync(ServiceResult<ReadParkingLotDto>.Ok(lotDto));

        _mockSessionsRepo
            .Setup(r => r.GetActiveSessionByLicensePlate(p))
            .ReturnsAsync((ParkingSessionModel?)null);

        _mockHotelPassService
            .Setup(s => s.GetActiveHotelPassByLicensePlateAndLotIdAsync(lotId, p))
            .ReturnsAsync(ServiceResult<ReadHotelPassDto>.NotFound("No active pass"));

        _mockPreAuthService
            .Setup(pr => pr.PreauthorizeAsync(token, m, false))
            .ReturnsAsync(new PreAuthDto { Approved = true });

        _mockParkingLotService
            .Setup(s => s.PatchParkingLotByIdAsync(
                lotId,
                It.IsAny<PatchParkingLotDto>()))
            .ReturnsAsync(ServiceResult<ReadParkingLotDto>.Ok(lotDto));

        _mockSessionsRepo
            .Setup(r =>
                r.CreateWithId(It.Is<ParkingSessionModel>(s => s.LicensePlateNumber == p)))
            .ReturnsAsync((true, newId));

        _mockGateService
            .Setup(g => g.OpenGateAsync((int)lotId, p))
            .ReturnsAsync(true);

        var result = await _sessionService.StartSession(dto, token, m, user);

        Assert.IsInstanceOfType(result, typeof(StartSessionResult.Success));
    }

    [TestMethod]
    [DataRow(1, 50, 10, "AB-12-CD", "token", 10, "user", 123L)]
    public async Task StartSession_GateFails_RollsBackOnError(
        long lotId, int capacity, int reserved,
        string plate, string token, double amount, string user, long newId)
    {
        var dto = new CreateParkingSessionDto { ParkingLotId = lotId, LicensePlate = plate };
        var lot = new ParkingLotModel { Id = lotId, Capacity = capacity, Reserved = reserved };
        string p = plate.ToUpper();
        var m = (decimal)amount;

        var lotDto = new ReadParkingLotDto
        {
            Id = lot.Id,
            Name = "Test lot",
            Location = "Somewhere",
            Address = "Teststreet 1",
            Reserved = lot.Reserved,
            Capacity = lot.Capacity,
            Tariff = 0m,
            DayTariff = 0m
        };

        _mockParkingLotService
            .Setup(r => r.GetParkingLotByIdAsync(lotId))
            .ReturnsAsync(ServiceResult<ReadParkingLotDto>.Ok(lotDto));

        _mockSessionsRepo
            .Setup(r => r.GetActiveSessionByLicensePlate(p))
            .ReturnsAsync((ParkingSessionModel?)null);

        _mockHotelPassService
            .Setup(s => s.GetActiveHotelPassByLicensePlateAndLotIdAsync(lotId, p))
            .ReturnsAsync(ServiceResult<ReadHotelPassDto>.NotFound("No active pass"));

        _mockPreAuthService
            .Setup(pr => pr.PreauthorizeAsync(token, m, false))
            .ReturnsAsync(new PreAuthDto { Approved = true });

        _mockParkingLotService
            .Setup(s => s.PatchParkingLotByIdAsync(
                lotId,
                It.IsAny<PatchParkingLotDto>()))
            .ReturnsAsync(ServiceResult<ReadParkingLotDto>.Ok(lotDto));

        _mockSessionsRepo
            .Setup(r =>
                r.CreateWithId(It.Is<ParkingSessionModel>(s => s.LicensePlateNumber == p)))
            .ReturnsAsync((true, newId));

        _mockGateService
            .Setup(g => g.OpenGateAsync((int)lotId, p))
            .ReturnsAsync(false);

        _mockSessionsRepo
            .Setup(r => r.GetById<ParkingSessionModel>(newId))
            .ReturnsAsync(new ParkingSessionModel { Id = newId });

        _mockSessionsRepo
            .Setup(r => r.Delete(It.Is<ParkingSessionModel>(s => s.Id == newId)))
            .ReturnsAsync(true);

        var result = await _sessionService.StartSession(dto, token, m, user);

        Assert.IsInstanceOfType(result, typeof(StartSessionResult.Error));
    }

    [TestMethod]
    public async Task StartSession_HasHotelPass_ReturnsSuccess()
    {
        // Arrange
        long lotId = 1;
        int capacity = 50;
        int reserved = 10;
        string plate = "AB-12-CD";
        string token = "token";  
        decimal amount = 10m;      
        string user = "user";
        long newSessionId = 123L;

        var dto = new CreateParkingSessionDto
        {
            ParkingLotId = lotId,
            LicensePlate = plate
        };

        string p = plate.ToUpper();
        
        var lotDto = new ReadParkingLotDto
        {
            Id = lotId,
            Name = "Test lot",
            Location = "Somewhere",
            Address = "Teststreet 1",
            Reserved = reserved,
            Capacity = capacity,
            Tariff = 0m,
            DayTariff = 0m
        };

        _mockParkingLotService
            .Setup(r => r.GetParkingLotByIdAsync(lotId))
            .ReturnsAsync(ServiceResult<ReadParkingLotDto>.Ok(lotDto));
        
        _mockSessionsRepo
            .Setup(r => r.GetActiveSessionByLicensePlate(p))
            .ReturnsAsync((ParkingSessionModel?)null);

        // Hotel pass exists & is active
        var hotelPassDto = new ReadHotelPassDto
        {
            Id = 42,
            LicensePlate = p,
            ParkingLotId = (int)lotId,
            Start = DateTime.UtcNow.AddDays(-1),
            End = DateTime.UtcNow.AddDays(1),
            ExtraTime = TimeSpan.FromMinutes(30)
        };

        _mockHotelPassService
            .Setup(s => s.GetActiveHotelPassByLicensePlateAndLotIdAsync(lotId, p))
            .ReturnsAsync(ServiceResult<ReadHotelPassDto>.Ok(hotelPassDto));
        
        _mockParkingLotService
            .Setup(s => s.PatchParkingLotByIdAsync(
                lotId,
                It.IsAny<PatchParkingLotDto>()))
            .ReturnsAsync(ServiceResult<ReadParkingLotDto>.Ok(lotDto));
        
        _mockSessionsRepo
            .Setup(r => r.CreateWithId(It.Is<ParkingSessionModel>(s =>
                s.LicensePlateNumber == p &&
                s.ParkingLotId == lotId &&
                s.PaymentStatus == ParkingSessionStatus.HotelPass &&
                s.HotelPassId == hotelPassDto.Id)))
            .ReturnsAsync((true, newSessionId));
        
        _mockGateService
            .Setup(g => g.OpenGateAsync((int)lotId, p))
            .ReturnsAsync(true);
        
        _mockPreAuthService
            .Setup(pr => pr.PreauthorizeAsync(It.IsAny<string>(), It.IsAny<decimal>(), It.IsAny<bool>()))
            .Throws(new Exception("PreAuth should not be called when hotel pass exists"));

        // Act
        var result = await _sessionService.StartSession(dto, token, amount, user);

        // Assert
        Assert.IsInstanceOfType(result, typeof(StartSessionResult.Success));

        var success = (StartSessionResult.Success)result;
        var session = success.Session;

        Assert.AreEqual(ParkingSessionStatus.HotelPass, session.PaymentStatus);
        Assert.AreEqual(hotelPassDto.Id, session.HotelPassId);
        Assert.AreEqual(p, session.LicensePlateNumber);
        Assert.AreEqual(lotId, session.ParkingLotId);
    }

    #endregion
    
    #region StopSession

    [TestMethod]
    [DataRow("AB-12-CD")]
    [DataRow("WX-99-YZ")]
    public async Task StopSession_LicensePlateNotFound_ReturnsLicensePlateNotFound(string plate)
    {
        var dto = new StopParkingSessionDto { LicensePlate = plate };
        _mockSessionsRepo.Setup(r => r.GetActiveSessionByLicensePlate(plate.ToUpper()))
            .ReturnsAsync((ParkingSessionModel?)null);

        var result = await _sessionService.StopSession(dto);

        Assert.IsInstanceOfType(result, typeof(StopSessionResult.LicensePlateNotFound));
    }

    [TestMethod]
    [DataRow("AB-12-CD")]
    [DataRow("WX-99-YZ")]
    public async Task StopSession_AlreadyStopped_ReturnsAlreadyStopped(string plate)
    {
        var dto = new StopParkingSessionDto { LicensePlate = plate };
        var activeSession = new ParkingSessionModel
        {
            Id = 1,
            LicensePlateNumber = plate.ToUpper(),
            Stopped = DateTime.UtcNow
        };
        _mockSessionsRepo.Setup(r => r.GetActiveSessionByLicensePlate(plate.ToUpper()))
            .ReturnsAsync(activeSession);

        var result = await _sessionService.StopSession(dto);

        Assert.IsInstanceOfType(result, typeof(StopSessionResult.AlreadyStopped));
    }

    [TestMethod]
    [DataRow("AB-12-CD")]
    [DataRow("WX-99-YZ")]
    public async Task StopSession_ParkingLotNotFound_ReturnsError(string plate)
    {
        var dto = new StopParkingSessionDto { LicensePlate = plate };
        var activeSession = new ParkingSessionModel
        {
            Id = 1,
            LicensePlateNumber = plate.ToUpper(),
            ParkingLotId = 99,
            Started = DateTime.UtcNow.AddHours(-1)
        };

        _mockSessionsRepo.Setup(r => r.GetActiveSessionByLicensePlate(plate.ToUpper()))
            .ReturnsAsync(activeSession);

        _mockParkingLotService.Setup(p => p.GetParkingLotByIdAsync(99))
            .ReturnsAsync(ServiceResult<ReadParkingLotDto>.NotFound("Parking lot not found"));

        var result = await _sessionService.StopSession(dto);

        Assert.IsInstanceOfType(result, typeof(StopSessionResult.Error));
        StringAssert.Contains(((StopSessionResult.Error)result).Message, "Failed to retrieve parking lot");
    }

    [TestMethod]
    [DataRow("AB-12-CD")]
    [DataRow("WX-99-YZ")]
    public async Task StopSession_PaymentFails_ReturnsPaymentFailed(string plate)
    {
        var dto = new StopParkingSessionDto { LicensePlate = plate, CardToken = "token" };
        var activeSession = new ParkingSessionModel
        {
            Id = 1,
            LicensePlateNumber = plate.ToUpper(),
            ParkingLotId = 1,
            Started = DateTime.UtcNow.AddHours(-1)
        };

        var lotDto = new ReadParkingLotDto
        {
            Id = 1,
            Name = "Test lot",
            Location = "Somewhere",
            Address = "Teststreet 1",
            Reserved = 0,
            Capacity = 0,
            Tariff = 0m,
            DayTariff = 0m
        };

        _mockSessionsRepo.Setup(r => r.GetActiveSessionByLicensePlate(plate.ToUpper()))
            .ReturnsAsync(activeSession);

        _mockParkingLotService.Setup(p => p.GetParkingLotByIdAsync(1))
            .ReturnsAsync(ServiceResult<ReadParkingLotDto>.Ok(lotDto));

        _mockPricingService.Setup(p => p.CalculateParkingCost(It.IsAny<ParkingLotModel>(), It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>()))
            .Returns(new CalculatePriceResult.Success(10m, 1, 0));

        _mockPreAuthService
            .Setup(p => p.PreauthorizeAsync("token", 10m, false))
            .ReturnsAsync(new PreAuthDto { Approved = false, Reason = "Card declined" });

        var result = await _sessionService.StopSession(dto);

        Assert.IsInstanceOfType(result, typeof(StopSessionResult.PaymentFailed));
        StringAssert.Contains(((StopSessionResult.PaymentFailed)result).Reason, "Card declined");
    }

    [TestMethod]
    [DataRow("AB-12-CD")]
    [DataRow("WX-99-YZ")]
    public async Task StopSession_UpdateFails_ReturnsError(string plate)
    {
        var dto = new StopParkingSessionDto { LicensePlate = plate, CardToken = "token" };
        var activeSession = new ParkingSessionModel
        {
            Id = 1,
            LicensePlateNumber = plate.ToUpper(),
            ParkingLotId = 1,
            Started = DateTime.UtcNow.AddHours(-1)
        };

        var lotDto = new ReadParkingLotDto
        {
            Id = 1,
            Name = "Test lot",
            Location = "Somewhere",
            Address = "Teststreet 1",
            Reserved = 0,
            Capacity = 0,
            Tariff = 0m,
            DayTariff = 0m
        };

        _mockSessionsRepo.Setup(r => r.GetActiveSessionByLicensePlate(plate.ToUpper()))
            .ReturnsAsync(activeSession);

        _mockParkingLotService.Setup(p => p.GetParkingLotByIdAsync(1))
            .ReturnsAsync(ServiceResult<ReadParkingLotDto>.Ok(lotDto));

        _mockPricingService.Setup(p => p.CalculateParkingCost(It.IsAny<ParkingLotModel>(), It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>()))
            .Returns(new CalculatePriceResult.Success(10m, 1, 0));

        _mockPreAuthService
            .Setup(p => p.PreauthorizeAsync("token", 10m, It.IsAny<bool>()))
            .ReturnsAsync(new PreAuthDto { Approved = true });

        _mockSessionsRepo.Setup(r => r.Update(activeSession, It.IsAny<UpdateParkingSessionDto>()))
            .ReturnsAsync(false);

        var result = await _sessionService.StopSession(dto);

        Assert.IsInstanceOfType(result, typeof(StopSessionResult.Error));
        StringAssert.Contains(((StopSessionResult.Error)result).Message, "Failed to update session after payment");
    }

    [TestMethod]
    [DataRow("AB-12-CD")]
    [DataRow("WX-99-YZ")]
    public async Task StopSession_Success_ReturnsSuccess(string plate)
    {
        var dto = new StopParkingSessionDto { LicensePlate = plate, CardToken = "token" };
        var lotId = 1;

        var dbSession = new ParkingSessionModel
        {
            Id = 1,
            LicensePlateNumber = plate.ToUpper(),
            ParkingLotId = lotId,
            Started = DateTime.UtcNow.AddHours(-1),
            Stopped = null,
            PaymentStatus = ParkingSessionStatus.PreAuthorized
        };

        var lotDto = new ReadParkingLotDto
        {
            Id = lotId,
            Name = "Test lot",
            Location = "Somewhere",
            Address = "Teststreet 1",
            Reserved = 0,
            Capacity = 0,
            Tariff = 0m,
            DayTariff = 0m
        };

        _mockSessionsRepo.Setup(r => r.GetActiveSessionByLicensePlate(plate.ToUpper()))
            .ReturnsAsync(new ParkingSessionModel
            {
                Id = dbSession.Id,
                LicensePlateNumber = dbSession.LicensePlateNumber,
                ParkingLotId = dbSession.ParkingLotId,
                Started = dbSession.Started,
                PaymentStatus = dbSession.PaymentStatus,
                Stopped = null
            });

        _mockParkingLotService.Setup(p => p.GetParkingLotByIdAsync(lotId))
            .ReturnsAsync(ServiceResult<ReadParkingLotDto>.Ok(lotDto));

        _mockPricingService.Setup(p => p.CalculateParkingCost(It.IsAny<ParkingLotModel>(), It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>()))
            .Returns(new CalculatePriceResult.Success(10m, 1, 0));

        _mockPreAuthService
            .Setup(p => p.PreauthorizeAsync("token", It.IsAny<decimal>(), It.IsAny<bool>()))
            .ReturnsAsync(new PreAuthDto { Approved = true });

        _mockSessionsRepo.Setup(r => r.GetById<ParkingSessionModel>(dbSession.Id))
            .ReturnsAsync(new ParkingSessionModel
            {
                Id = dbSession.Id,
                LicensePlateNumber = dbSession.LicensePlateNumber,
                ParkingLotId = dbSession.ParkingLotId,
                Started = dbSession.Started,
                PaymentStatus = dbSession.PaymentStatus,
                Stopped = null
            });

        _mockSessionsRepo.Setup(r => r.Update(It.IsAny<ParkingSessionModel>(), It.IsAny<UpdateParkingSessionDto>()))
            .ReturnsAsync(true);

        _mockGateService.Setup(g => g.OpenGateAsync((int)lotId, plate.ToUpper()))
            .ReturnsAsync(true);

        var result = await _sessionService.StopSession(dto);

        Assert.IsInstanceOfType(result, typeof(StopSessionResult.Success));
        var success = (StopSessionResult.Success)result;
        Assert.AreEqual(dbSession.Id, success.Session.Id);
        Assert.AreEqual(10m, success.totalAmount);

        _mockSessionsRepo.Verify(r => r.Update(It.IsAny<ParkingSessionModel>(), It.IsAny<UpdateParkingSessionDto>()), Times.Once);
    }

    [TestMethod]
    [DataRow("AB-12-CD")]
    [DataRow("WX-99-YZ")]
    public async Task StopSession_GateFails_RollsBack_ReturnsError(string plate)
    {
        var dto = new StopParkingSessionDto { LicensePlate = plate, CardToken = "token" };
        var lotId = 1;

        var session = new ParkingSessionModel
        {
            Id = 1,
            LicensePlateNumber = plate.ToUpper(),
            ParkingLotId = lotId,
            Started = DateTime.UtcNow.AddHours(-1),
            Stopped = null,
            PaymentStatus = ParkingSessionStatus.PreAuthorized
        };

        var lotDto = new ReadParkingLotDto
        {
            Id = lotId,
            Name = "Test lot",
            Location = "Somewhere",
            Address = "Teststreet 1",
            Reserved = 0,
            Capacity = 0,
            Tariff = 0m,
            DayTariff = 0m
        };

        _mockParkingLotService.Setup(p => p.GetParkingLotByIdAsync(lotId))
            .ReturnsAsync(ServiceResult<ReadParkingLotDto>.Ok(lotDto));

        _mockPricingService.Setup(p => p.CalculateParkingCost(It.IsAny<ParkingLotModel>(), It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>()))
            .Returns(new CalculatePriceResult.Success(10m, 1, 0));

        _mockPreAuthService
            .Setup(p => p.PreauthorizeAsync("token", It.IsAny<decimal>(), It.IsAny<bool>()))
            .ReturnsAsync(new PreAuthDto { Approved = true });

        _mockSessionsRepo.Setup(r => r.GetById<ParkingSessionModel>(session.Id))
            .ReturnsAsync(() => new ParkingSessionModel
            {
                Id = session.Id,
                LicensePlateNumber = session.LicensePlateNumber,
                ParkingLotId = session.ParkingLotId,
                Started = session.Started,
                Stopped = session.Stopped,
                PaymentStatus = session.PaymentStatus
            });

        _mockSessionsRepo.Setup(r => r.GetActiveSessionByLicensePlate(plate.ToUpper()))
            .ReturnsAsync(() => new ParkingSessionModel
            {
                Id = session.Id,
                LicensePlateNumber = session.LicensePlateNumber,
                ParkingLotId = session.ParkingLotId,
                Started = session.Started,
                Stopped = session.Stopped,
                PaymentStatus = session.PaymentStatus
            });

        _mockSessionsRepo.Setup(r => r.Update(It.IsAny<ParkingSessionModel>(), It.IsAny<UpdateParkingSessionDto>()))
            .Callback<ParkingSessionModel, UpdateParkingSessionDto>((updatedModel, updateDto) =>
            {
                session.PaymentStatus = updatedModel.PaymentStatus;
                session.Stopped = updatedModel.Stopped;
            })
            .ReturnsAsync(true);

        _mockGateService.Setup(g => g.OpenGateAsync(It.IsAny<long>(), It.IsAny<string>()))
            .ReturnsAsync(false);

        var result = await _sessionService.StopSession(dto);

        Assert.IsInstanceOfType(result, typeof(StopSessionResult.Error));
        StringAssert.Contains(((StopSessionResult.Error)result).Message, "Payment successful but gate error");
        _mockSessionsRepo.Verify(r => r.Update(It.IsAny<ParkingSessionModel>(), It.IsAny<UpdateParkingSessionDto>()), Times.Exactly(2));
    }
    
    [TestMethod]
    [DataRow("AB-12-CD")]
    [DataRow("WX-99-YZ")]
    public async Task StopSession_HotelPass_WithinFreeWindow_DoesNotCharge_ReturnsSuccess(string plate)
    {
        var dto = new StopParkingSessionDto { LicensePlate = plate, CardToken = "token" };
        var lotId = 1L;
        var sessionId = 1L;
        var p = plate.ToUpper();

        var activeSession = new ParkingSessionModel
        {
            Id = sessionId,
            LicensePlateNumber = p,
            ParkingLotId = lotId,
            Started = DateTime.UtcNow.AddHours(-2),
            Stopped = null,
            PaymentStatus = ParkingSessionStatus.PreAuthorized,
            HotelPassId = 42
        };

        var lotDto = new ReadParkingLotDto
        {
            Id = lotId,
            Name = "Test lot",
            Location = "Somewhere",
            Address = "Teststreet 1",
            Reserved = 0,
            Capacity = 100,
            Tariff = 0m,
            DayTariff = 0m
        };

        var hotelPassDto = new ReadHotelPassDto
        {
            Id = 42,
            LicensePlate = p,
            ParkingLotId = (int)lotId,
            Start = DateTime.UtcNow.AddDays(-1),
            End = DateTime.UtcNow.AddHours(1),
            ExtraTime = TimeSpan.FromMinutes(30)
        };

        _mockSessionsRepo
            .Setup(r => r.GetActiveSessionByLicensePlate(p))
            .ReturnsAsync(activeSession);

        _mockParkingLotService
            .Setup(r => r.GetParkingLotByIdAsync(lotId))
            .ReturnsAsync(ServiceResult<ReadParkingLotDto>.Ok(lotDto));

        _mockHotelPassService
            .Setup(s => s.GetHotelPassByIdAsync(hotelPassDto.Id))
            .ReturnsAsync(ServiceResult<ReadHotelPassDto>.Ok(hotelPassDto));

        _mockSessionsRepo
            .Setup(r => r.GetById<ParkingSessionModel>(sessionId))
            .ReturnsAsync(new ParkingSessionModel
            {
                Id = activeSession.Id,
                LicensePlateNumber = activeSession.LicensePlateNumber,
                ParkingLotId = activeSession.ParkingLotId,
                Started = activeSession.Started,
                PaymentStatus = activeSession.PaymentStatus,
                Stopped = activeSession.Stopped
            });

        _mockSessionsRepo
            .Setup(r => r.Update(It.IsAny<ParkingSessionModel>(), It.IsAny<UpdateParkingSessionDto>()))
            .ReturnsAsync(true);

        _mockGateService
            .Setup(g => g.OpenGateAsync((int)lotId, p))
            .ReturnsAsync(true);

        var result = await _sessionService.StopSession(dto);

        Assert.IsInstanceOfType(result, typeof(StopSessionResult.Success));
        var success = (StopSessionResult.Success)result;
        Assert.AreEqual(0m, success.totalAmount);
        Assert.AreEqual(ParkingSessionStatus.Paid, success.Session.PaymentStatus);
        _mockPreAuthService.Verify(p => p.PreauthorizeAsync(It.IsAny<string>(), It.IsAny<decimal>(), It.IsAny<bool>()), Times.Never);
    }

    [TestMethod]
    [DataRow("AB-12-CD")]
    [DataRow("WX-99-YZ")]
    public async Task StopSession_HotelPass_AfterFreeWindow_ChargesAndReturnsSuccess(string plate)
    {
        var dto = new StopParkingSessionDto { LicensePlate = plate, CardToken = "token" };
        var lotId = 1L;
        var sessionId = 1L;
        var p = plate.ToUpper();

        var activeSession = new ParkingSessionModel
        {
            Id = sessionId,
            LicensePlateNumber = p,
            ParkingLotId = lotId,
            Started = DateTime.UtcNow.AddHours(-5),
            Stopped = null,
            PaymentStatus = ParkingSessionStatus.PreAuthorized,
            HotelPassId = 42
        };

        var lotDto = new ReadParkingLotDto
        {
            Id = lotId,
            Name = "Test lot",
            Location = "Somewhere",
            Address = "Teststreet 1",
            Reserved = 0,
            Capacity = 100,
            Tariff = 0m,
            DayTariff = 0m
        };

        var hotelPassDto = new ReadHotelPassDto
        {
            Id = 42,
            LicensePlate = p,
            ParkingLotId = (int)lotId,
            Start = DateTime.UtcNow.AddDays(-1),
            End = DateTime.UtcNow.AddHours(-2),
            ExtraTime = TimeSpan.FromMinutes(30)
        };

        _mockSessionsRepo
            .Setup(r => r.GetActiveSessionByLicensePlate(p))
            .ReturnsAsync(activeSession);

        _mockParkingLotService
            .Setup(r => r.GetParkingLotByIdAsync(lotId))
            .ReturnsAsync(ServiceResult<ReadParkingLotDto>.Ok(lotDto));

        _mockHotelPassService
            .Setup(s => s.GetHotelPassByIdAsync(hotelPassDto.Id))
            .ReturnsAsync(ServiceResult<ReadHotelPassDto>.Ok(hotelPassDto));

        _mockPricingService
            .Setup(p => p.CalculateParkingCost(It.IsAny<ParkingLotModel>(), It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>()))
            .Returns(new CalculatePriceResult.Success(15m, 1, 0));

        _mockPreAuthService
            .Setup(p => p.PreauthorizeAsync("token", 15m, It.IsAny<bool>()))
            .ReturnsAsync(new PreAuthDto { Approved = true });

        _mockSessionsRepo
            .Setup(r => r.GetById<ParkingSessionModel>(sessionId))
            .ReturnsAsync(new ParkingSessionModel
            {
                Id = activeSession.Id,
                LicensePlateNumber = activeSession.LicensePlateNumber,
                ParkingLotId = activeSession.ParkingLotId,
                Started = activeSession.Started,
                PaymentStatus = activeSession.PaymentStatus,
                Stopped = activeSession.Stopped
            });

        _mockSessionsRepo
            .Setup(r => r.Update(It.IsAny<ParkingSessionModel>(), It.IsAny<UpdateParkingSessionDto>()))
            .ReturnsAsync(true);

        _mockGateService
            .Setup(g => g.OpenGateAsync((int)lotId, p))
            .ReturnsAsync(true);

        var result = await _sessionService.StopSession(dto);

        Assert.IsInstanceOfType(result, typeof(StopSessionResult.Success));
        var success = (StopSessionResult.Success)result;
        Assert.AreEqual(15m, success.totalAmount);
        Assert.AreEqual(ParkingSessionStatus.Paid, success.Session.PaymentStatus);
        _mockPreAuthService.Verify(p => p.PreauthorizeAsync("token", 15m, It.IsAny<bool>()), Times.Once);
    }

    [TestMethod]
    [DataRow("AB-12-CD")]
    [DataRow("WX-99-YZ")]
    public async Task StopSession_HotelPassLookupFails_ReturnsError(string plate)
    {
        var dto = new StopParkingSessionDto { LicensePlate = plate, CardToken = "token" };
        var lotId = 1L;
        var p = plate.ToUpper();

        var activeSession = new ParkingSessionModel
        {
            Id = 1,
            LicensePlateNumber = p,
            ParkingLotId = lotId,
            Started = DateTime.UtcNow.AddHours(-2),
            Stopped = null,
            PaymentStatus = ParkingSessionStatus.PreAuthorized,
            HotelPassId = 42
        };

        var lotDto = new ReadParkingLotDto
        {
            Id = lotId,
            Name = "Test lot",
            Location = "Somewhere",
            Address = "Teststreet 1",
            Reserved = 0,
            Capacity = 100,
            Tariff = 0m,
            DayTariff = 0m
        };

        _mockSessionsRepo
            .Setup(r => r.GetActiveSessionByLicensePlate(p))
            .ReturnsAsync(activeSession);

        _mockParkingLotService
            .Setup(r => r.GetParkingLotByIdAsync(lotId))
            .ReturnsAsync(ServiceResult<ReadParkingLotDto>.Ok(lotDto));

        _mockHotelPassService
            .Setup(s => s.GetHotelPassByIdAsync(42))
            .ReturnsAsync(ServiceResult<ReadHotelPassDto>.NotFound("not found"));

        var result = await _sessionService.StopSession(dto);

        Assert.IsInstanceOfType(result, typeof(StopSessionResult.Error));
        StringAssert.Contains(((StopSessionResult.Error)result).Message, "Failed to retrieve hotel pass from database");
    }

    #endregion
}