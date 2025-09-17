using MobyPark.Models;
using MobyPark.Services;
using Moq;

namespace MobyPark.Tests;

[TestClass]
public sealed class ParkingSessionServiceTests
{
    private ParkingSessionService _parkingSessionService;

    [TestInitialize]
    public void TestInitialize()
    {
        _parkingSessionService = new ParkingSessionService();
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
        var (price, hours, days) = _parkingSessionService.CalculatePrice(parkingLot, session);

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
        var (price, hours, days) = _parkingSessionService.CalculatePrice(parkingLot, session);

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
        var result = _parkingSessionService.GeneratePaymentHash(sessionId, licensePlate);

        // Assert
        Assert.AreEqual(expectedHash, result);
    }

    [TestMethod]
    public void GenerateTransactionValidationHash_ReturnsUniqueHash()
    {
        // Act
        var hash1 = _parkingSessionService.GenerateTransactionValidationHash();
        var hash2 = _parkingSessionService.GenerateTransactionValidationHash();

        // Assert
        Assert.IsNotNull(hash1);
        Assert.IsNotNull(hash2);
        Assert.AreNotEqual(hash1, hash2);
    }
}