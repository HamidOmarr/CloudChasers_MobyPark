using MobyPark.Models;
using MobyPark.Models.Access;
using MobyPark.Models.DataService;
using MobyPark.Services;
using Moq;

namespace MobyPark.Tests;

[TestClass]
public sealed class ParkingLotServiceTests
{
    private Mock<IDataAccess>? _mockDataService;
    private Mock<IParkingLotAccess>? _mockParkingLotAccess;
    private ParkingLotService? _parkingLotService;

    [TestInitialize]
    public void TestInitialize()
    {
        _mockDataService = new Mock<IDataAccess>();
        _mockParkingLotAccess = new Mock<IParkingLotAccess>();

        _mockDataService.Setup(ds => ds.ParkingLots).Returns(_mockParkingLotAccess.Object);

        _parkingLotService = new ParkingLotService(_mockDataService.Object);
    }

    [TestMethod]
    [DataRow(1, "1", "Wijnhaven", "Wijnhaven 1", 100, 10, 5.0, 20.0)]
    [DataRow(2, "2", "Kralingse Zoom", "Kralingse Zoom 1", 200, 50, 10.0, 25.0)]
    [DataRow(3, "3", "Centrum", "Centrum 1", 300, 100, 7.5, 15.0)]
    public async Task GetParkingLotById_ValidId_ReturnsParkingLot(
        int id, string name, string location, string address,
        int capacity, int reserved, double tariffDouble, double dayTariffDouble)
    {
        // Arrange
        decimal tariff = (decimal)tariffDouble;
        decimal dayTariff = (decimal)dayTariffDouble;

        var expected = new ParkingLotModel
        {
            Id = id,
            Name = name,
            Location = location,
            Address = address,
            Capacity = capacity,
            Reserved = reserved,
            Tariff = tariff,
            DayTariff = dayTariff,
            CreatedAt = DateTime.UtcNow,
            Coordinates = new CoordinatesModel { Lat = 10.5, Lng = 20.5 }
        };

        _mockParkingLotAccess!
            .Setup(access => access.GetById(id))
            .ReturnsAsync(expected);

        // Act
        var result = await _parkingLotService!.GetParkingLotById(id);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(expected.Id, result.Id);
        Assert.AreEqual(expected.Name, result.Name);
        Assert.AreEqual(expected.Location, result.Location);
        Assert.AreEqual(expected.Address, result.Address);
        Assert.AreEqual(expected.Capacity, result.Capacity);
        Assert.AreEqual(expected.Reserved, result.Reserved);
        Assert.AreEqual(expected.Tariff, result.Tariff);
        Assert.AreEqual(expected.DayTariff, result.DayTariff);

        _mockParkingLotAccess.Verify(access => access.GetById(id), Times.Once);
    }

    [TestMethod]
    [DataRow(10)]
    [DataRow(999)]
    public async Task GetParkingLotById_InvalidId_ThrowsKeyNotFoundException(int id)
    {
        // Arrange
        _mockParkingLotAccess!
            .Setup(access => access.GetById(id))
            .ReturnsAsync((ParkingLotModel?)null);

        // Act & Assert
        await Assert.ThrowsExceptionAsync<KeyNotFoundException>(async () =>
            await _parkingLotService!.GetParkingLotById(id));

        _mockParkingLotAccess.Verify(access => access.GetById(id), Times.Once);
    }

    [TestMethod]
    [DataRow(1, "Lot A", "Downtown", "123 Main St", 100, 10, 5.0, 20.0, 10.5, 20.5)]
    [DataRow(2, "Lot B", "Airport", "456 Sky Rd", 200, 50, 10.0, 25.0, 30.0, 40.0)]
    [DataRow(3, "Lot C", "Mall", "789 Shop Ln", 300, 100, 7.5, 15.0, -15.0, 120.0)]
    public async Task UpdateParkingLot_ValidData_UpdatesAndReturnsParkingLot(
        int id, string name, string location, string address,
        int capacity, int reserved, double tariffDouble, double dayTariffDouble,
        double latitude, double longitude)
    {
        // Arrange
        decimal tariff = (decimal)tariffDouble;
        decimal dayTariff = (decimal)dayTariffDouble;

        var createdAt = DateTime.UtcNow.AddDays(-1);
        var coordinates = new CoordinatesModel { Lat = latitude, Lng = longitude };

        _mockParkingLotAccess!
            .Setup(access => access.Update(It.IsAny<ParkingLotModel>()))
            .ReturnsAsync(true).Verifiable();

        // Act
        var result = await _parkingLotService!.UpdateParkingLot(
            id, name, location, address, capacity, reserved, tariff, dayTariff, createdAt, coordinates);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(id, result.Id);
        Assert.AreEqual(name, result.Name);
        Assert.AreEqual(location, result.Location);
        Assert.AreEqual(address, result.Address);
        Assert.AreEqual(capacity, result.Capacity);
        Assert.AreEqual(reserved, result.Reserved);
        Assert.AreEqual(tariff, result.Tariff);
        Assert.AreEqual(dayTariff, result.DayTariff);
        Assert.AreEqual(createdAt, result.CreatedAt);
        Assert.AreEqual(latitude, result.Coordinates.Lat);
        Assert.AreEqual(longitude, result.Coordinates.Lng);

        _mockParkingLotAccess.Verify(access => access.Update(It.Is<ParkingLotModel>(lot =>
            lot.Id == id &&
            lot.Name == name &&
            lot.Location == location &&
            lot.Address == address &&
            lot.Capacity == capacity &&
            lot.Reserved == reserved &&
            lot.Tariff == tariff &&
            lot.DayTariff == dayTariff &&
            lot.CreatedAt == createdAt &&
            Math.Abs(lot.Coordinates.Lat - latitude) < 0.1 &&
            Math.Abs(lot.Coordinates.Lng - longitude) < 0.1
        )), Times.Once);
    }
}
