using MobyPark.DTOs.ParkingLot.Request;
using MobyPark.DTOs.ParkingSession.Request;
using MobyPark.Models;
using MobyPark.Models.Repositories.Interfaces;
using MobyPark.Services;
using MobyPark.Services.Interfaces;
using MobyPark.Services.Results.ParkingLot;
using MobyPark.Services.Results.ParkingSession;
using MobyPark.Services.Results.Price;
using MobyPark.Services.Results.UserPlate;
using Moq;

namespace MobyPark.Tests;

[TestClass]
public sealed class ParkingSessionServiceTests
{
    #region Setup

    private Mock<IParkingSessionRepository> _mockSessionsRepo = null!;
    private Mock<IParkingLotService> _mockParkingLotService = null!;
    private Mock<IUserPlateService> _mockUserPlateService = null!;
    private Mock<IUserService> _mockUserService = null!;
    private Mock<IPricingService> _mockPricingService = null!;
    private ParkingSessionService _sessionService = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _mockSessionsRepo = new Mock<IParkingSessionRepository>();
        _mockParkingLotService = new Mock<IParkingLotService>();
        _mockUserPlateService = new Mock<IUserPlateService>();
        _mockUserService = new Mock<IUserService>();
        _mockPricingService = new Mock<IPricingService>();

        _sessionService = new ParkingSessionService(
            _mockSessionsRepo.Object,
            _mockParkingLotService.Object,
            _mockUserPlateService.Object,
            _mockUserService.Object,
            _mockPricingService.Object
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

        _mockSessionsRepo.Setup(sessionRepo => sessionRepo.GetActiveSessionByLicensePlate(expectedPlate)).ReturnsAsync((ParkingSessionModel?)null);

        _mockSessionsRepo.Setup(sessionRepo => sessionRepo.CreateWithId(It.Is<ParkingSessionModel>(
            sessionModel => sessionModel.LicensePlateNumber == expectedPlate &&
                            sessionModel.ParkingLotId == dto.ParkingLotId))).ReturnsAsync((true, expectedId));

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

        _mockSessionsRepo.Setup(sessionRepo => sessionRepo.GetActiveSessionByLicensePlate(expectedPlate)).ReturnsAsync(existingSession);

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

        _mockSessionsRepo.Setup(sessionRepo => sessionRepo.GetActiveSessionByLicensePlate(expectedPlate)).ReturnsAsync((ParkingSessionModel?)null);

        _mockSessionsRepo.Setup(sessionRepo => sessionRepo.CreateWithId(It.IsAny<ParkingSessionModel>())).ReturnsAsync((false, 0L));

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

        _mockSessionsRepo.Setup(sessionRepo => sessionRepo.GetActiveSessionByLicensePlate(expectedPlate)).ReturnsAsync((ParkingSessionModel?)null);

        _mockSessionsRepo.Setup(sessionRepo => sessionRepo.CreateWithId(It.IsAny<ParkingSessionModel>())).ThrowsAsync(new InvalidOperationException("DB Boom!"));

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
        _mockSessionsRepo.Setup(sessionRepo => sessionRepo.GetById<ParkingSessionModel>(id)).ReturnsAsync(expectedSession);

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
        _mockSessionsRepo.Setup(sessionRepo => sessionRepo.GetById<ParkingSessionModel>(id)).ReturnsAsync((ParkingSessionModel?)null);

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

        _mockSessionsRepo.Setup(sessionRepo => sessionRepo.GetById<ParkingSessionModel>(id)).ReturnsAsync(existingSession);
        _mockParkingLotService.Setup(lotService => lotService.GetParkingLotById(lotId)).ReturnsAsync(new GetLotResult.Success(parkingLot));
        _mockPricingService.Setup(pricingService => pricingService.CalculateParkingCost(parkingLot, startTime, stopTime)).Returns(new CalculatePriceResult.Success(expectedCost, billableHours, billableDays));

        _mockSessionsRepo.Setup(sessionRepo => sessionRepo.Update(It.IsAny<ParkingSessionModel>(), dto)).ReturnsAsync(true);

        // Act
        var result = await _sessionService.UpdateParkingSession(id, dto);

        // Assert
        Assert.IsInstanceOfType(result, typeof(UpdateSessionResult.Success));
        var successResult = (UpdateSessionResult.Success)result;
        var updatedSession = successResult.Session;

        Assert.AreEqual(stopTime, updatedSession.Stopped);
        Assert.AreEqual(expectedCost, updatedSession.Cost);
        Assert.AreEqual(expectedDuration, updatedSession.DurationMinutes);

        _mockPricingService.Verify(pricingService => pricingService.CalculateParkingCost(parkingLot, startTime, stopTime), Times.Once);
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

        _mockSessionsRepo.Setup(sessionRepo => sessionRepo.GetById<ParkingSessionModel>(id)).ReturnsAsync(existingSession);
        _mockSessionsRepo.Setup(sessionRepo => sessionRepo.Update(existingSession, dto)).ReturnsAsync(true);

        // Act
        var result = await _sessionService.UpdateParkingSession(id, dto);

        // Assert
        Assert.IsInstanceOfType(result, typeof(UpdateSessionResult.Success));
        Assert.AreEqual(newStatus, ((UpdateSessionResult.Success)result).Session.PaymentStatus);

        _mockPricingService.Verify(pricingService => pricingService.CalculateParkingCost(It.IsAny<ParkingLotModel>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()), Times.Never);
        _mockSessionsRepo.Verify(sessionRepo => sessionRepo.Update(existingSession, dto), Times.Once);
    }

    [TestMethod]
    [DataRow(99)]
    [DataRow(404)]
    public async Task UpdateParkingSession_SessionNotFound_ReturnsNotFound(long id)
    {
        // Arrange
        var dto = new UpdateParkingSessionDto { Stopped = DateTime.UtcNow };
        _mockSessionsRepo.Setup(sessionRepo => sessionRepo.GetById<ParkingSessionModel>(id)).ReturnsAsync((ParkingSessionModel?)null);

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

        _mockSessionsRepo.Setup(sessionRepo => sessionRepo.GetById<ParkingSessionModel>(id)).ReturnsAsync(existingSession);

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

        _mockSessionsRepo.Setup(sessionRepo => sessionRepo.GetById<ParkingSessionModel>(id)).ReturnsAsync(existingSession);

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

        _mockSessionsRepo.Setup(sessionRepo => sessionRepo.GetById<ParkingSessionModel>(id)).ReturnsAsync(existingSession);
        _mockParkingLotService.Setup(lotService => lotService.GetParkingLotById(lotId)).ReturnsAsync(new GetLotResult.Success(parkingLot));
        _mockPricingService.Setup(pricingService => pricingService.CalculateParkingCost(parkingLot, startTime, stopTime))
            .Returns(new CalculatePriceResult.Error("Pricing error"));

        // Act
        var result = await _sessionService.UpdateParkingSession(id, dto);

        // Assert
        Assert.IsInstanceOfType(result, typeof(UpdateSessionResult.Error));
        StringAssert.Contains(((UpdateSessionResult.Error)result).Message, "Failed to recalculate cost");
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

        _mockSessionsRepo.Setup(sessionRepo => sessionRepo.GetById<ParkingSessionModel>(id)).ReturnsAsync(existingSession);
        _mockParkingLotService.Setup(lotService => lotService.GetParkingLotById(lotId)).ReturnsAsync(new GetLotResult.NotFound());

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

        _mockSessionsRepo.Setup(sessionRepo => sessionRepo.GetById<ParkingSessionModel>(id)).ReturnsAsync(existingSession);
        _mockSessionsRepo.Setup(sessionRepo => sessionRepo.Update(existingSession, dto)).ReturnsAsync(false);

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

        _mockSessionsRepo.Setup(sessionRepo => sessionRepo.GetById<ParkingSessionModel>(id)).ReturnsAsync(existingSession);

        _mockSessionsRepo.Setup(sessionRepo => sessionRepo.Update(existingSession, dto)).ThrowsAsync(new InvalidOperationException("DB Boom!"));

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
        _mockSessionsRepo.Setup(sessionRepo => sessionRepo.GetById<ParkingSessionModel>(id)).ReturnsAsync(session);
        _mockSessionsRepo.Setup(sessionRepo => sessionRepo.Delete(session)).ReturnsAsync(true);

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
        _mockSessionsRepo.Setup(sessionRepo => sessionRepo.GetById<ParkingSessionModel>(id)).ReturnsAsync((ParkingSessionModel?)null);

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
        _mockSessionsRepo.Setup(sessionRepo => sessionRepo.GetById<ParkingSessionModel>(id)).ReturnsAsync(session);
        _mockSessionsRepo.Setup(sessionRepo => sessionRepo.Delete(session)).ReturnsAsync(false); // Simulate DB failure

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

        _mockSessionsRepo.Setup(sessionRepo => sessionRepo.GetById<ParkingSessionModel>(id)).ReturnsAsync(session);
        _mockSessionsRepo.Setup(sessionRepo => sessionRepo.Delete(session)).ThrowsAsync(new InvalidOperationException("DB Boom!"));

        // Act
        var result = await _sessionService.DeleteParkingSession(id);

        // Assert
        Assert.IsInstanceOfType(result, typeof(DeleteSessionResult.Error));
        StringAssert.Contains(((DeleteSessionResult.Error)result).Message, "DB Boom!");
    }

    #endregion

    #region GetByVariousCriteria

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
        _mockSessionsRepo.Setup(sessionRepo => sessionRepo.GetByParkingLotId(lotId)).ReturnsAsync(sessions);

        // Act
        var result = await _sessionService.GetParkingSessionsByParkingLotId(lotId);

        // Assert
        Assert.IsInstanceOfType(result, typeof(GetSessionListResult.Success));
        Assert.AreEqual(totalSessions, ((GetSessionListResult.Success)result).Sessions.Count);
    }

    [TestMethod]
    [DataRow(null)]
    public async Task GetParkingSessionsByParkingLotId_InvalidLotId_ReturnsNotFound(long lotId)
    {
        // Arrange
        _mockSessionsRepo.Setup(sessionRepo => sessionRepo.GetByParkingLotId(lotId)).ReturnsAsync(new List<ParkingSessionModel>());

        // Act
        var result = await _sessionService.GetParkingSessionsByParkingLotId(lotId);

        // Assert
        Assert.IsInstanceOfType(result, typeof(GetSessionListResult.NotFound));
    }

    [TestMethod]
    [DataRow(99)]
    [DataRow(1000)]
    public async Task GetParkingSessionByParkingLotId_NoSessionsFound_ReturnsNotFound(long lotId)
    {
        // Arrange
        _mockSessionsRepo.Setup(sessionRepo => sessionRepo.GetByParkingLotId(lotId)).ReturnsAsync(new List<ParkingSessionModel>());

        // Act
        var result = await _sessionService.GetParkingSessionsByParkingLotId(lotId);

        // Assert
        Assert.IsInstanceOfType(result, typeof(GetSessionListResult.NotFound));
    }

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
        _mockSessionsRepo.Setup(sessionRepo => sessionRepo.GetByLicensePlateNumber(plate)).ReturnsAsync(sessions);

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
        _mockSessionsRepo.Setup(sessionRepo => sessionRepo.GetByLicensePlateNumber(plate)).ReturnsAsync(new List<ParkingSessionModel>());

        // Act
        var result = await _sessionService.GetParkingSessionsByLicensePlate(plate);

        // Assert
        Assert.IsInstanceOfType(result, typeof(GetSessionListResult.NotFound));
    }

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
        _mockSessionsRepo.Setup(sessionRepo => sessionRepo.GetByPaymentStatus(parsedStatus)).ReturnsAsync(sessions);

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
            .ReturnsAsync(new List<ParkingSessionModel>());

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
        _mockSessionsRepo.Setup(sessionRepo => sessionRepo.GetAll()).ReturnsAsync(sessions);

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
        _mockSessionsRepo.Setup(sessionRepo => sessionRepo.GetAll()).ReturnsAsync(new List<ParkingSessionModel>());

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
        _mockSessionsRepo.Setup(sessionRepo => sessionRepo.GetActiveSessions()).ReturnsAsync(sessions);

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
        _mockSessionsRepo.Setup(sessionRepo => sessionRepo.GetActiveSessions()).ReturnsAsync(new List<ParkingSessionModel>());

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
        _mockSessionsRepo.Setup(sessionRepo => sessionRepo.GetActiveSessionByLicensePlate(plate)).ReturnsAsync(session);

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
        _mockSessionsRepo.Setup(sessionRepo => sessionRepo.GetActiveSessionByLicensePlate(plate)).ReturnsAsync((ParkingSessionModel?)null);

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

        _mockSessionsRepo.Setup(sessionRepo => sessionRepo.GetAllRecentSessionsByLicensePlate(normalizedPlate, duration)).ReturnsAsync(sessions);

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

        _mockSessionsRepo.Setup(sessionRepo => sessionRepo.GetAllRecentSessionsByLicensePlate(normalizedPlate, duration)).ReturnsAsync(new List<ParkingSessionModel>());

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
        _mockSessionsRepo.Setup(sessionRepo => sessionRepo.GetById<ParkingSessionModel>(sessionId)).ReturnsAsync(session);

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
        _mockSessionsRepo.Setup(sessionRepo => sessionRepo.GetById<ParkingSessionModel>(sessionId)).ReturnsAsync(session);

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
        var userPlates = new List<UserPlateModel>
        {
            new UserPlateModel { UserId = userId, LicensePlateNumber = plate, CreatedAt = DateOnly.FromDateTime(plateAddedTime) }
        };

        _mockSessionsRepo.Setup(sessionRepo => sessionRepo.GetById<ParkingSessionModel>(sessionId)).ReturnsAsync(session);
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
        var userPlates = new List<UserPlateModel>
        {
            new UserPlateModel
            {
                UserId = userId,
                LicensePlateNumber = userPlate,
                CreatedAt = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-2))
            }
        };

        _mockSessionsRepo.Setup(sessionRepo => sessionRepo.GetById<ParkingSessionModel>(sessionId)).ReturnsAsync(session);
        _mockUserPlateService.Setup(s => s.GetUserPlatesByUserId(userId)).ReturnsAsync(new GetUserPlateListResult.Success(userPlates));

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
        var sessionStartTime = DateTime.UtcNow.AddDays(sessionStartedDaysAgo); // Session started BEFORE plate was added

        var session = new ParkingSessionModel
        {
            Id = sessionId,
            ParkingLotId = lotId,
            LicensePlateNumber = plate,
            Started = sessionStartTime
        };
        var userPlates = new List<UserPlateModel>
        {
            new UserPlateModel
            {
                UserId = userId,
                LicensePlateNumber = plate,
                CreatedAt = DateOnly.FromDateTime(plateAddedTime)
            }
        };

        _mockSessionsRepo.Setup(sessionRepo => sessionRepo.GetById<ParkingSessionModel>(sessionId)).ReturnsAsync(session);
        _mockUserPlateService.Setup(uPlateService => uPlateService.GetUserPlatesByUserId(userId)).ReturnsAsync(new GetUserPlateListResult.Success(userPlates));

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
        _mockSessionsRepo.Setup(sessionRepo => sessionRepo.GetByParkingLotId(lotId)).ReturnsAsync(sessions);

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

        _mockSessionsRepo.Setup(sessionRepo => sessionRepo.GetByParkingLotId(lotId)).ReturnsAsync(new List<ParkingSessionModel> { ownedSession, randomSession, tooOldSession });

        var userPlates = new List<UserPlateModel>
        {
            new UserPlateModel
            {
                UserId = userId,
                LicensePlateNumber = ownedPlate,
                CreatedAt = DateOnly.FromDateTime(plateAddedTime)
            }
        };
        _mockUserPlateService.Setup(uPlateService => uPlateService.GetUserPlatesByUserId(userId)).ReturnsAsync(new GetUserPlateListResult.Success(userPlates));

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
        _mockSessionsRepo.Setup(r => r.GetByParkingLotId(lotId)).ReturnsAsync(new List<ParkingSessionModel>());

        // Act
        var result = await _sessionService.GetAuthorizedSessionsAsync(userId, lotId, false);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(0, result.Count);
    }

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
        _mockParkingLotService.Setup(lotService => lotService.GetParkingLotById(lotId)).ReturnsAsync(new GetLotResult.NotFound());

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

        _mockParkingLotService.Setup(lotService => lotService.GetParkingLotById(lotId))
            .ReturnsAsync(new GetLotResult.Success(lot));

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

        _mockParkingLotService.Setup(lotService => lotService.GetParkingLotById(lotId)).ReturnsAsync(new GetLotResult.Success(lot));

        _mockSessionsRepo.Setup(sessionRepo => sessionRepo.GetActiveSessionByLicensePlate(expectedPlate)).ReturnsAsync(activeSession);

        // Act
        var result = await _sessionService.StartSession(dto, token, (decimal)amount, user);

        // Assert
        Assert.IsInstanceOfType(result, typeof(StartSessionResult.AlreadyActive));
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

        _mockParkingLotService.Setup(lotService => lotService.GetParkingLotById(lotId)).ReturnsAsync(new GetLotResult.Success(lot));
        _mockSessionsRepo.Setup(sessionRepo => sessionRepo.GetActiveSessionByLicensePlate(expectedPlate)).ReturnsAsync((ParkingSessionModel?)null);

        _mockParkingLotService.Setup(lotService => lotService.UpdateParkingLot(lotId, It.IsAny<UpdateParkingLotDto>())).ReturnsAsync(new UpdateLotResult.Success(lot));
        _mockSessionsRepo.Setup(sessionRepo => sessionRepo.CreateWithId(
                It.Is<ParkingSessionModel>(sessionModel => sessionModel.LicensePlateNumber == expectedPlate))).ReturnsAsync((false, 0L));

        _mockParkingLotService.Setup(lotService => lotService.UpdateParkingLot(
                lotId, It.Is<UpdateParkingLotDto>(updateDto => updateDto.Reserved == reserved))).ReturnsAsync(new UpdateLotResult.Success(lot));

        // Act
        var result = await _sessionService.StartSession(dto, token, (decimal)amount, user, false);

        // Assert
        Assert.IsInstanceOfType(result, typeof(StartSessionResult.Error));
        StringAssert.Contains(((StartSessionResult.Error)result).Message, "Failed to persist");
        _mockParkingLotService.Verify(lotService => lotService.UpdateParkingLot(lotId, It.Is<UpdateParkingLotDto>(u => u.Reserved == reserved)), Times.Once);
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

        _mockParkingLotService.Setup(lotService => lotService.GetParkingLotById(lotId))
            .ReturnsAsync(new GetLotResult.Success(lot));
        _mockSessionsRepo.Setup(sessionRepo => sessionRepo.GetActiveSessionByLicensePlate(expectedPlate))
            .ReturnsAsync((ParkingSessionModel?)null);

        // Act
        var result = await _sessionService.StartSession(dto, token, (decimal)amount, user, true);
        // Beware: Currently, this is a static call. It sends a real request to the PreAuth service.

        // Assert
        Assert.IsInstanceOfType(result, typeof(StartSessionResult.PreAuthFailed));
        _mockSessionsRepo.Verify(sessionRepo => sessionRepo.CreateWithId(It.IsAny<ParkingSessionModel>()), Times.Never);
    }

    // Note: Due to the stateful nature of the PreAuth service and external dependencies, successful pre-authorization tests cannot be included here.

    #endregion
}
