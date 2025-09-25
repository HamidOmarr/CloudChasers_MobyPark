using MobyPark.Models;
using MobyPark.Models.Access;
using MobyPark.Models.DataService;
using MobyPark.Services;
using Moq;

namespace MobyPark.Tests;

[TestClass]
public sealed class ParkingSessionServiceTests
{
    private Mock<IDataAccess>? _mockDataService;
    private Mock<IParkingSessionAccess>? _mockParkingSessionAccess;
    private ParkingSessionService? _parkingSessionService;

    private IDataAccess? _dataService;

    [TestInitialize]
    public void TestInitialize()
    {
        _mockDataService = new Mock<IDataAccess>();
        _mockParkingSessionAccess = new Mock<IParkingSessionAccess>();

        _dataService = _mockDataService.Object;
        _parkingSessionService = new(_dataService);

        _mockDataService.Setup(access => access.ParkingSessions).Returns(_mockParkingSessionAccess.Object);

        _mockParkingSessionAccess.Setup(access => access.Create(It.IsAny<ParkingSessionModel>())).ReturnsAsync(true);
    }

    [TestMethod]
    [DataRow(120, 0, 0, 0)]
    [DataRow(3600, 15.0, 1, 0)]
    [DataRow(10800, 45.0, 3, 0)]
    [DataRow(36000, 60.0, 10, 0)]
    public void CalculatePrice_SameDay_ReturnsCorrectPrice(int totalSeconds, double expectedPrice, int expectedHours, int expectedDays)
    {
        // Arrange
        var parkingLot = new ParkingLotModel { Tariff = 15.0m, DayTariff = 60.0m };
        var startTime = new DateTime(2023, 10, 26, 10, 0, 0);
        var endTime = startTime.AddSeconds(totalSeconds);

        var session = new ParkingSessionModel
        {
            Started = startTime,
            Stopped = endTime
        };

        // Act
        var (price, hours, days) = _parkingSessionService!.CalculatePrice(parkingLot, session);

        // Assert
        Assert.AreEqual((decimal)expectedPrice, price);
        Assert.AreEqual(expectedHours, hours);
        Assert.AreEqual(expectedDays, days);
    }

    [TestMethod]
    [DataRow(1, 60.0, 24, 1)]
    [DataRow(1.5, 120.0, 36, 2)]
    [DataRow(2, 120.0, 48, 2)]
    public void CalculatePrice_MultipleDays_ReturnsCorrectPrice(double totalDays, double expectedPrice, int expectedHours, int expectedDays)
    {
        // Arrange
        var parkingLot = new ParkingLotModel { Tariff = 15.0m, DayTariff = 60.0m };
        var startTime = new DateTime(2023, 10, 26, 10, 0, 0);
        var endTime = startTime.AddDays(totalDays);

        var session = new ParkingSessionModel
        {
            Started = startTime,
            Stopped = endTime
        };

        // Act
        var (price, hours, days) = _parkingSessionService!.CalculatePrice(parkingLot, session);

        // Assert
        Assert.AreEqual((decimal)expectedPrice, price);
        Assert.AreEqual(expectedHours, hours);
        Assert.AreEqual(expectedDays, days);
    }

    [TestMethod]
    [DataRow("ses123", "lic123", "edcb1a402e1b9269b087d43600511fb2")]
    [DataRow("ses456", "lic456", "9e9610705b8d6740b402e09444a9739e")]
    [DataRow("short", "data", "e55cc4695f14d36596170f103755cb35")]
    public void GeneratePaymentHash_ReturnsCorrectHash(string sessionId, string licensePlate, string expectedHash)
    {
        // Act
        string result = _parkingSessionService!.GeneratePaymentHash(sessionId, licensePlate);

        // Assert
        Assert.AreEqual(expectedHash, result);
    }

    [TestMethod]
    public void GenerateTransactionValidationHash_ReturnsUniqueHash()
    {
        // Act
        string hash1 = _parkingSessionService!.GenerateTransactionValidationHash();
        string hash2 = _parkingSessionService.GenerateTransactionValidationHash();

        // Assert
        Assert.IsNotNull(hash1);
        Assert.IsNotNull(hash2);
        Assert.AreNotEqual(hash1, hash2);
    }

    [TestMethod]
    [DataRow(1)]
    [DataRow(2)]
    public async Task GetParkingSessionById_ValidId_ReturnsSession(int id)
    {
        var expected = new ParkingSessionModel { Id = id };
        _mockParkingSessionAccess!.Setup(access => access.GetById(id)).ReturnsAsync(expected);

        ParkingSessionModel result = await _parkingSessionService!.GetParkingSessionById(id);

        Assert.IsNotNull(result);
        Assert.AreEqual(id, result.Id);
        _mockParkingSessionAccess.Verify(access => access.GetById(id), Times.Once);
    }

    [TestMethod]
    [DataRow(99)]
    [DataRow(1000)]
    public async Task GetParkingSessionById_InvalidId_ThrowsKeyNotFoundException(int id)
    {
        _mockParkingSessionAccess!.Setup(access => access.GetById(id)).ReturnsAsync((ParkingSessionModel?)null);

        await Assert.ThrowsExceptionAsync<KeyNotFoundException>(async () =>
            await _parkingSessionService!.GetParkingSessionById(id));

        _mockParkingSessionAccess.Verify(access => access.GetById(id), Times.Once);
    }

    [TestMethod]
    [DataRow(1)]
    [DataRow(2)]
    public async Task DeleteParkingSession_ExistingSession_ReturnsTrue(int id)
    {
        var session = new ParkingSessionModel { Id = id };
        _mockParkingSessionAccess!.Setup(access => access.GetById(id)).ReturnsAsync(session);
        _mockParkingSessionAccess.Setup(access => access.Delete(id)).ReturnsAsync(true);

        bool result = await _parkingSessionService!.DeleteParkingSession(id);

        Assert.IsTrue(result);
        _mockParkingSessionAccess.Verify(access => access.Delete(id), Times.Once);
    }

    [TestMethod]
    [DataRow(99)]
    [DataRow(1000)]
    public async Task DeleteParkingSession_NotFound_ThrowsKeyNotFoundException(int id)
    {
        _mockParkingSessionAccess!.Setup(access => access.GetById(id)).ReturnsAsync((ParkingSessionModel?)null);

        await Assert.ThrowsExceptionAsync<KeyNotFoundException>(async () =>
            await _parkingSessionService!.DeleteParkingSession(id));

        _mockParkingSessionAccess.Verify(access => access.Delete(It.IsAny<int>()), Times.Never);
    }

    [TestMethod]
    [DataRow(1)]
    [DataRow(2)]
    public async Task GetParkingSessionsByParkingLotId_ValidLot_ReturnsSessions(int lotId)
    {
        var sessions = new List<ParkingSessionModel>
        {
            new() { Id = 1, ParkingLotId = lotId },
            new() { Id = 2, ParkingLotId = lotId }
        };
        _mockParkingSessionAccess!.Setup(access => access.GetByParkingLotId(lotId)).ReturnsAsync(sessions);

        List<ParkingSessionModel> result = await _parkingSessionService!.GetParkingSessionsByParkingLotId(lotId);

        Assert.AreEqual(sessions.Count, result.Count);
        _mockParkingSessionAccess.Verify(access => access.GetByParkingLotId(lotId), Times.Once);
    }

    [TestMethod]
    [DataRow(99)]
    [DataRow(1000)]
    public async Task GetParkingSessionsByParkingLotId_NoSessions_ThrowsKeyNotFoundException(int lotId)
    {
        _mockParkingSessionAccess!.Setup(access => access.GetByParkingLotId(lotId)).ReturnsAsync(new List<ParkingSessionModel>());

        await Assert.ThrowsExceptionAsync<KeyNotFoundException>(async () =>
            await _parkingSessionService!.GetParkingSessionsByParkingLotId(lotId));

        _mockParkingSessionAccess.Verify(access => access.GetByParkingLotId(lotId), Times.Once);
    }
}