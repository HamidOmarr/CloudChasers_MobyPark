using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using MobyPark.DTOs.ParkingLot.Request;
using MobyPark.Models;
using MobyPark.Models.Repositories.Interfaces;
using MobyPark.Services;
using MobyPark.Services.Results.ParkingLot;
using Moq;

namespace MobyPark.Tests;

[TestClass]
public sealed class ParkingLotServiceTests
{
    private Mock<IParkingLotRepository>? _mockParkingLotRepository;
    private ParkingLotService? _parkingLotService;

    [TestInitialize]
    public void TestInitialize()
    {
        _mockParkingLotRepository = new Mock<IParkingLotRepository>();
        _parkingLotService = new ParkingLotService(_mockParkingLotRepository.Object);
    }

    [TestMethod]
    [DataRow("Lot A", "Location A", "Address A", 100, 5.0, 20.0)]
    [DataRow("Lot B", "Location B", "Address B", 200, 10.0, null)]
    [DataRow("Lot C", "Location C", "Address C", 50, 7.5, 15.0)]
    public async Task CreateParkingLot_ValidData_ReturnsSuccessResult(
        string name, string location, string address,
        int capacity, double tariffDouble, double? dayTariffDouble)
    {
        // Arrange
        decimal tariff = (decimal)tariffDouble;
        decimal? dayTariff = dayTariffDouble.HasValue ? (decimal)dayTariffDouble.Value : null;

        var createDto = new CreateParkingLotDto
        {
            Name = name,
            Location = location,
            Address = address,
            Capacity = capacity,
            Tariff = tariff,
            DayTariff = dayTariff
        };

         _mockParkingLotRepository!
             .Setup(lotRepo => lotRepo.Exists(It.IsAny<Expression<Func<ParkingLotModel, bool>>>()))
             .ReturnsAsync(false);

        long expectedNewId = 123;
        _mockParkingLotRepository!
            .Setup(lotRepo => lotRepo.CreateWithId(It.IsAny<ParkingLotModel>()))
            .ReturnsAsync((true, expectedNewId))
            .Callback<ParkingLotModel>(createdLot => {
                 Assert.AreEqual(createDto.Name, createdLot.Name);
                 Assert.AreEqual(createDto.Address, createdLot.Address);
                 Assert.AreEqual(0, createdLot.Reserved);
                 Assert.AreEqual(createDto.Capacity, createdLot.Capacity);
                 Assert.AreEqual(createDto.Tariff, createdLot.Tariff);
                 Assert.AreEqual(createDto.DayTariff, createdLot.DayTariff);
                 Assert.AreNotEqual(default, createdLot.CreatedAt);
             });

        // Act
        var result = await _parkingLotService!.CreateParkingLot(createDto);

        // Assert
        Assert.IsInstanceOfType(result, typeof(CreateLotResult.Success));
        var successResult = (CreateLotResult.Success)result;
        var createdLot = successResult.Lot;

        Assert.AreEqual(expectedNewId, createdLot.Id);
        Assert.AreEqual(createDto.Name, createdLot.Name);
        Assert.AreEqual(createDto.Address, createdLot.Address);

        _mockParkingLotRepository.Verify(lotRepo => lotRepo.Exists(It.IsAny<Expression<Func<ParkingLotModel, bool>>>()), Times.Once);
        _mockParkingLotRepository.Verify(lotRepo => lotRepo.CreateWithId(It.IsAny<ParkingLotModel>()), Times.Once);
    }


    [TestMethod]
    [DataRow("Address 1")]
    [DataRow("Address 2")]
    public async Task CreateParkingLot_AddressExists_ReturnsErrorResult(string existingAddress)
    {
        // Arrange
        var createDto = new CreateParkingLotDto
        {
            Name = "New Lot",
            Location = "New Location",
            Address = existingAddress,
            Capacity = 50,
            Tariff = 1
        };

        _mockParkingLotRepository!.Setup(lotRepo => lotRepo.Exists(It.IsAny<Expression<Func<ParkingLotModel, bool>>>())).ReturnsAsync(true);

        // Act
        var result = await _parkingLotService!.CreateParkingLot(createDto);

        // Assert
        Assert.IsInstanceOfType(result, typeof(CreateLotResult.Error));
        _mockParkingLotRepository.Verify(lotRepo => lotRepo.CreateWithId(It.IsAny<ParkingLotModel>()), Times.Never);
    }

    [TestMethod]
    [DataRow("Lot A", "Location A", "Address A", 100, 5.0, 20.0)]
    [DataRow("Lot B", "Location B", "Address B", 200, 10.0, null)]
    public async Task CreateParkingLot_DatabaseInsertionFails_ReturnsErrorResult(
        string name, string location, string address,
        int capacity, double tariffDouble, double? dayTariffDouble)
    {
        // Arrange
        decimal tariff = (decimal)tariffDouble;
        decimal? dayTariff = dayTariffDouble.HasValue ? (decimal)dayTariffDouble.Value : null;

        var createDto = new CreateParkingLotDto
        {
            Name = name,
            Location = location,
            Address = address,
            Capacity = capacity,
            Tariff = tariff,
            DayTariff = dayTariff
        };

         _mockParkingLotRepository!.Setup(lotRepo => lotRepo.Exists(It.IsAny<Expression<Func<ParkingLotModel, bool>>>())).ReturnsAsync(false);

         _mockParkingLotRepository!.Setup(lotRepo => lotRepo.CreateWithId(It.IsAny<ParkingLotModel>())).ReturnsAsync((false, 0L));

         // Act
         var result = await _parkingLotService!.CreateParkingLot(createDto);

         // Assert
         Assert.IsInstanceOfType(result, typeof(CreateLotResult.Error));
         var errorResult = (CreateLotResult.Error)result;
         Assert.AreEqual("Database insertion failed.", errorResult.Message);
         _mockParkingLotRepository.Verify(lotRepo => lotRepo.CreateWithId(It.IsAny<ParkingLotModel>()), Times.Once);
    }

    [TestMethod]
    [DataRow(null, "Location", "Address", 100, 5.0, 10.0, 45.0, 90.0)]
    [DataRow("Name", null, "Address", 100, 5.0, 10.0, 45.0, 90.0)]
    [DataRow("Name", "Location", null, 100, 5.0, 10.0, 45.0, 90.0)]
    public async Task CreateParkingLot_NullValues_ReturnsErrorResult(
        string name, string location, string address, int capacity,
        double tariff, double dayTariff, double latitude, double longitude)
    {
        // Arrange
        var lot = new CreateParkingLotDto
        {
            Name = name,
            Location = location,
            Address = address,
            Capacity = capacity,
            Tariff = (decimal)tariff,
            DayTariff = (decimal)dayTariff
        };

        // Act
        var result = await _parkingLotService!.CreateParkingLot(lot);
        Assert.IsInstanceOfType(result, typeof(CreateLotResult.Error));

        // Assert
        _mockParkingLotRepository!.Verify(access => access.Create(It.IsAny<ParkingLotModel>()), Times.Never);
    }

    [TestMethod]
    public async Task CreateParkingLot_RepositoryThrows_ReturnsErrorResult()
    {
        // Arrange
        var createDto = new CreateParkingLotDto { Name="Fail", Address="Fail Address", Capacity=10, Tariff=1, Location="Fail" };
        _mockParkingLotRepository!.Setup(lotRepo => lotRepo.Exists(It.IsAny<Expression<Func<ParkingLotModel, bool>>>())).ReturnsAsync(false);
        _mockParkingLotRepository!.Setup(lotRepo => lotRepo.CreateWithId(It.IsAny<ParkingLotModel>())).ThrowsAsync(new InvalidOperationException("DB Boom!")); // Db exception

        // Act
        var result = await _parkingLotService!.CreateParkingLot(createDto);

        // Assert
        Assert.IsInstanceOfType(result, typeof(CreateLotResult.Error));
        StringAssert.Contains(((CreateLotResult.Error)result).Message, "DB Boom!");
    }

    [TestMethod]
    [DataRow(1, "1", "Wijnhaven", "Wijnhaven 1", 100, 10, 5.0, 20.0)]
    [DataRow(2, "2", "Kralingse Zoom", "Kralingse Zoom 1", 200, 50, 10.0, 25.0)]
    [DataRow(3, "3", "Centrum", "Centrum 1", 300, 100, 7.5, 15.0)]
    public async Task GetParkingLotById_ValidId_ReturnsSuccessResultWithLot(
        int id, string name, string location, string address,
        int capacity, int reserved, double tariffDouble, double dayTariffDouble)
    {
        // Arrange
        var tariff = (decimal)tariffDouble;
        var dayTariff = (decimal)dayTariffDouble;

        var expected = new ParkingLotModel
        {
            Id = id, Name = name, Location = location, Address = address, Capacity = capacity,
            Reserved = reserved, Tariff = tariff, DayTariff = dayTariff,
            CreatedAt = DateOnly.FromDateTime(DateTime.UtcNow)
        };

        _mockParkingLotRepository!
            .Setup(lotRepo => lotRepo.GetById<ParkingLotModel>(id))
            .ReturnsAsync(expected);

        // Act
        var result = await _parkingLotService!.GetParkingLotById(id);

        // Assert
        Assert.IsInstanceOfType(result, typeof(GetLotResult.Success));
        var successResult = (GetLotResult.Success)result;
        var actualLot = successResult.Lot;

        Assert.IsNotNull(actualLot);
        Assert.AreEqual(expected.Id, actualLot.Id);
        Assert.AreEqual(expected.Name, actualLot.Name);
        Assert.AreEqual(expected.Location, actualLot.Location);
        Assert.AreEqual(expected.Address, actualLot.Address);
        Assert.AreEqual(expected.Capacity, actualLot.Capacity);
        Assert.AreEqual(expected.Reserved, actualLot.Reserved);
        Assert.AreEqual(expected.Tariff, actualLot.Tariff);
        Assert.AreEqual(expected.DayTariff, actualLot.DayTariff);

        _mockParkingLotRepository.Verify(lotRepo => lotRepo.GetById<ParkingLotModel>(id), Times.Once);
    }

    [TestMethod]
    [DataRow(10)]
    [DataRow(999)]
    public async Task GetParkingLotById_InvalidId_ReturnsNotFoundResult(int id)
    {
        // Arrange
        _mockParkingLotRepository!
            .Setup(lotRepo => lotRepo.GetById<ParkingLotModel>(id))
            .ReturnsAsync((ParkingLotModel?)null);

        // Act
        var result = await _parkingLotService!.GetParkingLotById(id);

        // Assert
        Assert.IsInstanceOfType(result, typeof(GetLotResult.NotFound));
        _mockParkingLotRepository.Verify(lotRepo => lotRepo.GetById<ParkingLotModel>(id), Times.Once);
    }

    [TestMethod]
    [DataRow(1, "New Name", null, null, null, null, null)]
    [DataRow(2, null, "New Location", null, 150, null, null)]
    [DataRow(3, "Lot C Updated", "Central Updated", "Main St 123", 350, 8.0, 18.0)]
    public async Task UpdateParkingLot_ValidData_ReturnsSuccessResult(
        int id, string? name, string? location, string? address,
        int? capacity, double? tariffDouble, double? dayTariffDouble)
    {
        // Arrange
        decimal? tariff = tariffDouble.HasValue ? (decimal)tariffDouble.Value : null;
        decimal? dayTariff = dayTariffDouble.HasValue ? (decimal)dayTariffDouble.Value : null;

        var updateDto = new UpdateParkingLotDto
        {
            Name = name,
            Location = location,
            Address = address,
            Capacity = capacity,
            Tariff = tariff,
            DayTariff = dayTariff
        };

        var existingLot = new ParkingLotModel
        {
            Id = id,
            Name = "Old Name",
            Location = "Old Location",
            Address = "Old Address",
            Capacity = 100,
            Reserved = 20,
            Tariff = 5,
            DayTariff = 15
        };

        _mockParkingLotRepository!
            .Setup(lotRepo => lotRepo.GetById<ParkingLotModel>(id))
            .ReturnsAsync(existingLot);

        _mockParkingLotRepository!
            .Setup(lotRepo => lotRepo.Update(
                It.IsAny<ParkingLotModel>(),
                It.IsAny<UpdateParkingLotDto>()
            )).Callback<ParkingLotModel, UpdateParkingLotDto>((parkingLot, dto) =>
            {
                parkingLot.Name = dto.Name ?? parkingLot.Name;
                parkingLot.Location = dto.Location ?? parkingLot.Location;
                parkingLot.Address = dto.Address ?? parkingLot.Address;
                parkingLot.Capacity = dto.Capacity ?? parkingLot.Capacity;
                parkingLot.Tariff = dto.Tariff ?? parkingLot.Tariff;
                parkingLot.DayTariff = dto.DayTariff ?? parkingLot.DayTariff;
            }).ReturnsAsync(true);

        // Act
        var result = await _parkingLotService!.UpdateParkingLot(id, updateDto);

        // Assert
        Assert.IsInstanceOfType(result, typeof(UpdateLotResult.Success));
        var successResult = (UpdateLotResult.Success)result;
        var updatedLot = successResult.Lot;

        _mockParkingLotRepository.Verify(lotRepo => lotRepo.Update(existingLot, updateDto), Times.Once);

        Assert.AreEqual(id, updatedLot.Id);
        Assert.AreEqual(name ?? existingLot.Name, updatedLot.Name);
        Assert.AreEqual(location ?? existingLot.Location, updatedLot.Location);
        Assert.AreEqual(address ?? existingLot.Address, updatedLot.Address);
        Assert.AreEqual(capacity ?? existingLot.Capacity, updatedLot.Capacity);
        Assert.AreEqual(tariff ?? existingLot.Tariff, updatedLot.Tariff);
        Assert.AreEqual(dayTariff ?? existingLot.DayTariff, updatedLot.DayTariff);
        Assert.AreEqual(existingLot.Reserved, updatedLot.Reserved);
        Assert.AreEqual(existingLot.CreatedAt, updatedLot.CreatedAt);
    }

    [TestMethod]
    [DataRow(1, "Same Name", "Same Location")]
    [DataRow(2, null, null)]
    public async Task UpdateParkingLot_NoChangesMade_ReturnsNoChangesMadeResult(
        int id, string? name, string? location)
    {
        // Arrange
        var updateDto = new UpdateParkingLotDto
        {
            Name = name,
            Location = location
        };

        var existingLot = new ParkingLotModel
        {
            Id = id,
            Name = "Same Name",
            Location = "Same Location",
            Address = "Same Address",
            Capacity = 100,
            Reserved = 20,
            Tariff = 5,
            DayTariff = 15
        };

        _mockParkingLotRepository!.Setup(lotRepo => lotRepo.GetById<ParkingLotModel>(id)).ReturnsAsync(existingLot);
        _mockParkingLotRepository!.Setup(lotRepo => lotRepo.Update(It.IsAny<ParkingLotModel>(), It.IsAny<UpdateParkingLotDto>())).ReturnsAsync(false);

        // Act
        var result = await _parkingLotService!.UpdateParkingLot(id, updateDto);

        // Assert
        Assert.IsInstanceOfType(result, typeof(UpdateLotResult.NoChangesMade));
        _mockParkingLotRepository.Verify(lotRepo => lotRepo.Update(existingLot, updateDto), Times.Once);
    }

    [TestMethod]
    [DataRow(1, "New Name", "New Location", "New Address", 10, 5.0, 20.0)]
    [DataRow(2, "Another Name", "Location B", "Address B", 15, 10.0, null)]
    public async Task UpdateParkingLot_CapacityBelowReserved_ReturnsInvalidInputResult(
        int id, string name, string location, string address,
        int capacity, double tariffDouble, double? dayTariffDouble)
    {
        // Arrange
        decimal? tariff = (decimal)tariffDouble;
        decimal? dayTariff = dayTariffDouble.HasValue ? (decimal)dayTariffDouble.Value : null;

        var updateDto = new UpdateParkingLotDto
        {
            Name = name,
            Location = location,
            Address = address,
            Capacity = capacity,
            Tariff = tariff,
            DayTariff = dayTariff
        };

        var existingLot = new ParkingLotModel
        {
            Id = id,
            Name = "Old Name",
            Location = "Old Location",
            Address = "Old Address",
            Capacity = 100,
            Reserved = 20,
            Tariff = 5,
            DayTariff = 15
        };

        _mockParkingLotRepository!.Setup(lotRepo => lotRepo.GetById<ParkingLotModel>(id)).ReturnsAsync(existingLot);

        // Act
        var result = await _parkingLotService!.UpdateParkingLot(id, updateDto);

        // Assert
        Assert.IsInstanceOfType(result, typeof(UpdateLotResult.InvalidInput));
        _mockParkingLotRepository.Verify(lotRepo => lotRepo.Update(It.IsAny<ParkingLotModel>(), It.IsAny<UpdateParkingLotDto>()), Times.Never);
    }

    [TestMethod]
    [DataRow(1, "New Name", "New Location", "New Address", 150, 8.0, 18.0)]
    [DataRow(2, "Another Name", "Location B", "Address B", 250, 12.0, null)]
    public async Task UpdateParkingLot_LotNotFound_ReturnsNotFoundResult(
        int id, string name, string location, string address,
        int capacity, double tariffDouble, double? dayTariffDouble)
    {
        // Arrange
        decimal? tariff = (decimal)tariffDouble;
        decimal? dayTariff = dayTariffDouble.HasValue ? (decimal)dayTariffDouble.Value : null;

        var updateDto = new UpdateParkingLotDto
        {
            Name = name,
            Location = location,
            Address = address,
            Capacity = capacity,
            Tariff = tariff,
            DayTariff = dayTariff
        };

        _mockParkingLotRepository!.Setup(lotRepo => lotRepo.GetById<ParkingLotModel>(id)).ReturnsAsync((ParkingLotModel?)null);

        // Act
        var result = await _parkingLotService!.UpdateParkingLot(id, updateDto);

        // Assert
        Assert.IsInstanceOfType(result, typeof(UpdateLotResult.NotFound));
        _mockParkingLotRepository.Verify(lotRepo => lotRepo.Update(It.IsAny<ParkingLotModel>(), It.IsAny<UpdateParkingLotDto>()), Times.Never);
    }

    [TestMethod]
    public async Task UpdateParkingLot_RepositoryThrows_ReturnsErrorResult()
    {
        // Arrange
        int id = 1;
        var updateDto = new UpdateParkingLotDto { Name = "Fail Update" };
        var existingLot = new ParkingLotModel { Id = id };
        _mockParkingLotRepository!.Setup(lotRepo => lotRepo.GetById<ParkingLotModel>(id)).ReturnsAsync(existingLot);
        _mockParkingLotRepository!.Setup(lotRepo => lotRepo.Update(It.IsAny<ParkingLotModel>(), It.IsAny<UpdateParkingLotDto>())).ThrowsAsync(new TimeoutException("DB Timeout")); // Db timeout exception

        // Act
        var result = await _parkingLotService!.UpdateParkingLot(id, updateDto);

        // Assert
        Assert.IsInstanceOfType(result, typeof(UpdateLotResult.Error));
        StringAssert.Contains(((UpdateLotResult.Error)result).Message, "DB Timeout");
    }

    [TestMethod]
    [DataRow(1, "Lot A")]
    [DataRow(2, "Lot B")]
    public async Task DeleteParkingLot_ExistingId_DeletesSuccessfully(int id, string name)
    {
        // Arrange
        var lotToDelete = new ParkingLotModel { Id = id, Name = name };

        _mockParkingLotRepository!.Setup(lotRepo => lotRepo.GetById<ParkingLotModel>(id)).ReturnsAsync(lotToDelete);
        _mockParkingLotRepository!.Setup(lotRepo => lotRepo.Delete(lotToDelete)).ReturnsAsync(true);

        // Act
        var result = await _parkingLotService!.DeleteParkingLot(id);

        // Assert
        Assert.IsInstanceOfType(result, typeof(DeleteLotResult.Success));
        _mockParkingLotRepository.Verify(lotRepo => lotRepo.GetById<ParkingLotModel>(id), Times.Once);
        _mockParkingLotRepository.Verify(lotRepo => lotRepo.Delete(lotToDelete), Times.Once);
    }

    [TestMethod]
    [DataRow(10000000)]
    [DataRow(99999999)]
    public async Task DeleteParkingLot_DeleteFails_ReturnsErrorResult(int id)
    {
        // Arrange
        var lotToDelete = new ParkingLotModel { Id = id, Name = "Lot X" };

        _mockParkingLotRepository!.Setup(lotRepo => lotRepo.GetById<ParkingLotModel>(id)).ReturnsAsync(lotToDelete);
        _mockParkingLotRepository!.Setup(lotRepo => lotRepo.Delete(lotToDelete)).ReturnsAsync(false);

        // Act
        var result = await _parkingLotService!.DeleteParkingLot(id);

        // Assert
        Assert.IsInstanceOfType(result, typeof(DeleteLotResult.Error));
        var errorResult = (DeleteLotResult.Error)result;
        Assert.AreEqual("Failed to delete the parking lot.", errorResult.Message);
        _mockParkingLotRepository.Verify(lotRepo => lotRepo.GetById<ParkingLotModel>(id), Times.Once);
        _mockParkingLotRepository.Verify(lotRepo => lotRepo.Delete(lotToDelete), Times.Once);
    }

    [TestMethod]
    [DataRow(50)]
    [DataRow(75)]
    public async Task DeleteParkingLot_NotFound_ReturnsNotFoundResult(int id)
    {
        // Arrange
        _mockParkingLotRepository!.Setup(lotRepo => lotRepo.GetById<ParkingLotModel>(id)).ReturnsAsync((ParkingLotModel?)null);

        // Act
        var result = await _parkingLotService!.DeleteParkingLot(id);

        // Assert
        Assert.IsInstanceOfType(result, typeof(DeleteLotResult.NotFound));
        _mockParkingLotRepository.Verify(lotRepo => lotRepo.Delete(It.IsAny<ParkingLotModel>()), Times.Never);
    }

    [TestMethod]
    public async Task DeleteParkingLot_RepositoryThrows_ReturnsErrorResult()
    {
        // Arrange
        int id = 1;
        var lotToDelete = new ParkingLotModel { Id = id };
        _mockParkingLotRepository!.Setup(lotRepo => lotRepo.GetById<ParkingLotModel>(id)).ReturnsAsync(lotToDelete);
        _mockParkingLotRepository!.Setup(lotRepo => lotRepo.Delete(It.IsAny<ParkingLotModel>())).ThrowsAsync(new DbUpdateConcurrencyException("Concurrency conflict")); // Db concurrency exception

        // Act
        var result = await _parkingLotService!.DeleteParkingLot(id);

        // Assert
        Assert.IsInstanceOfType(result, typeof(DeleteLotResult.Error));
        StringAssert.Contains(((DeleteLotResult.Error)result).Message, "Concurrency conflict");
    }

    [TestMethod]
    [DataRow("Valid Lot Name")]
    public async Task GetParkingLotByName_ValidName_ReturnsSuccessResult(string name)
    {
        // Arrange
        var expectedLot = new ParkingLotModel { Id = 1, Name = name, Address = "Some Address" };
        _mockParkingLotRepository!.Setup(lotRepo => lotRepo.GetByName(name)).ReturnsAsync(expectedLot);

        // Act
        var result = await _parkingLotService!.GetParkingLotByName(name);

        // Assert
        Assert.IsInstanceOfType(result, typeof(GetLotResult.Success));
        Assert.AreEqual(expectedLot.Name, ((GetLotResult.Success)result).Lot.Name);
        _mockParkingLotRepository.Verify(lotRepo => lotRepo.GetByName(name), Times.Once);
    }

    [TestMethod]
    [DataRow("NonExistent Name")]
    public async Task GetParkingLotByName_NotFound_ReturnsNotFoundResult(string name)
    {
        // Arrange
        _mockParkingLotRepository!.Setup(lotRepo => lotRepo.GetByName(name)).ReturnsAsync((ParkingLotModel?)null);

        // Act
        var result = await _parkingLotService!.GetParkingLotByName(name);

        // Assert
        Assert.IsInstanceOfType(result, typeof(GetLotResult.NotFound));
        _mockParkingLotRepository.Verify(lotRepo => lotRepo.GetByName(name), Times.Once);
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow(" ")]
    public async Task GetParkingLotByName_InvalidInput_ReturnsInvalidInputResult(string? name)
    {
        // Act
        var result = await _parkingLotService!.GetParkingLotByName(name!);

        // Assert
        Assert.IsInstanceOfType(result, typeof(GetLotResult.InvalidInput));
        _mockParkingLotRepository!.Verify(lotRepo => lotRepo.GetByName(It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    [DataRow("Test Location")]
    public async Task GetParkingLotsByLocation_ValidLocation_ReturnsSuccessResult(string location)
    {
        // Arrange
        var expectedLots = new List<ParkingLotModel> { new ParkingLotModel { Id = 1, Location = location } };
        _mockParkingLotRepository!.Setup(lotRepo => lotRepo.GetByLocation(location)).ReturnsAsync(expectedLots);

        // Act
        var result = await _parkingLotService!.GetParkingLotsByLocation(location);

        // Assert
        Assert.IsInstanceOfType(result, typeof(GetLotListResult.Success));
        Assert.AreEqual(1, ((GetLotListResult.Success)result).Lots.Count);
        _mockParkingLotRepository.Verify(lotRepo => lotRepo.GetByLocation(location), Times.Once);
    }

    [TestMethod]
    [DataRow("Unknown Location")]
    public async Task GetParkingLotsByLocation_NotFound_ReturnsNotFoundResult(string location)
    {
        // Arrange
        _mockParkingLotRepository!.Setup(lotRepo => lotRepo.GetByLocation(location)).ReturnsAsync(new List<ParkingLotModel>()); // Empty list

        // Act
        var result = await _parkingLotService!.GetParkingLotsByLocation(location);

        // Assert
        Assert.IsInstanceOfType(result, typeof(GetLotListResult.NotFound));
        _mockParkingLotRepository.Verify(lotRepo => lotRepo.GetByLocation(location), Times.Once);
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow(" ")]
    public async Task GetParkingLotsByLocation_InvalidInput_ReturnsInvalidInputResult(string? location)
    {
        // Act
        var result = await _parkingLotService!.GetParkingLotsByLocation(location!);

        // Assert
        Assert.IsInstanceOfType(result, typeof(GetLotListResult.InvalidInput));
        _mockParkingLotRepository!.Verify(lotRepo => lotRepo.GetByLocation(It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public async Task GetAllParkingLots_ReturnsAllLots()
    {
        // Arrange
        var lots = new List<ParkingLotModel>
        {
            new() { Id = 1, Name = "Lot A", Location = "City A", Address = "Street 1", Capacity = 100, Tariff = 2, DayTariff = 10 },
            new() { Id = 2, Name = "Lot B", Location = "City B", Address = "Street 2", Capacity = 200, Tariff = 3, DayTariff = 12 }
        };

        _mockParkingLotRepository!.Setup(access => access.GetAll()).ReturnsAsync(lots);

        // Act
        var result = await _parkingLotService!.GetAllParkingLots();

        // Assert
        Assert.IsInstanceOfType(result, typeof(GetLotListResult.Success));
        var successResult = (GetLotListResult.Success)result;
        Assert.AreEqual(2, successResult.Lots.Count);
        _mockParkingLotRepository.Verify(lotRepo => lotRepo.GetAll(), Times.Once);
    }

    [TestMethod]
    public async Task GetAllParkingLots_NoLots_ReturnsNotFoundResult()
    {
        // Arrange
        _mockParkingLotRepository!.Setup(lotRepo => lotRepo.GetAll()).ReturnsAsync(new List<ParkingLotModel>());

        // Act
        var result = await _parkingLotService!.GetAllParkingLots();

        // Assert
        Assert.IsInstanceOfType(result, typeof(GetLotListResult.NotFound));
        _mockParkingLotRepository.Verify(lotRepo => lotRepo.GetAll(), Times.Once);
    }

    [TestMethod]
    [DataRow("id", "123")]
    [DataRow("address", "123 Main St")]
    public async Task ParkingLotExists_WhenExists_ReturnsExistsResult(string checkBy, string value)
    {
        // Arrange
        _mockParkingLotRepository!.Setup(lotRepo => lotRepo.Exists(It.IsAny<Expression<Func<ParkingLotModel, bool>>>())).ReturnsAsync(true);

        // Act
        var result = await _parkingLotService!.ParkingLotExists(checkBy, value);

        // Assert
        Assert.IsInstanceOfType(result, typeof(ParkingLotExistsResult.Exists));
        _mockParkingLotRepository.Verify(lotRepo => lotRepo.Exists(It.IsAny<Expression<Func<ParkingLotModel, bool>>>()), Times.Once);
    }

    [TestMethod]
    [DataRow("id", "456")]
    [DataRow("address", "Unknown Address")]
    public async Task ParkingLotExists_WhenNotExists_ReturnsNotExistsResult(string checkBy, string value)
    {
        // Arrange
        _mockParkingLotRepository!.Setup(lotRepo => lotRepo.Exists(It.IsAny<Expression<Func<ParkingLotModel, bool>>>())).ReturnsAsync(false);

        // Act
        var result = await _parkingLotService!.ParkingLotExists(checkBy, value);

        // Assert
        Assert.IsInstanceOfType(result, typeof(ParkingLotExistsResult.NotExists)); // Directly assert NotExists
        _mockParkingLotRepository.Verify(lotRepo => lotRepo.Exists(It.IsAny<Expression<Func<ParkingLotModel, bool>>>()), Times.Once);
    }

    [TestMethod]
    [DataRow("id", "abc")]
    [DataRow("name", "Some Name")]
    [DataRow("address", " ")]
    [DataRow("id", "")]
    [DataRow("id", null)]
    public async Task ParkingLotExists_InvalidInput_ReturnsInvalidInputResult(string checkBy, string? value)
    {
        // Act
        var result = await _parkingLotService!.ParkingLotExists(checkBy, value!);

        // Assert
        Assert.IsInstanceOfType(result, typeof(ParkingLotExistsResult.InvalidInput));
         _mockParkingLotRepository!.Verify(lotRepo => lotRepo.Exists(It.IsAny<Expression<Func<ParkingLotModel, bool>>>()), Times.Never);
    }

    [TestMethod]
    [DataRow("id", "9999999999")]
    [DataRow("address", "Error Address")]
    public async Task ParkingLotExists_RepositoryThrows_ReturnsErrorResult(string checkBy, string value)
    {
        // Arrange
        _mockParkingLotRepository!.Setup(lotRepo => lotRepo.Exists(It.IsAny<Expression<Func<ParkingLotModel, bool>>>())).ThrowsAsync(new DbUpdateConcurrencyException("Concurrency conflict"));

        // Act
        var result = await _parkingLotService!.ParkingLotExists(checkBy, value);

        // Assert
        Assert.IsInstanceOfType(result, typeof(ParkingLotExistsResult.Error));
        StringAssert.Contains(((ParkingLotExistsResult.Error)result).Message, "Concurrency conflict");
    }

    [TestMethod]
    [DataRow(0)]
    [DataRow(5)]
    [DataRow(123)]
    public async Task CountParkingLots_ReturnsCountFromRepository(int expectedCount)
    {
        // Arrange
        _mockParkingLotRepository!.Setup(lotRepo => lotRepo.Count()).ReturnsAsync(expectedCount);

        // Act
        var result = await _parkingLotService!.CountParkingLots();

        // Assert
        Assert.AreEqual(expectedCount, result);
        _mockParkingLotRepository.Verify(lotRepo => lotRepo.Count(), Times.Once);
    }
}
