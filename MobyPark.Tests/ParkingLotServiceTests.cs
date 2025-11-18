using System.Linq.Expressions;
using Moq;
using MobyPark.DTOs.ParkingLot.Request;
using MobyPark.Models;
using MobyPark.Models.Repositories.Interfaces;
using MobyPark.Services;
using MobyPark.Services.Results;

namespace MobyPark.Tests;

[TestClass]
public class ParkingLotServiceTests
{
    private Mock<IRepository<ParkingLotModel>> _parkingRepoMock = null!;
    private ParkingLotService _service = null!;

    [TestInitialize]
    public void Setup()
    {
        _parkingRepoMock = new Mock<IRepository<ParkingLotModel>>();
        _service = new ParkingLotService(_parkingRepoMock.Object);
    }

    #region GetParkingLotByAddressAsync

    [TestMethod]
    public async Task GetParkingLotByAddressAsync_LotExists_ReturnsOkWithDto()
    {
        // Arrange
        var address = "Kenna Street 1";

        var lot = new ParkingLotModel
        {
            Id = 1,
            Name = "Lot A",
            Location = "Location A",
            Address = "Kenna Street 1",
            Capacity = 100,
            Tariff = 5m,
            DayTariff = 20m
        };

        _parkingRepoMock
            .Setup(r => r.GetByAsync(It.IsAny<Expression<Func<ParkingLotModel, bool>>>()))
            .ReturnsAsync(new[] { lot }.ToList());

        // Act
        var result = await _service.GetParkingLotByAddressAsync(address);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(ServiceStatus.Success, result.Status);
        Assert.IsNotNull(result.Data);
        Assert.AreEqual(lot.Id, result.Data!.Id);
        Assert.AreEqual(lot.Name, result.Data.Name);
        Assert.AreEqual(lot.Address, result.Data.Address);

        _parkingRepoMock.Verify(r => r.GetByAsync(It.IsAny<Expression<Func<ParkingLotModel, bool>>>()), Times.Once);
    }

    [TestMethod]
    public async Task GetParkingLotByAddressAsync_LotDoesNotExist_ReturnsNotFound()
    {
        // Arrange
        _parkingRepoMock
            .Setup(r => r.GetByAsync(It.IsAny<Expression<Func<ParkingLotModel, bool>>>()))
            .ReturnsAsync(Enumerable.Empty<ParkingLotModel>().ToList());

        // Act
        var result = await _service.GetParkingLotByAddressAsync("Unknown");

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(ServiceStatus.NotFound, result.Status);
        Assert.IsNull(result.Data);
        Assert.AreEqual("No parking lot found at address Unknown", result.Error);

        _parkingRepoMock.Verify(r => r.GetByAsync(It.IsAny<Expression<Func<ParkingLotModel, bool>>>()), Times.Once);
    }

    #endregion

    #region GetParkingLotByIdAsync

    [TestMethod]
    public async Task GetParkingLotByIdAsync_LotExists_ReturnsOkWithDto()
    {
        // Arrange
        var lot = new ParkingLotModel
        {
            Id = 42,
            Name = "Lot 42",
            Location = "Loc",
            Address = "Addr",
            Capacity = 123,
            Tariff = 7m,
            DayTariff = 30m
        };

        _parkingRepoMock
            .Setup(r => r.FindByIdAsync(lot.Id))
            .ReturnsAsync(lot);

        // Act
        var result = await _service.GetParkingLotByIdAsync(lot.Id);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(ServiceStatus.Success, result.Status);
        Assert.IsNotNull(result.Data);
        Assert.AreEqual(lot.Id, result.Data!.Id);
        Assert.AreEqual(lot.Name, result.Data.Name);
        Assert.AreEqual(lot.Address, result.Data.Address);

        _parkingRepoMock.Verify(r => r.FindByIdAsync(lot.Id), Times.Once);
    }

    [TestMethod]
    public async Task GetParkingLotByIdAsync_LotDoesNotExist_ReturnsNotFound()
    {
        // Arrange
        _parkingRepoMock
            .Setup(r => r.FindByIdAsync(It.IsAny<int>()))
            .ReturnsAsync((ParkingLotModel?)null);

        // Act
        var result = await _service.GetParkingLotByIdAsync(999);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(ServiceStatus.NotFound, result.Status);
        Assert.AreEqual($"No lot with id: {999} found.", result.Error);
        Assert.IsNull(result.Data);
    }

    #endregion

    #region CreateParkingLotAsync

    [TestMethod]
    public async Task CreateParkingLotAsync_AddressFree_CreatesAndReturnsOk()
    {
        // Arrange
        var dto = new CreateParkingLotDto
        {
            Name = "Lot A",
            Location = "Location A",
            Address = "Address A",
            Capacity = 100,
            Tariff = 5m,
            DayTariff = 20m
        };

        _parkingRepoMock
            .Setup(r => r.GetByAsync(It.IsAny<Expression<Func<ParkingLotModel, bool>>>()))
            .ReturnsAsync(Enumerable.Empty<ParkingLotModel>().ToList()); // no existing lot

        ParkingLotModel? addedLot = null;

        _parkingRepoMock
            .Setup(r => r.Add(It.IsAny<ParkingLotModel>()))
            .Callback<ParkingLotModel>(lot => addedLot = lot);

        _parkingRepoMock
            .Setup(r => r.SaveChangesAsync())
            .ReturnsAsync(1);

        // Act
        var result = await _service.CreateParkingLotAsync(dto);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(ServiceStatus.Success, result.Status);
        Assert.IsNotNull(result.Data);

        Assert.IsNotNull(addedLot);
        Assert.AreEqual(dto.Name, addedLot!.Name);
        Assert.AreEqual(dto.Address, addedLot.Address);
        Assert.AreEqual(dto.Capacity, addedLot.Capacity);
        Assert.AreEqual(dto.Tariff, addedLot.Tariff);
        Assert.AreEqual(dto.DayTariff, addedLot.DayTariff);

        Assert.AreEqual(addedLot.Id, result.Data!.Id);
        Assert.AreEqual(dto.Name, result.Data.Name);

        _parkingRepoMock.Verify(r => r.Add(It.IsAny<ParkingLotModel>()), Times.Once);
        _parkingRepoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [TestMethod]
    public async Task CreateParkingLotAsync_AddressTaken_ReturnsFail()
    {
        // Arrange
        var dto = new CreateParkingLotDto
        {
            Name = "Lot A",
            Location = "Location A",
            Address = "Address A",
            Capacity = 100,
            Tariff = 5m,
            DayTariff = 20m
        };

        var existing = new ParkingLotModel { Id = 1, Address = dto.Address };

        _parkingRepoMock
            .Setup(r => r.GetByAsync(It.IsAny<Expression<Func<ParkingLotModel, bool>>>()))
            .ReturnsAsync(new[] { existing }.ToList());

        // Act
        var result = await _service.CreateParkingLotAsync(dto);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(ServiceStatus.Fail, result.Status);
        Assert.AreEqual("Address taken", result.Error);
        Assert.IsNull(result.Data);

        _parkingRepoMock.Verify(r => r.Add(It.IsAny<ParkingLotModel>()), Times.Never);
        _parkingRepoMock.Verify(r => r.SaveChangesAsync(), Times.Never);
    }

    #endregion

    #region PatchParkingLotByAddressAsync

    [TestMethod]
    public async Task PatchParkingLotByAddressAsync_LotNotFound_ReturnsNotFound()
    {
        // Arrange
        _parkingRepoMock
            .Setup(r => r.GetByAsync(It.IsAny<Expression<Func<ParkingLotModel, bool>>>()))
            .ReturnsAsync(Enumerable.Empty<ParkingLotModel>().ToList());

        var patchDto = new PatchParkingLotDto { Name = "New Name" };

        // Act
        var result = await _service.PatchParkingLotByAddressAsync("Unknown", patchDto);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(ServiceStatus.NotFound, result.Status);
        Assert.AreEqual("No parking lot was found with that address", result.Error);

        _parkingRepoMock.Verify(r => r.Update(It.IsAny<ParkingLotModel>()), Times.Never);
        _parkingRepoMock.Verify(r => r.SaveChangesAsync(), Times.Never);
    }

    [TestMethod]
    public async Task PatchParkingLotByAddressAsync_ValidUpdate_UpdatesAndReturnsOk()
    {
        // Arrange
        var existing = new ParkingLotModel
        {
            Id = 1,
            Name = "Old",
            Location = "OldLoc",
            Address = "Address A",
            Capacity = 10,
            Tariff = 5m,
            DayTariff = 20m
        };

        _parkingRepoMock
            .SetupSequence(r => r.GetByAsync(It.IsAny<Expression<Func<ParkingLotModel, bool>>>()))
            .ReturnsAsync(new[] { existing }.ToList())    // by current address
            .ReturnsAsync(Enumerable.Empty<ParkingLotModel>().ToList()); // new address free

        var patchDto = new PatchParkingLotDto
        {
            Name = "New",
            Location = "NewLoc",
            Address = "New Address A",
            Capacity = 100,
            Tariff = 7m,
            DayTariff = 30m
        };

        ParkingLotModel? updatedLot = null;
        _parkingRepoMock
            .Setup(r => r.Update(It.IsAny<ParkingLotModel>()))
            .Callback<ParkingLotModel>(lot => updatedLot = lot);

        _parkingRepoMock
            .Setup(r => r.SaveChangesAsync())
            .ReturnsAsync(1);

        // Act
        var result = await _service.PatchParkingLotByAddressAsync(existing.Address, patchDto);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(ServiceStatus.Success, result.Status);
        Assert.IsNotNull(result.Data);

        Assert.IsNotNull(updatedLot);
        Assert.AreEqual(patchDto.Name, updatedLot!.Name);
        Assert.AreEqual(patchDto.Location, updatedLot.Location);
        Assert.AreEqual(patchDto.Address, updatedLot.Address);
        Assert.AreEqual(patchDto.Capacity, updatedLot.Capacity);
        Assert.AreEqual(patchDto.Tariff, updatedLot.Tariff);
        Assert.AreEqual(patchDto.DayTariff, updatedLot.DayTariff);

        _parkingRepoMock.Verify(r => r.Update(It.IsAny<ParkingLotModel>()), Times.Once);
        _parkingRepoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [TestMethod]
    public async Task PatchParkingLotByAddressAsync_NewAddressTaken_ReturnsBadRequest()
    {
        // Arrange
        var existing = new ParkingLotModel { Id = 1, Address = "Address A" };
        var other = new ParkingLotModel { Id = 2, Address = "New Address" };

        _parkingRepoMock
            .SetupSequence(r => r.GetByAsync(It.IsAny<Expression<Func<ParkingLotModel, bool>>>()))
            .ReturnsAsync(new[] { existing }.ToList()) // by old address
            .ReturnsAsync(new[] { other }.ToList());   // new address taken

        var patchDto = new PatchParkingLotDto { Address = "New Address" };

        // Act
        var result = await _service.PatchParkingLotByAddressAsync(existing.Address, patchDto);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(ServiceStatus.BadRequest, result.Status);
        Assert.AreEqual("There is already a parking lot assigned to the new address.", result.Error);

        _parkingRepoMock.Verify(r => r.Update(It.IsAny<ParkingLotModel>()), Times.Never);
        _parkingRepoMock.Verify(r => r.SaveChangesAsync(), Times.Never);
    }

    #endregion

    #region PatchParkingLotByIdAsync

    [TestMethod]
    public async Task PatchParkingLotByIdAsync_LotNotFound_ReturnsNotFound()
    {
        // Arrange
        _parkingRepoMock
            .Setup(r => r.FindByIdAsync(It.IsAny<int>()))
            .ReturnsAsync((ParkingLotModel?)null);

        var patchDto = new PatchParkingLotDto { Name = "New Name" };

        // Act
        var result = await _service.PatchParkingLotByIdAsync(999, patchDto);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(ServiceStatus.NotFound, result.Status);
        Assert.AreEqual("No parking lot was found with that id", result.Error);
    }

    [TestMethod]
    public async Task PatchParkingLotByIdAsync_ValidUpdate_UpdatesAndReturnsOk()
    {
        // Arrange
        var existing = new ParkingLotModel
        {
            Id = 1,
            Name = "Old",
            Location = "OldLoc",
            Address = "Addr",
            Capacity = 10,
            Tariff = 5m,
            DayTariff = 20m
        };

        _parkingRepoMock
            .Setup(r => r.FindByIdAsync(existing.Id))
            .ReturnsAsync(existing);

        _parkingRepoMock
            .Setup(r => r.GetByAsync(It.IsAny<Expression<Func<ParkingLotModel, bool>>>()))
            .ReturnsAsync(Enumerable.Empty<ParkingLotModel>().ToList()); // new address free if changed

        var patchDto = new PatchParkingLotDto
        {
            Name = "New",
            Capacity = 100
        };

        ParkingLotModel? updatedLot = null;
        _parkingRepoMock
            .Setup(r => r.Update(It.IsAny<ParkingLotModel>()))
            .Callback<ParkingLotModel>(lot => updatedLot = lot);

        _parkingRepoMock
            .Setup(r => r.SaveChangesAsync())
            .ReturnsAsync(1);

        // Act
        var result = await _service.PatchParkingLotByIdAsync(existing.Id, patchDto);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(ServiceStatus.Success, result.Status);
        Assert.IsNotNull(result.Data);

        Assert.IsNotNull(updatedLot);
        Assert.AreEqual(patchDto.Name, updatedLot!.Name);
        Assert.AreEqual(patchDto.Capacity, updatedLot.Capacity);

        _parkingRepoMock.Verify(r => r.Update(It.IsAny<ParkingLotModel>()), Times.Once);
        _parkingRepoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    #endregion

    #region DeleteParkingLotByIdAsync

    [TestMethod]
    public async Task DeleteParkingLotByIdAsync_LotExists_DeletesAndReturnsOk()
    {
        // Arrange
        var existing = new ParkingLotModel { Id = 1 };

        _parkingRepoMock
            .Setup(r => r.FindByIdAsync(existing.Id))
            .ReturnsAsync(existing);

        _parkingRepoMock
            .Setup(r => r.SaveChangesAsync())
            .ReturnsAsync(1);

        // Act
        var result = await _service.DeleteParkingLotByIdAsync(existing.Id);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(ServiceStatus.Success, result.Status);
        Assert.IsTrue(result.Data);

        _parkingRepoMock.Verify(r => r.Deletee(existing), Times.Once);
        _parkingRepoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [TestMethod]
    public async Task DeleteParkingLotByIdAsync_LotNotFound_ReturnsNotFound()
    {
        // Arrange
        _parkingRepoMock
            .Setup(r => r.FindByIdAsync(It.IsAny<int>()))
            .ReturnsAsync((ParkingLotModel?)null);

        // Act
        var result = await _service.DeleteParkingLotByIdAsync(999);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(ServiceStatus.NotFound, result.Status);
        Assert.AreEqual("No lot found with that id. Deletion failed.", result.Error);
        // Data will be default(bool) == false, but it's not very important to assert here.

        _parkingRepoMock.Verify(r => r.Deletee(It.IsAny<ParkingLotModel>()), Times.Never);
        _parkingRepoMock.Verify(r => r.SaveChangesAsync(), Times.Never);
    }

    #endregion

    #region DeleteParkingLotByAddressAsync

    [TestMethod]
    public async Task DeleteParkingLotByAddressAsync_LotExists_DeletesAndReturnsOk()
    {
        // Arrange
        var existing = new ParkingLotModel { Id = 1, Address = "Addr" };

        _parkingRepoMock
            .Setup(r => r.GetByAsync(It.IsAny<Expression<Func<ParkingLotModel, bool>>>()))
            .ReturnsAsync(new[] { existing }.ToList());

        _parkingRepoMock
            .Setup(r => r.SaveChangesAsync())
            .ReturnsAsync(1);

        // Act
        var result = await _service.DeleteParkingLotByAddressAsync(existing.Address);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(ServiceStatus.Success, result.Status);
        Assert.IsTrue(result.Data);

        _parkingRepoMock.Verify(r => r.Deletee(existing), Times.Once);
        _parkingRepoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [TestMethod]
    public async Task DeleteParkingLotByAddressAsync_LotNotFound_ReturnsNotFound()
    {
        // Arrange
        _parkingRepoMock
            .Setup(r => r.GetByAsync(It.IsAny<Expression<Func<ParkingLotModel, bool>>>()))
            .ReturnsAsync(Enumerable.Empty<ParkingLotModel>().ToList());

        // Act
        var result = await _service.DeleteParkingLotByAddressAsync("Unknown");

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(ServiceStatus.NotFound, result.Status);
        Assert.AreEqual("No lot found with that address. Deletion failed.", result.Error);

        _parkingRepoMock.Verify(r => r.Deletee(It.IsAny<ParkingLotModel>()), Times.Never);
        _parkingRepoMock.Verify(r => r.SaveChangesAsync(), Times.Never);
    }

    #endregion

    #region GetAllParkingLotsAsync

    [TestMethod]
    public async Task GetAllParkingLotsAsync_ReturnsMappedList()
    {
        // Arrange
        var lots = new[]
        {
            new ParkingLotModel { Id = 1, Name = "Lot1", Location = "L1", Address = "A1", Capacity = 10, Tariff = 5m, DayTariff = 20m },
            new ParkingLotModel { Id = 2, Name = "Lot2", Location = "L2", Address = "A2", Capacity = 20, Tariff = 6m, DayTariff = 25m }
        }.ToList();

        _parkingRepoMock
            .Setup(r => r.ReadAllAsync())
            .ReturnsAsync(lots);

        // Act
        var result = await _service.GetAllParkingLotsAsync();

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(ServiceStatus.Success, result.Status);
        Assert.IsNotNull(result.Data);
        Assert.AreEqual(2, result.Data!.Count);
        Assert.AreEqual(lots[0].Id, result.Data[0].Id);
        Assert.AreEqual(lots[1].Id, result.Data[1].Id);

        _parkingRepoMock.Verify(r => r.ReadAllAsync(), Times.Once);
    }

    #endregion
}