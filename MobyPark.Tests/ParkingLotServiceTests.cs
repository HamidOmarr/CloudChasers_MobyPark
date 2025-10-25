using MobyPark.Models;
using MobyPark.Models.Access;
using MobyPark.Models.DataService;
using MobyPark.Services;
using MobyPark.Services.Results.ParkingLot;
using Moq;

namespace MobyPark.Tests;

[TestClass]
public sealed class ParkingLotServiceTests
{
    private Mock<IDataAccess>? _mockDataService;
    private Mock<IParkingLotAccess>? _mockParkingLotAccess;
    private Mock<SessionService>? _mockSessions;
    private ParkingLotService? _parkingLotService;

    [TestInitialize]
    public void TestInitialize()
    {
        _mockDataService = new Mock<IDataAccess>();
        _mockParkingLotAccess = new Mock<IParkingLotAccess>();
        _mockSessions = new Mock<SessionService>();

        _mockDataService.Setup(access => access.ParkingLots).Returns(_mockParkingLotAccess.Object);

        _parkingLotService = new ParkingLotService(_mockDataService.Object, _mockSessions.Object);
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
        };

        // Act
        var result = await _parkingLotService!.UpdateParkingLotByIDAsync(lot, lot.Id);

        // Assert
        Assert.AreEqual(result, new RegisterResult.Success(lot));

        _mockParkingLotAccess.Verify(access => access.Update(It.Is<ParkingLotModel>(parkingLot =>
            parkingLot.Id == id &&
            parkingLot.Name == name &&
            parkingLot.Location == location &&
            parkingLot.Address == address &&
            parkingLot.Capacity == capacity &&
            parkingLot.Reserved == reserved &&
            parkingLot.Tariff == tariff &&
            parkingLot.DayTariff == dayTariff &&
            parkingLot.CreatedAt == createdAt
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
        };

        _mockParkingLotAccess!
            .Setup(access => access.Create(It.IsAny<ParkingLotModel>()))
            .ReturnsAsync(true);

        // Act
        var result = await _parkingLotService!.InsertParkingLotAsync(lot);

        // Assert
        Assert.IsNotNull(result);
        _mockParkingLotAccess.Verify(access => access.CreateWithId(It.Is<ParkingLotModel>(pl =>
            pl.Name == name &&
            pl.Location == location &&
            pl.Capacity == capacity &&
            pl.Tariff == tariff &&
            pl.DayTariff == dayTariff
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
            DayTariff = (decimal)dayTariff
        };

        // Act & Assert
        await Assert.ThrowsExceptionAsync<ArgumentNullException>(async () =>
            await _parkingLotService!.InsertParkingLotAsync(lot));

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
            DayTariff = (decimal)dayTariff
        };

        // Act & Assert
        await Assert.ThrowsExceptionAsync<ArgumentOutOfRangeException>(async () =>
            await _parkingLotService!.InsertParkingLotAsync(lot));

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
            DayTariff = 5
        };

        _mockParkingLotAccess!
            .Setup(access => access.GetById(id))
            .ReturnsAsync(lot);
        _mockParkingLotAccess!
            .Setup(access => access.Delete(id))
            .ReturnsAsync(true);

        // Act
        RegisterResult result = await _parkingLotService!.DeleteParkingLotByIDAsync(id);

        // Assert
        Assert.AreEqual(result, new RegisterResult.SuccessfullyDeleted());
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
            await _parkingLotService!.DeleteParkingLotByIDAsync(id));

        _mockParkingLotAccess.Verify(access => access.Delete(It.IsAny<int>()), Times.Never);
    }
    
}
