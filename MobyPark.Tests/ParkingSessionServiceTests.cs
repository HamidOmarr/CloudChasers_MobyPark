using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using MobyPark.DTOs.ParkingLot.Request;
using MobyPark.DTOs.ParkingSession.Request;
using MobyPark.DTOs.PreAuth.Response;
using MobyPark.Models;
using MobyPark.Models.Repositories.Interfaces;
using MobyPark.Services;
using MobyPark.Services.Interfaces;
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
    private Mock<IParkingLotService> _mockParkingLotService = null!;
    private Mock<IUserPlateService> _mockUserPlateService = null!;
    private Mock<IPricingService> _mockPricingService = null!;
    private ParkingSessionService _sessionService = null!;
    private Mock<IParkingSessionService> _mockSessions = null!;
    private Mock<IGateService> _mockGateService = null!;
    private Mock<IPreAuthService> _mockPreAuthService = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _mockSessionsRepo = new Mock<IParkingSessionRepository>();
        _mockParkingLotService = new Mock<IParkingLotService>();
        _mockUserPlateService = new Mock<IUserPlateService>();
        _mockPricingService = new Mock<IPricingService>();
        _mockGateService = new Mock<IGateService>();
        _mockPreAuthService = new Mock<IPreAuthService>();
        _mockSessions = new Mock<IParkingSessionService>();

        _sessionService = new ParkingSessionService(
            _mockSessionsRepo.Object,
            _mockParkingLotService.Object,
            _mockUserPlateService.Object,
            _mockPricingService.Object,
            _mockGateService.Object,
            _mockPreAuthService.Object
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
        // Arrange
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

        var parkingLot = new ParkingLotModel { Id = lotId, Tariff = (decimal)tariff };

        _mockSessionsRepo.Setup(sessionRepo => sessionRepo.GetById<ParkingSessionModel>(id))
            .ReturnsAsync(existingSession);

        // Service now expects ParkingLotModel? (not GetLotResult)
        _mockParkingLotService.Setup(lotService => lotService.GetParkingLotById((int)lotId))
            .ReturnsAsync(parkingLot);

        _mockPricingService.Setup(ps => ps.CalculateParkingCost(parkingLot, startTime, stopTime))
            .Returns(new CalculatePriceResult.Success(expectedCost, billableHours, billableDays));

        _mockSessionsRepo.Setup(sessionRepo => sessionRepo.Update(existingSession, dto))
            .ReturnsAsync(true);

        // Act
        var result = await _sessionService.UpdateParkingSession(id, dto);

        // Assert
        Assert.IsInstanceOfType(result, typeof(UpdateSessionResult.Success));
        var successResult = (UpdateSessionResult.Success)result;
        var updatedSession = successResult.Session;

        Assert.AreEqual(stopTime, updatedSession.Stopped);
        Assert.AreEqual(expectedCost, updatedSession.Cost);

        _mockPricingService.Verify(ps => ps.CalculateParkingCost(parkingLot, startTime, stopTime), Times.Once);
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
    [DataRow(1, 10, -1)]
    [DataRow(5, 55, -3)]
    public async Task UpdateParkingSession_StopChangedPricingFails_ReturnsError(long id, long lotId, int hoursAgo)
    {
        // Arrange
        var startTime = DateTime.UtcNow.AddHours(hoursAgo);
        var stopTime = DateTime.UtcNow;
        var dto = new UpdateParkingSessionDto { Stopped = stopTime };
        var existingSession = new ParkingSessionModel { Id = id, ParkingLotId = lotId, Started = startTime };
        var parkingLot = new ParkingLotModel { Id = lotId };

        _mockSessionsRepo.Setup(sessionRepo => sessionRepo.GetById<ParkingSessionModel>(id))
            .ReturnsAsync(existingSession);

        _mockParkingLotService.Setup(lotService => lotService.GetParkingLotById((int)lotId))
            .ReturnsAsync(parkingLot);

        _mockPricingService.Setup(ps => ps.CalculateParkingCost(parkingLot, startTime, stopTime))
            .Returns(new CalculatePriceResult.Error("Pricing error"));

        // Act
        var result = await _sessionService.UpdateParkingSession(id, dto);

        // Assert
        Assert.IsInstanceOfType(result, typeof(UpdateSessionResult.Error));
        StringAssert.Contains(((UpdateSessionResult.Error)result).Message, "Pricing error");
    }

    [TestMethod]
    [DataRow(1, 10, -2)]
    public async Task UpdateParkingSession_LotNotFoundDuringCostRecalc_ReturnsError(long id, long lotId, int hoursAgo)
    {
        // Arrange
        var stopTime = DateTime.UtcNow;
        var startTime = stopTime.AddHours(hoursAgo);
        var dto = new UpdateParkingSessionDto { Stopped = stopTime };
        var existingSession = new ParkingSessionModel { Id = id, ParkingLotId = lotId, Started = startTime };

        _mockSessionsRepo.Setup(sessionRepo => sessionRepo.GetById<ParkingSessionModel>(id))
            .ReturnsAsync(existingSession);

        // Service now returns null for missing lot
        _mockParkingLotService.Setup(lotService => lotService.GetParkingLotById((int)lotId))
            .ReturnsAsync((ParkingLotModel?)null);

        // Act
        var result = await _sessionService.UpdateParkingSession(id, dto);

        // Assert
        Assert.IsInstanceOfType(result, typeof(UpdateSessionResult.Error));
        StringAssert.Contains(((UpdateSessionResult.Error)result).Message, "Failed to retrieve parking lot");
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
        // Arrange
        var dto = new CreateParkingSessionDto { ParkingLotId = lotId, LicensePlate = plate };

        // Service expects null for missing lot
        _mockParkingLotService.Setup(lotService => lotService.GetParkingLotById((int)lotId))
            .ReturnsAsync((ParkingLotModel?)null);

        // Act
        var result = await _sessionService.StartSession(dto, token, (decimal)amount, user);

        // Assert
        Assert.IsInstanceOfType(result, typeof(StartSessionResult.LotNotFound));
    }

    [TestMethod]
    [DataRow(1, 50, 50, "AB-12-CD", "token", 10, "user")]
    [DataRow(2, 100, 100, "WX-99-YZ", "token2", 20, "user2")]
    public async Task StartSession_LotFull_ReturnsLotFull(
        long lotId, int capacity, int reserved, string plate, string token, double amount, string user)
    {
        // Arrange
        var lot = new ParkingLotModel { Id = lotId, Capacity = capacity, Reserved = reserved };
        var dto = new CreateParkingSessionDto { ParkingLotId = lotId, LicensePlate = plate };

        _mockParkingLotService.Setup(lotService => lotService.GetParkingLotById((int)lotId))
            .ReturnsAsync(lot);

        // Act
        var result = await _sessionService.StartSession(dto, token, (decimal)amount, user);

        // Assert
        Assert.IsInstanceOfType(result, typeof(StartSessionResult.LotFull));
    }

    [TestMethod]
    [DataRow(1, 50, 10, "AB-12-CD", 5, "token", 10, "user")]
    [DataRow(2, 100, 20, "WX-99-YZ", 10, "token2", 20, "user2")]
    public async Task StartSession_SessionAlreadyActive_ReturnsAlreadyActive(
        long lotId, int capacity, int reserved, string plate, long activeSessionId, string token, double amount, string user)
    {
        // Arrange
        var lot = new ParkingLotModel { Id = lotId, Capacity = capacity, Reserved = reserved };
        var dto = new CreateParkingSessionDto { ParkingLotId = lotId, LicensePlate = plate };
        string expectedPlate = plate.ToUpper();
        var activeSession = new ParkingSessionModel { Id = activeSessionId, LicensePlateNumber = expectedPlate };

        _mockParkingLotService.Setup(lotService => lotService.GetParkingLotById((int)lotId))
            .ReturnsAsync(lot);

        _mockSessionsRepo.Setup(sessionRepo => sessionRepo.GetActiveSessionByLicensePlate(expectedPlate))
            .ReturnsAsync(activeSession);

        // Act
        var result = await _sessionService.StartSession(dto, token, (decimal)amount, user);

        // Assert
        Assert.IsInstanceOfType(result, typeof(StartSessionResult.AlreadyActive));
    }

    [TestMethod]
    [DataRow(1, 50, 10, "AB-12-CD", "token", 10, "user")]
    [DataRow(2, 100, 20, "WX-99-YZ", "token2", 20, "user2")]
    public async Task StartSession_PreAuthFails_ReturnsPreAuthFailed(
        long lotId, int capacity, int reserved, string plate, string token, double amount, string user)
    {
        // Arrange
        var lot = new ParkingLotModel { Id = lotId, Capacity = capacity, Reserved = reserved };
        var dto = new CreateParkingSessionDto { ParkingLotId = lotId, LicensePlate = plate };
        string expectedPlate = plate.ToUpper();
        var decAmount = (decimal)amount;

        _mockParkingLotService.Setup(lotService => lotService.GetParkingLotById((int)lotId))
            .ReturnsAsync(lot);

        _mockSessionsRepo.Setup(sessionRepo => sessionRepo.GetActiveSessionByLicensePlate(expectedPlate))
            .ReturnsAsync((ParkingSessionModel?)null);

        _mockPreAuthService.Setup(preAuth => preAuth.PreauthorizeAsync(token, decAmount, false))
            .ReturnsAsync(new PreAuthDto { Approved = false, Reason = "Card declined" });

        // Act
        var result = await _sessionService.StartSession(dto, token, decAmount, user);

        // Assert
        Assert.IsInstanceOfType(result, typeof(StartSessionResult.PreAuthFailed));
        _mockSessionsRepo.Verify(sessionRepo => sessionRepo.CreateWithId(It.IsAny<ParkingSessionModel>()), Times.Never);
    }

    [TestMethod]
    [DataRow(1, 50, 10, "AB-12-CD", "token", 10, "user")]
    [DataRow(2, 100, 20, "WX-99-YZ", "token2", 20, "user2")]
    public async Task StartSession_PersistenceFails_ReturnsError(
        long lotId, int capacity, int reserved, string plate, string token, double amount, string user)
    {
        // Arrange
        var lot = new ParkingLotModel { Id = lotId, Capacity = capacity, Reserved = reserved };
        var dto = new CreateParkingSessionDto { ParkingLotId = lotId, LicensePlate = plate };
        string expectedPlate = plate.ToUpper();
        var decAmount = (decimal)amount;
        int newReserved = reserved + 1;

        _mockParkingLotService.Setup(lotService => lotService.GetParkingLotById((int)lotId))
            .ReturnsAsync(lot);

        _mockSessionsRepo.Setup(sessionRepo => sessionRepo.GetActiveSessionByLicensePlate(expectedPlate))
            .ReturnsAsync((ParkingSessionModel?)null);

        _mockPreAuthService.Setup(preAuthService => preAuthService.PreauthorizeAsync(token, decAmount, false))
            .ReturnsAsync(new PreAuthDto { Approved = true });

        // The service calls UpdateParkingLotByIDAsync(lot, (int)lot.Id) and then sets lot.Reserved = newReserved
        _mockParkingLotService.Setup(lotService => lotService.UpdateParkingLotByIDAsync(It.Is<ParkingLotModel>(pl => pl.Id == lotId), (int)lotId))
            .ReturnsAsync(new RegisterResult.Success(lot));

        _mockSessionsRepo.Setup(sessionRepo => sessionRepo.CreateWithId(
                It.Is<ParkingSessionModel>(sessionModel => sessionModel.LicensePlateNumber == expectedPlate)))
            .ReturnsAsync((false, 0L));

        // Rollback path: service again calls UpdateParkingLotByIDAsync(lot, id) without asserting reserved value
        _mockParkingLotService.Setup(lotService => lotService.UpdateParkingLotByIDAsync(It.Is<ParkingLotModel>(pl => pl.Id == lotId), (int)lotId))
            .ReturnsAsync(new RegisterResult.Success(lot));

        // Act
        var result = await _sessionService.StartSession(dto, token, decAmount, user);

        // Assert
        Assert.IsInstanceOfType(result, typeof(StartSessionResult.Error));
        StringAssert.Contains(((StartSessionResult.Error)result).Message, "Failed to persist");
        _mockParkingLotService.Verify(lotService => lotService.UpdateParkingLotByIDAsync(It.IsAny<ParkingLotModel>(), (int)lotId), Times.AtLeastOnce);
    }

    [TestMethod]
    [DataRow(1, 50, 10, "AB-12-CD", "token", 10, "user", 123L)]
    [DataRow(2, 100, 20, "WX-99-YZ", "token2", 20, "user2", 456L)]
    public async Task StartSession_Success_ReturnsSuccess(
        long lotId, int capacity, int reserved, string plate, string token, double amount, string user, long newSessionId)
    {
        // Arrange
        var lot = new ParkingLotModel { Id = lotId, Capacity = capacity, Reserved = reserved };
        var dto = new CreateParkingSessionDto { ParkingLotId = lotId, LicensePlate = plate };
        string expectedPlate = plate.ToUpper();
        var decAmount = (decimal)amount;
        int newReserved = reserved + 1;

        _mockParkingLotService.Setup(lotService => lotService.GetParkingLotById((int)lotId))
            .ReturnsAsync(lot);

        _mockSessionsRepo.Setup(sessionRepo => sessionRepo.GetActiveSessionByLicensePlate(expectedPlate))
            .ReturnsAsync((ParkingSessionModel?)null);

        _mockPreAuthService.Setup(preAuthService => preAuthService.PreauthorizeAsync(token, decAmount, false))
            .ReturnsAsync(new PreAuthDto { Approved = true });

        _mockParkingLotService.Setup(lotService => lotService.UpdateParkingLotByIDAsync(It.Is<ParkingLotModel>(pl => pl.Id == lotId), (int)lotId))
            .ReturnsAsync(new RegisterResult.Success(lot));

        _mockSessionsRepo.Setup(sessionRepo => sessionRepo.CreateWithId(
                It.Is<ParkingSessionModel>(sessionModel => sessionModel.LicensePlateNumber == expectedPlate)))
            .ReturnsAsync((true, newSessionId));

        _mockGateService.Setup(gateService => gateService.OpenGateAsync((int)lotId, expectedPlate))
            .ReturnsAsync(true);

        // Act
        var result = await _sessionService.StartSession(dto, token, decAmount, user);

        // Assert
        Assert.IsInstanceOfType(result, typeof(StartSessionResult.Success));
        var successResult = (StartSessionResult.Success)result;
        Assert.AreEqual(newSessionId, successResult.Session.Id);
        Assert.AreEqual(newReserved, lot.Reserved);

        _mockGateService.Verify(gateService => gateService.OpenGateAsync((int)lotId, expectedPlate), Times.Once);
        _mockSessionsRepo.Verify(sessionRepo => sessionRepo.Delete(It.IsAny<ParkingSessionModel>()), Times.Never);
    }

    [TestMethod]
    [DataRow(1, 50, 10, "AB-12-CD", "token", 10, "user", 123L)]
    [DataRow(2, 100, 20, "WX-99-YZ", "token2", 20, "user2", 456L)]
    public async Task StartSession_GateFails_RollsBackOnError(
        long lotId, int capacity, int reserved, string plate, string token, double amount, string user, long newSessionId)
    {
        // Arrange
        var lot = new ParkingLotModel { Id = lotId, Capacity = capacity, Reserved = reserved };
        var dto = new CreateParkingSessionDto { ParkingLotId = lotId, LicensePlate = plate };
        string expectedPlate = plate.ToUpper();
        var decAmount = (decimal)amount;

        _mockParkingLotService.Setup(lotService => lotService.GetParkingLotById((int)lotId))
            .ReturnsAsync(lot);

        _mockSessionsRepo.Setup(sessionRepo => sessionRepo.GetActiveSessionByLicensePlate(expectedPlate))
            .ReturnsAsync((ParkingSessionModel?)null);

        _mockPreAuthService.Setup(p => p.PreauthorizeAsync(token, decAmount, false))
            .ReturnsAsync(new PreAuthDto { Approved = true });

        _mockParkingLotService.Setup(lotService => lotService.UpdateParkingLotByIDAsync(It.Is<ParkingLotModel>(pl => pl.Id == lotId), (int)lotId))
            .ReturnsAsync(new RegisterResult.Success(lot));

        _mockSessionsRepo.Setup(sessionRepo => sessionRepo.CreateWithId(
                It.Is<ParkingSessionModel>(sessionModel => sessionModel.LicensePlateNumber == expectedPlate)))
            .ReturnsAsync((true, newSessionId));

        _mockGateService.Setup(gateService => gateService.OpenGateAsync((int)lotId, expectedPlate))
            .ReturnsAsync(false);

        var sessionToDelete = new ParkingSessionModel { Id = newSessionId };
        _mockSessionsRepo.Setup(sessionRepo => sessionRepo.GetById<ParkingSessionModel>(newSessionId))
            .ReturnsAsync(sessionToDelete);

        _mockSessionsRepo.Setup(sessionRepo => sessionRepo.Delete(sessionToDelete))
            .ReturnsAsync(true);

        // Rollback update (no assertion on reserved value, just ensure it was called)
        _mockParkingLotService.Setup(lotService => lotService.UpdateParkingLotByIDAsync(It.Is<ParkingLotModel>(pl => pl.Id == lotId), (int)lotId))
            .ReturnsAsync(new RegisterResult.Success(lot));

        // Act
        var result = await _sessionService.StartSession(dto, token, decAmount, user);

        // Assert
        Assert.IsInstanceOfType(result, typeof(StartSessionResult.Error));
        StringAssert.Contains(((StartSessionResult.Error)result).Message, "Failed to open gate");
        _mockSessionsRepo.Verify(sessionRepo => sessionRepo.Delete(It.Is<ParkingSessionModel>(ps => ps.Id == newSessionId)), Times.Once);
        _mockParkingLotService.Verify(lotService => lotService.UpdateParkingLotByIDAsync(It.IsAny<ParkingLotModel>(), (int)lotId), Times.AtLeastOnce);
    }

    #endregion

    #region StopSession

    [TestMethod]
    [DataRow("AB-12-CD")]
    [DataRow("WX-99-YZ")]
    public async Task StopSession_LicensePlateNotFound_ReturnsLicensePlateNotFound(string plate)
    {
        // Arrange
        var dto = new StopParkingSessionDto { LicensePlate = plate };
        _mockSessionsRepo.Setup(r => r.GetActiveSessionByLicensePlate(plate.ToUpper()))
            .ReturnsAsync((ParkingSessionModel?)null);

        // Act
        var result = await _sessionService.StopSession(dto);

        // Assert
        Assert.IsInstanceOfType(result, typeof(StopSessionResult.LicensePlateNotFound));
    }

    [TestMethod]
    [DataRow("AB-12-CD")]
    [DataRow("WX-99-YZ")]
    public async Task StopSession_AlreadyStopped_ReturnsAlreadyStopped(string plate)
    {
        // Arrange
        var dto = new StopParkingSessionDto { LicensePlate = plate };
        var activeSession = new ParkingSessionModel
        {
            Id = 1,
            LicensePlateNumber = plate.ToUpper(),
            Stopped = DateTime.UtcNow
        };
        _mockSessionsRepo.Setup(r => r.GetActiveSessionByLicensePlate(plate.ToUpper()))
            .ReturnsAsync(activeSession);

        // Act
        var result = await _sessionService.StopSession(dto);

        // Assert
        Assert.IsInstanceOfType(result, typeof(StopSessionResult.AlreadyStopped));
    }

    [TestMethod]
    [DataRow("AB-12-CD")]
    [DataRow("WX-99-YZ")]
    public async Task StopSession_ParkingLotNotFound_ReturnsError(string plate)
    {
        // Arrange
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

        _mockParkingLotService.Setup(p => p.GetParkingLotById(99))
            .ReturnsAsync((ParkingLotModel?)null);

        // Act
        var result = await _sessionService.StopSession(dto);

        // Assert
        Assert.IsInstanceOfType(result, typeof(StopSessionResult.Error));
        StringAssert.Contains(((StopSessionResult.Error)result).Message, "Failed to retrieve parking lot");
    }

    [TestMethod]
    [DataRow("AB-12-CD")]
    [DataRow("WX-99-YZ")]
    public async Task StopSession_PaymentFails_ReturnsPaymentFailed(string plate)
    {
        // Arrange
        var dto = new StopParkingSessionDto { LicensePlate = plate, CardToken = "token" };
        var activeSession = new ParkingSessionModel
        {
            Id = 1,
            LicensePlateNumber = plate.ToUpper(),
            ParkingLotId = 1,
            Started = DateTime.UtcNow.AddHours(-1)
        };
        var lot = new ParkingLotModel { Id = 1 };

        _mockSessionsRepo.Setup(r => r.GetActiveSessionByLicensePlate(plate.ToUpper()))
            .ReturnsAsync(activeSession);

        _mockParkingLotService.Setup(p => p.GetParkingLotById(1))
            .ReturnsAsync(lot);

        _mockPricingService.Setup(p => p.CalculateParkingCost(lot, It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>()))
            .Returns(new CalculatePriceResult.Success(10m, 1, 0));

        _mockPreAuthService.Setup(p => p.PreauthorizeAsync("token", 10m, It.IsAny<bool>()))
            .ReturnsAsync(new PreAuthDto { Approved = false, Reason = "Card declined" });

        // Act
        var result = await _sessionService.StopSession(dto);

        // Assert
        Assert.IsInstanceOfType(result, typeof(StopSessionResult.PaymentFailed));
        StringAssert.Contains(((StopSessionResult.PaymentFailed)result).Reason, "Card declined");
    }

    [TestMethod]
    [DataRow("AB-12-CD")]
    [DataRow("WX-99-YZ")]
    public async Task StopSession_UpdateFails_ReturnsError(string plate)
    {
        // Arrange
        var dto = new StopParkingSessionDto { LicensePlate = plate, CardToken = "token" };
        var activeSession = new ParkingSessionModel
        {
            Id = 1,
            LicensePlateNumber = plate.ToUpper(),
            ParkingLotId = 1,
            Started = DateTime.UtcNow.AddHours(-1)
        };
        var lot = new ParkingLotModel { Id = 1 };

        _mockSessionsRepo.Setup(r => r.GetActiveSessionByLicensePlate(plate.ToUpper()))
            .ReturnsAsync(activeSession);

        _mockParkingLotService.Setup(p => p.GetParkingLotById(1))
            .ReturnsAsync(lot);

        _mockPricingService.Setup(p => p.CalculateParkingCost(lot, It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>()))
            .Returns(new CalculatePriceResult.Success(10m, 1, 0));

        _mockPreAuthService.Setup(p => p.PreauthorizeAsync("token", 10m, It.IsAny<bool>()))
            .ReturnsAsync(new PreAuthDto { Approved = true });

        _mockSessionsRepo.Setup(r => r.Update(activeSession, It.IsAny<UpdateParkingSessionDto>()))
        .ReturnsAsync(false);
        // Act
        var result = await _sessionService.StopSession(dto);

        // Assert
        Assert.IsInstanceOfType(result, typeof(StopSessionResult.Error));
        StringAssert.Contains(((StopSessionResult.Error)result).Message, "Failed to update session after payment");
    }

    [TestMethod]
    [DataRow("AB-12-CD")]
    [DataRow("WX-99-YZ")]
    public async Task StopSession_Success_ReturnsSuccess(string plate)
    {
        // Arrange
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

        var lot = new ParkingLotModel { Id = lotId };

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

        _mockParkingLotService.Setup(p => p.GetParkingLotById(lotId))
            .ReturnsAsync(lot);

        _mockPricingService.Setup(p => p.CalculateParkingCost(lot, It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>()))
            .Returns(new CalculatePriceResult.Success(10m, 1, 0));

        _mockPreAuthService.Setup(p => p.PreauthorizeAsync("token", It.IsAny<decimal>(), It.IsAny<bool>()))
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

        _mockGateService.Setup(g => g.OpenGateAsync(lot.Id, plate.ToUpper()))
            .ReturnsAsync(true);

        // Act
        var result = await _sessionService.StopSession(dto);

        // Assert
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
        // Arrange
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

        var lot = new ParkingLotModel { Id = lotId };

        _mockParkingLotService.Setup(p => p.GetParkingLotById(lotId)).ReturnsAsync(lot);
        _mockPricingService.Setup(p => p.CalculateParkingCost(It.IsAny<ParkingLotModel>(), It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>()))
            .Returns(new CalculatePriceResult.Success(10m, 1, 0));
        _mockPreAuthService.Setup(p => p.PreauthorizeAsync("token", It.IsAny<decimal>(), It.IsAny<bool>()))
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

        // Act
        var result = await _sessionService.StopSession(dto);

        // Assert
        Assert.IsInstanceOfType(result, typeof(StopSessionResult.Error));
        StringAssert.Contains(((StopSessionResult.Error)result).Message, "Payment successful but gate error");
        _mockSessionsRepo.Verify(r => r.Update(It.IsAny<ParkingSessionModel>(), It.IsAny<UpdateParkingSessionDto>()), Times.Exactly(2));
    }

    #endregion

}
