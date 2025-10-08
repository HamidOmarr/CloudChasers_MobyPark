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

        _mockDataService.Setup(access => access.ParkingLots).Returns(_mockParkingLotAccess.Object);

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
            CreatedAt = DateOnly.FromDateTime(DateTime.UtcNow),
            Coordinates = new CoordinatesModel { Lat = 10.5, Lng = 20.5 }
        };

        _mockParkingLotAccess!
            .Setup(access => access.GetById(id))
            .ReturnsAsync(expected);

        // Act
        ParkingLotModel? result = await _parkingLotService!.GetParkingLotById(id);

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
    public async Task GetParkingLotById_InvalidId_ReturnsNull(int id)
    {
        // Arrange
        _mockParkingLotAccess!
            .Setup(access => access.GetById(id))
            .ReturnsAsync((ParkingLotModel?)null);

        // Act
        ParkingLotModel? result = await _parkingLotService!.GetParkingLotById(id);

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    [DataRow(1, "1", "Wijnhaven", "Wijnhaven 1", 100, 10, 5.0, 20.0, 10.5, 20.5)]
    [DataRow(2, "2", "Kralingse Zoom", "Kralingse Zoom 1", 200, 50, 10.0, 25.0, 30.0, 40.0)]
    [DataRow(3, "3", "Centrum", "Centrum 1", 300, 100, 7.5, 15.0, -15.0, 120.0)]
    public async Task UpdateParkingLot_ValidData_UpdatesAndReturnsParkingLot(
        int id, string name, string location, string address,
        int capacity, int reserved, double tariffDouble, double dayTariffDouble,
        double latitude, double longitude)
    {
        // Arrange
        decimal tariff = (decimal)tariffDouble;
        decimal dayTariff = (decimal)dayTariffDouble;

        var createdAt = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1));
        var coordinates = new CoordinatesModel { Lat = latitude, Lng = longitude };

        _mockParkingLotAccess!
            .Setup(access => access.Update(It.IsAny<ParkingLotModel>()))
            .ReturnsAsync(true).Verifiable();

        var lot = new ParkingLotModel
        {
            Id = id,
            Name = name,
            Location = location,
            Address = address,
            Capacity = capacity,
            Reserved = reserved,
            Tariff = tariff,
            DayTariff = dayTariff,
            CreatedAt = createdAt,
            Coordinates = coordinates
        };

        // Act
        var result = await _parkingLotService!.UpdateParkingLot(lot);

        // Assert
        Assert.IsTrue(result);

        _mockParkingLotAccess.Verify(access => access.Update(It.Is<ParkingLotModel>(parkingLot =>
            parkingLot.Id == id &&
            parkingLot.Name == name &&
            parkingLot.Location == location &&
            parkingLot.Address == address &&
            parkingLot.Capacity == capacity &&
            parkingLot.Reserved == reserved &&
            parkingLot.Tariff == tariff &&
            parkingLot.DayTariff == dayTariff &&
            parkingLot.CreatedAt == createdAt &&
            (decimal)parkingLot.Coordinates.Lat == (decimal)latitude &&
            (decimal)parkingLot.Coordinates.Lng == (decimal)longitude
        )), Times.Once);
    }

    [TestMethod]
    [DataRow(1, "1", "Wijnhaven", "Wijnhaven 1", 100, 10, 5.0, 20.0, 10.5, 20.5)]
    [DataRow(2, "2", "Kralingse Zoom", "Kralingse Zoom 1", 200, 50, 10.0, 25.0, 30.0, 40.0)]
    [DataRow(3, "3", "Centrum", "Centrum 1", 300, 100, 7.5, 15.0, -15.0, 120.0)]
    public async Task CreateParkingLot_ValidData_CreatesAndReturnsParkingLot(
        int id, string name, string location, string address,
        int capacity, int reserved, double tariffDouble, double dayTariffDouble,
        double latitude, double longitude)
    {
        // Arrange
        decimal tariff = (decimal)tariffDouble;
        decimal dayTariff = (decimal)dayTariffDouble;

        var lot = new ParkingLotModel
        {
            Id = id,
            Name = name,
            Location = location,
            Address = address,
            Capacity = capacity,
            Reserved = reserved,
            Tariff = tariff,
            DayTariff = dayTariff,
            Coordinates = new CoordinatesModel { Lat = latitude, Lng = longitude }
        };

        _mockParkingLotAccess!
            .Setup(access => access.Create(It.IsAny<ParkingLotModel>()))
            .ReturnsAsync(true);

        // Act
        var result = await _parkingLotService!.CreateParkingLot(lot);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(name, result.Name);
        Assert.AreEqual(location, result.Location);
        _mockParkingLotAccess.Verify(access => access.CreateWithId(It.Is<ParkingLotModel>(pl =>
            pl.Name == name &&
            pl.Location == location &&
            pl.Capacity == capacity &&
            pl.Tariff == tariff &&
            pl.DayTariff == dayTariff &&
            (decimal)pl.Coordinates.Lat == (decimal)latitude &&
            (decimal)pl.Coordinates.Lng == (decimal)longitude
        )), Times.Once);
    }

    [TestMethod]
    [DataRow(null, "Location", "Address", 100, 5.0, 10.0, 45.0, 90.0)]
    [DataRow("Name", null, "Address", 100, 5.0, 10.0, 45.0, 90.0)]
    [DataRow("Name", "Location", null, 100, 5.0, 10.0, 45.0, 90.0)]
    public async Task CreateParkingLot_NullValues_ThrowsArgumentNullException(
        string name, string location, string address, int capacity,
        double tariff, double dayTariff, double latitude, double longitude)
    {
        // Arrange
        var lot = new ParkingLotModel
        {
            Name = name,
            Location = location,
            Address = address,
            Capacity = capacity,
            Tariff = (decimal)tariff,
            DayTariff = (decimal)dayTariff,
            Coordinates = new CoordinatesModel { Lat = latitude, Lng = longitude }
        };

        // Act & Assert
        await Assert.ThrowsExceptionAsync<ArgumentNullException>(async () =>
            await _parkingLotService!.CreateParkingLot(lot));

        _mockParkingLotAccess!.Verify(access => access.Create(It.IsAny<ParkingLotModel>()), Times.Never);
    }

    [TestMethod]
    [DataRow("Name", "Location", "Address", 0, 5.0, 10.0, 45.0, 90.0)]
    [DataRow("Name", "Location", "Address", 100, -1.0, 10.0, 45.0, 90.0)]
    [DataRow("Name", "Location", "Address", 100, 5.0, -1.0, 45.0, 90.0)]
    [DataRow("Name", "Location", "Address", 100, 5.0, 10.0, -100.0, 90.0)]
    [DataRow("Name", "Location", "Address", 100, 5.0, 10.0, 45.0, 200.0)]
    public async Task CreateParkingLot_OutOfRangeValues_ThrowsArgumentOutOfRangeException(
        string name, string location, string address, int capacity,
        double tariff, double dayTariff, double latitude, double longitude)
    {
        // Arrange
        var lot = new ParkingLotModel
        {
            Name = name,
            Location = location,
            Address = address,
            Capacity = capacity,
            Tariff = (decimal)tariff,
            DayTariff = (decimal)dayTariff,
            Coordinates = new CoordinatesModel { Lat = latitude, Lng = longitude }
        };

        // Act & Assert
        await Assert.ThrowsExceptionAsync<ArgumentOutOfRangeException>(async () =>
            await _parkingLotService!.CreateParkingLot(lot));

        _mockParkingLotAccess!.Verify(access => access.Create(It.IsAny<ParkingLotModel>()), Times.Never);
    }


    [TestMethod]
    [DataRow(1, "Lot A")]
    [DataRow(2, "Lot B")]
    public async Task DeleteParkingLot_ExistingId_DeletesAndReturnsTrue(int id, string name)
    {
        // Arrange
        var lot = new ParkingLotModel
        {
            Id = id,
            Name = name,
            Location = "Test Location",
            Address = "Test Address",
            Capacity = 100,
            Tariff = 2,
            DayTariff = 5,
            Coordinates = new CoordinatesModel { Lat = 0, Lng = 0 }
        };

        _mockParkingLotAccess!
            .Setup(access => access.GetById(id))
            .ReturnsAsync(lot);
        _mockParkingLotAccess!
            .Setup(access => access.Delete(id))
            .ReturnsAsync(true);

        // Act
        bool result = await _parkingLotService!.DeleteParkingLot(id);

        // Assert
        Assert.IsTrue(result);
        _mockParkingLotAccess.Verify(access => access.Delete(id), Times.Once);
    }

    [TestMethod]
    [DataRow(10)]
    [DataRow(99)]
    public async Task DeleteParkingLot_NotFound_ThrowsKeyNotFoundException(int id)
    {
        // Arrange
        _mockParkingLotAccess!
            .Setup(access => access.GetById(id))
            .ReturnsAsync((ParkingLotModel?)null);

        // Act & Assert
        await Assert.ThrowsExceptionAsync<KeyNotFoundException>(async () =>
            await _parkingLotService!.DeleteParkingLot(id));

        _mockParkingLotAccess.Verify(access => access.Delete(It.IsAny<int>()), Times.Never);
    }

    [TestMethod]
    public async Task GetAllParkingLots_ReturnsAllLots()
    {
        // Arrange
        var lots = new List<ParkingLotModel>
        {
            new() { Id = 1, Name = "Lot A", Location = "City A", Address = "Street 1", Capacity = 100, Tariff = 2, DayTariff = 10, Coordinates = new CoordinatesModel { Lat = 0, Lng = 0 } },
            new() { Id = 2, Name = "Lot B", Location = "City B", Address = "Street 2", Capacity = 200, Tariff = 3, DayTariff = 12, Coordinates = new CoordinatesModel { Lat = 10, Lng = 20 } }
        };

        _mockParkingLotAccess!
            .Setup(access => access.GetAll())
            .ReturnsAsync(lots);

        // Act
        List<ParkingLotModel> result = await _parkingLotService!.GetAllParkingLots();

        // Assert
        Assert.AreEqual(2, result.Count);
        Assert.AreEqual("Lot A", result[0].Name);
        Assert.AreEqual("Lot B", result[1].Name);
        _mockParkingLotAccess.Verify(access => access.GetAll(), Times.Once);
    }
}
