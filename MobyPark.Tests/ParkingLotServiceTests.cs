using System.Linq.Expressions;

using MobyPark.DTOs.ParkingLot.Request;
using MobyPark.Models;
using MobyPark.Models.Repositories.Interfaces;
using MobyPark.Services;
using MobyPark.Services.Interfaces;
using MobyPark.Services.Results;

using Moq;

namespace MobyPark.Tests;

[TestClass]
public class ParkingLotServiceTests
{
    private Mock<IRepository<ParkingLotModel>> _parkingRepoMock = null!;
    private Mock<IRepository<ParkingSessionModel>> _sessionRepoMock = null!;
    private Mock<IRepository<ReservationModel>> _reservationRepoMock = null!;
    private Mock<IRepository<HotelPassModel>> _hotelRepoMock = null!;
    private IParkingLotService _service = null!;

    [TestInitialize]
    public void Setup()
    {
        _parkingRepoMock = new Mock<IRepository<ParkingLotModel>>();
        _sessionRepoMock = new Mock<IRepository<ParkingSessionModel>>();
        _reservationRepoMock = new Mock<IRepository<ReservationModel>>();
        _hotelRepoMock = new Mock<IRepository<HotelPassModel>>();

        _service = new ParkingLotService(
            _parkingRepoMock.Object,
            _sessionRepoMock.Object,
            _reservationRepoMock.Object,
            _hotelRepoMock.Object);
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
    public async Task GetParkingLotByAddressAsync_RepoThrows_ReturnsException()
    {
        _parkingRepoMock
            .Setup(r => r.GetByAsync(It.IsAny<Expression<Func<ParkingLotModel, bool>>>()))
            .ThrowsAsync(new Exception("db"));

        var result = await _service.GetParkingLotByAddressAsync("Addr");

        Assert.AreEqual(ServiceStatus.Exception, result.Status);
        Assert.IsNull(result.Data);
        Assert.IsTrue(result.Error?.Contains("Unexpected error occurred") ?? false);
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
    public async Task GetParkingLotByIdAsync_RepoThrows_ReturnsFail()
    {
        _parkingRepoMock
            .Setup(r => r.FindByIdAsync(It.IsAny<long>()))
            .ThrowsAsync(new Exception("db"));

        var result = await _service.GetParkingLotByIdAsync(1);

        Assert.AreEqual(ServiceStatus.Fail, result.Status);
        Assert.IsNull(result.Data);
        Assert.IsTrue(result.Error?.Contains("Unexpected error occurred") ?? false);
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
    
    [TestMethod]
    public async Task CreateParkingLotAsync_SetsReservedToZero_ReturnsOk()
    {
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
            .ReturnsAsync(Enumerable.Empty<ParkingLotModel>().ToList());

        ParkingLotModel captured = null!;
        _parkingRepoMock
            .Setup(r => r.Add(It.IsAny<ParkingLotModel>()))
            .Callback<ParkingLotModel>(x => captured = x);

        _parkingRepoMock
            .Setup(r => r.SaveChangesAsync())
            .ReturnsAsync(1);

        var result = await _service.CreateParkingLotAsync(dto);

        Assert.AreEqual(ServiceStatus.Success, result.Status);
        Assert.IsNotNull(result.Data);
        Assert.IsNotNull(captured);
        Assert.AreEqual(0, captured.Reserved);
        Assert.AreEqual(0, result.Data!.Reserved);
    }

    [TestMethod]
    public async Task CreateParkingLotAsync_AddressNormalized_ReturnsFailWhenTaken()
    {
        var dto = new CreateParkingLotDto
        {
            Name = "Lot A",
            Location = "Location A",
            Address = "  ADDRESS A  ",
            Capacity = 100,
            Tariff = 5m,
            DayTariff = 20m
        };

        _parkingRepoMock
            .Setup(r => r.GetByAsync(It.IsAny<Expression<Func<ParkingLotModel, bool>>>()))
            .ReturnsAsync(new[] { new ParkingLotModel { Id = 1, Address = "address a" } }.ToList());

        var result = await _service.CreateParkingLotAsync(dto);

        Assert.AreEqual(ServiceStatus.Fail, result.Status);
        Assert.IsNull(result.Data);
        Assert.AreEqual("Address taken", result.Error);

        _parkingRepoMock.Verify(r => r.Add(It.IsAny<ParkingLotModel>()), Times.Never);
        _parkingRepoMock.Verify(r => r.SaveChangesAsync(), Times.Never);
    }

    [TestMethod]
    public async Task CreateParkingLotAsync_RepoThrows_ReturnsFail()
    {
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
            .ThrowsAsync(new Exception("db"));

        var result = await _service.CreateParkingLotAsync(dto);

        Assert.AreEqual(ServiceStatus.Fail, result.Status);
        Assert.IsNull(result.Data);
        Assert.IsTrue(result.Error?.Contains("Unexpected error occurred") ?? false);

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
    
    [TestMethod]
    public async Task PatchParkingLotByAddressAsync_PatchOnlyName_UpdatesAndReturnsOk()
    {
        var existing = new ParkingLotModel
        {
            Id = 1,
            Name = "Old",
            Location = "OldLoc",
            Address = "Address A",
            Capacity = 10,
            Tariff = 5m,
            DayTariff = 20m,
            Reserved = 3
        };

        _parkingRepoMock
            .Setup(r => r.GetByAsync(It.IsAny<Expression<Func<ParkingLotModel, bool>>>()))
            .ReturnsAsync(new[] { existing }.ToList());

        var patchDto = new PatchParkingLotDto { Name = "New" };

        ParkingLotModel updatedLot = null!;
        _parkingRepoMock
            .Setup(r => r.Update(It.IsAny<ParkingLotModel>()))
            .Callback<ParkingLotModel>(x => updatedLot = x);

        _parkingRepoMock
            .Setup(r => r.SaveChangesAsync())
            .ReturnsAsync(1);

        var result = await _service.PatchParkingLotByAddressAsync(existing.Address, patchDto);

        Assert.AreEqual(ServiceStatus.Success, result.Status);
        Assert.IsNotNull(result.Data);
        Assert.IsNotNull(updatedLot);
        Assert.AreEqual("New", updatedLot.Name);
        Assert.AreEqual(existing.Location, updatedLot.Location);
        Assert.AreEqual(existing.Address, updatedLot.Address);
        Assert.AreEqual(existing.Reserved, updatedLot.Reserved);
    }

    [TestMethod]
    public async Task PatchParkingLotByAddressAsync_AddressNormalized_FindsLot_ReturnsOk()
    {
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
            .Setup(r => r.GetByAsync(It.IsAny<Expression<Func<ParkingLotModel, bool>>>()))
            .ReturnsAsync(new[] { existing }.ToList());

        var patchDto = new PatchParkingLotDto { Location = "NewLoc" };

        _parkingRepoMock
            .Setup(r => r.SaveChangesAsync())
            .ReturnsAsync(1);

        var result = await _service.PatchParkingLotByAddressAsync("  ADDRESS A  ", patchDto);

        Assert.AreEqual(ServiceStatus.Success, result.Status);
        Assert.IsNotNull(result.Data);
        Assert.AreEqual("NewLoc", result.Data!.Location);
    }

    [TestMethod]
    public async Task PatchParkingLotByAddressAsync_NewAddressSameAsCurrent_DoesNotFail()
    {
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
            .ReturnsAsync(new[] { existing }.ToList())
            .ReturnsAsync(Enumerable.Empty<ParkingLotModel>().ToList());

        var patchDto = new PatchParkingLotDto { Address = "  Address A  " };

        _parkingRepoMock
            .Setup(r => r.SaveChangesAsync())
            .ReturnsAsync(1);

        var result = await _service.PatchParkingLotByAddressAsync(existing.Address, patchDto);

        Assert.AreEqual(ServiceStatus.Success, result.Status);
        Assert.IsNotNull(result.Data);
        Assert.AreEqual("  Address A  ", result.Data!.Address);
    }

    [TestMethod]
    public async Task PatchParkingLotByAddressAsync_RepoThrows_ReturnsException()
    {
        _parkingRepoMock
            .Setup(r => r.GetByAsync(It.IsAny<Expression<Func<ParkingLotModel, bool>>>()))
            .ThrowsAsync(new Exception("db"));

        var result = await _service.PatchParkingLotByAddressAsync("Addr", new PatchParkingLotDto { Name = "New" });

        Assert.AreEqual(ServiceStatus.Exception, result.Status);
        Assert.IsNull(result.Data);
        Assert.IsTrue(result.Error?.Contains("Unexpected error occurred") ?? false);
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
    
    [TestMethod]
    public async Task PatchParkingLotByIdAsync_NewAddressTaken_ReturnsBadRequest()
    {
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

        var other = new ParkingLotModel { Id = 2, Address = "New Address" };

        _parkingRepoMock
            .Setup(r => r.FindByIdAsync(existing.Id))
            .ReturnsAsync(existing);

        _parkingRepoMock
            .Setup(r => r.GetByAsync(It.IsAny<Expression<Func<ParkingLotModel, bool>>>()))
            .ReturnsAsync(new[] { other }.ToList());

        var patchDto = new PatchParkingLotDto { Address = "New Address" };

        var result = await _service.PatchParkingLotByIdAsync(existing.Id, patchDto);

        Assert.AreEqual(ServiceStatus.BadRequest, result.Status);
        Assert.AreEqual("There is already a parking lot assigned to the new address.", result.Error);

        _parkingRepoMock.Verify(r => r.Update(It.IsAny<ParkingLotModel>()), Times.Never);
        _parkingRepoMock.Verify(r => r.SaveChangesAsync(), Times.Never);
    }

    [TestMethod]
    public async Task PatchParkingLotByIdAsync_RepoThrows_ReturnsException()
    {
        _parkingRepoMock
            .Setup(r => r.FindByIdAsync(It.IsAny<long>()))
            .ThrowsAsync(new Exception("db"));

        var result = await _service.PatchParkingLotByIdAsync(1, new PatchParkingLotDto { Name = "New" });

        Assert.AreEqual(ServiceStatus.Exception, result.Status);
        Assert.IsNull(result.Data);
        Assert.IsTrue(result.Error?.Contains("Unexpected error occurred") ?? false);
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
    
    [TestMethod]
    public async Task DeleteParkingLotByIdAsync_RepoThrows_ReturnsFail()
    {
        _parkingRepoMock
            .Setup(r => r.FindByIdAsync(It.IsAny<long>()))
            .ThrowsAsync(new Exception("db"));

        var result = await _service.DeleteParkingLotByIdAsync(1);

        Assert.AreEqual(ServiceStatus.Fail, result.Status);
        Assert.IsFalse(result.Data);
        Assert.IsTrue(result.Error?.Contains("Unexpected error occurred") ?? false);

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
    
    [TestMethod]
    public async Task DeleteParkingLotByAddressAsync_AddressNormalized_FindsLot_DeletesAndReturnsOk()
    {
        var existing = new ParkingLotModel { Id = 1, Address = "Addr" };

        _parkingRepoMock
            .Setup(r => r.GetByAsync(It.IsAny<Expression<Func<ParkingLotModel, bool>>>()))
            .ReturnsAsync(new[] { existing }.ToList());

        _parkingRepoMock
            .Setup(r => r.SaveChangesAsync())
            .ReturnsAsync(1);

        var result = await _service.DeleteParkingLotByAddressAsync("  ADDR  ");

        Assert.AreEqual(ServiceStatus.Success, result.Status);
        Assert.IsTrue(result.Data);

        _parkingRepoMock.Verify(r => r.Deletee(existing), Times.Once);
        _parkingRepoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [TestMethod]
    public async Task DeleteParkingLotByAddressAsync_RepoThrows_ReturnsFail()
    {
        _parkingRepoMock
            .Setup(r => r.GetByAsync(It.IsAny<Expression<Func<ParkingLotModel, bool>>>()))
            .ThrowsAsync(new Exception("db"));

        var result = await _service.DeleteParkingLotByAddressAsync("Addr");

        Assert.AreEqual(ServiceStatus.Fail, result.Status);
        Assert.IsFalse(result.Data);
        Assert.IsTrue(result.Error?.Contains("Unexpected error occurred") ?? false);

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
    
    [TestMethod]
    public async Task GetAllParkingLotsAsync_RepoThrows_ReturnsFail()
    {
        _parkingRepoMock
            .Setup(r => r.ReadAllAsync())
            .ThrowsAsync(new Exception("db"));

        var result = await _service.GetAllParkingLotsAsync();

        Assert.AreEqual(ServiceStatus.Fail, result.Status);
        Assert.IsNull(result.Data);
        Assert.IsTrue(result.Error?.Contains("Unexpected error occurred") ?? false);
    }

    [TestMethod]
    public async Task GetAvailableSpotsByLotIdAsync_LotNotFound_ReturnsNotFound()
    {
        _parkingRepoMock
            .Setup(r => r.FindByIdAsync(It.IsAny<long>()))
            .ReturnsAsync((ParkingLotModel)null!);

        var result = await _service.GetAvailableSpotsByLotIdAsync(1);

        Assert.AreEqual(ServiceStatus.NotFound, result.Status);
        Assert.AreEqual("Parking lot not found", result.Error);
    }

    [TestMethod]
    public async Task GetAvailableSpotsByLotIdAsync_NoOccupancy_ReturnsCapacity()
    {
        var lot = new ParkingLotModel { Id = 1, Capacity = 10, Address = "Addr" };

        _parkingRepoMock
            .Setup(r => r.FindByIdAsync(lot.Id))
            .ReturnsAsync(lot);

        _sessionRepoMock
            .Setup(r => r.GetByAsync(It.IsAny<Expression<Func<ParkingSessionModel, bool>>>()))
            .ReturnsAsync(new List<ParkingSessionModel>());

        _reservationRepoMock
            .Setup(r => r.GetByAsync(It.IsAny<Expression<Func<ReservationModel, bool>>>()))
            .ReturnsAsync(new List<ReservationModel>());

        _hotelRepoMock
            .Setup(r => r.GetByAsync(It.IsAny<Expression<Func<HotelPassModel, bool>>>()))
            .ReturnsAsync(new List<HotelPassModel>());

        var result = await _service.GetAvailableSpotsByLotIdAsync(lot.Id);

        Assert.AreEqual(ServiceStatus.Success, result.Status);
        Assert.AreEqual(10, result.Data);
    }

    [TestMethod]
    public async Task GetAvailableSpotsByLotIdAsync_OccupancyExceedsCapacity_ReturnsZero()
    {
        var lot = new ParkingLotModel { Id = 1, Capacity = 2, Address = "Addr" };

        _parkingRepoMock
            .Setup(r => r.FindByIdAsync(lot.Id))
            .ReturnsAsync(lot);

        _sessionRepoMock
            .Setup(r => r.GetByAsync(It.IsAny<Expression<Func<ParkingSessionModel, bool>>>()))
            .ReturnsAsync(new List<ParkingSessionModel>
            {
                new ParkingSessionModel { Id = 1, ParkingLotId = 1, Started = DateTime.UtcNow.AddHours(-1), Stopped = null },
                new ParkingSessionModel { Id = 2, ParkingLotId = 1, Started = DateTime.UtcNow.AddHours(-2), Stopped = null }
            });

        _reservationRepoMock
            .Setup(r => r.GetByAsync(It.IsAny<Expression<Func<ReservationModel, bool>>>()))
            .ReturnsAsync(new List<ReservationModel>
            {
                new ReservationModel { Id = 1, ParkingLotId = 1, Status = ReservationStatus.Confirmed, StartTime = DateTime.UtcNow.AddHours(-1), EndTime = DateTime.UtcNow.AddHours(1) }
            });

        _hotelRepoMock
            .Setup(r => r.GetByAsync(It.IsAny<Expression<Func<HotelPassModel, bool>>>()))
            .ReturnsAsync(new List<HotelPassModel>
            {
                new HotelPassModel { Id = 1, ParkingLotId = 1, Start = DateTime.UtcNow.AddHours(-1), End = DateTime.UtcNow.AddHours(1), ExtraTime = TimeSpan.FromMinutes(10) }
            });

        var result = await _service.GetAvailableSpotsByLotIdAsync(lot.Id);

        Assert.AreEqual(ServiceStatus.Success, result.Status);
        Assert.AreEqual(0, result.Data);
    }

    [TestMethod]
    public async Task GetAvailableSpotsByAddressAsync_LotNotFound_ReturnsNotFound()
    {
        _parkingRepoMock
            .Setup(r => r.GetByAsync(It.IsAny<Expression<Func<ParkingLotModel, bool>>>()))
            .ReturnsAsync(new List<ParkingLotModel>());

        var result = await _service.GetAvailableSpotsByAddressAsync("Addr");

        Assert.AreEqual(ServiceStatus.NotFound, result.Status);
        Assert.AreEqual("Parking lot not found", result.Error);
    }

    [TestMethod]
    public async Task GetAvailableSpotsByAddressAsync_AddressNormalized_FindsLot_ReturnsOk()
    {
        var lot = new ParkingLotModel { Id = 1, Capacity = 10, Address = "Addr" };

        _parkingRepoMock
            .Setup(r => r.GetByAsync(It.IsAny<Expression<Func<ParkingLotModel, bool>>>()))
            .ReturnsAsync(new[] { lot }.ToList());

        _sessionRepoMock
            .Setup(r => r.GetByAsync(It.IsAny<Expression<Func<ParkingSessionModel, bool>>>()))
            .ReturnsAsync(new List<ParkingSessionModel>());

        _reservationRepoMock
            .Setup(r => r.GetByAsync(It.IsAny<Expression<Func<ReservationModel, bool>>>()))
            .ReturnsAsync(new List<ReservationModel>());

        _hotelRepoMock
            .Setup(r => r.GetByAsync(It.IsAny<Expression<Func<HotelPassModel, bool>>>()))
            .ReturnsAsync(new List<HotelPassModel>());

        var result = await _service.GetAvailableSpotsByAddressAsync("  ADDR  ");

        Assert.AreEqual(ServiceStatus.Success, result.Status);
        Assert.AreEqual(10, result.Data);
    }

    [TestMethod]
    public async Task GetAvailableSpotsForPeriodAsync_LotNotFound_ReturnsNotFound()
    {
        _parkingRepoMock
            .Setup(r => r.FindByIdAsync(It.IsAny<long>()))
            .ReturnsAsync((ParkingLotModel)null!);

        var result = await _service.GetAvailableSpotsForPeriodAsync(1, DateTime.UtcNow, DateTime.UtcNow.AddHours(1));

        Assert.AreEqual(ServiceStatus.NotFound, result.Status);
        Assert.AreEqual("Parking lot not found", result.Error);
    }

    [TestMethod]
    public async Task GetAvailableSpotsForPeriodAsync_EndBeforeStart_ReturnsBadRequest()
    {
        var lot = new ParkingLotModel { Id = 1, Capacity = 10, Address = "Addr" };

        _parkingRepoMock
            .Setup(r => r.FindByIdAsync(lot.Id))
            .ReturnsAsync(lot);

        var start = DateTime.UtcNow.AddHours(2);
        var end = DateTime.UtcNow.AddHours(1);

        var result = await _service.GetAvailableSpotsForPeriodAsync(lot.Id, start, end);

        Assert.AreEqual(ServiceStatus.BadRequest, result.Status);
        Assert.AreEqual("End time must be after start time.", result.Error);

        _sessionRepoMock.Verify(r => r.GetByAsync(It.IsAny<Expression<Func<ParkingSessionModel, bool>>>()), Times.Never);
        _reservationRepoMock.Verify(r => r.GetByAsync(It.IsAny<Expression<Func<ReservationModel, bool>>>()), Times.Never);
        _hotelRepoMock.Verify(r => r.GetByAsync(It.IsAny<Expression<Func<HotelPassModel, bool>>>()), Times.Never);
    }

    [TestMethod]
    public async Task GetAvailableSpotsForPeriodAsync_OverlappingCounts_ReturnsAvailable()
    {
        var lot = new ParkingLotModel { Id = 1, Capacity = 10, Address = "Addr" };

        _parkingRepoMock
            .Setup(r => r.FindByIdAsync(lot.Id))
            .ReturnsAsync(lot);

        var start = DateTime.UtcNow.AddHours(1);
        var end = DateTime.UtcNow.AddHours(3);

        _sessionRepoMock
            .Setup(r => r.GetByAsync(It.IsAny<Expression<Func<ParkingSessionModel, bool>>>()))
            .ReturnsAsync(new List<ParkingSessionModel>
            {
                new ParkingSessionModel { Id = 1, ParkingLotId = 1, Started = start.AddMinutes(-30), Stopped = null },
                new ParkingSessionModel { Id = 2, ParkingLotId = 1, Started = start.AddMinutes(-10), Stopped = start.AddMinutes(10) }
            });

        _reservationRepoMock
            .Setup(r => r.GetByAsync(It.IsAny<Expression<Func<ReservationModel, bool>>>()))
            .ReturnsAsync(new List<ReservationModel>
            {
                new ReservationModel { Id = 1, ParkingLotId = 1, Status = ReservationStatus.Pending, StartTime = start.AddMinutes(-5), EndTime = end.AddMinutes(5) },
                new ReservationModel { Id = 2, ParkingLotId = 1, Status = ReservationStatus.Confirmed, StartTime = start.AddMinutes(30), EndTime = end.AddMinutes(-30) }
            });

        _hotelRepoMock
            .Setup(r => r.GetByAsync(It.IsAny<Expression<Func<HotelPassModel, bool>>>()))
            .ReturnsAsync(new List<HotelPassModel>
            {
                new HotelPassModel { Id = 1, ParkingLotId = 1, Start = start.AddMinutes(-1), End = end.AddMinutes(-1), ExtraTime = TimeSpan.FromMinutes(10) }
            });

        var result = await _service.GetAvailableSpotsForPeriodAsync(lot.Id, start, end);

        Assert.AreEqual(ServiceStatus.Success, result.Status);
        Assert.AreEqual(5, result.Data);
    }

    [TestMethod]
    public async Task GetAvailableSpotsForPeriodAsync_OccupancyExceedsCapacity_ReturnsZero()
    {
        var lot = new ParkingLotModel { Id = 1, Capacity = 2, Address = "Addr" };

        _parkingRepoMock
            .Setup(r => r.FindByIdAsync(lot.Id))
            .ReturnsAsync(lot);

        var start = DateTime.UtcNow.AddHours(1);
        var end = DateTime.UtcNow.AddHours(2);

        _sessionRepoMock
            .Setup(r => r.GetByAsync(It.IsAny<Expression<Func<ParkingSessionModel, bool>>>()))
            .ReturnsAsync(new List<ParkingSessionModel>
            {
                new ParkingSessionModel { Id = 1, ParkingLotId = 1, Started = start.AddMinutes(-10), Stopped = null },
                new ParkingSessionModel { Id = 2, ParkingLotId = 1, Started = start.AddMinutes(-20), Stopped = null }
            });

        _reservationRepoMock
            .Setup(r => r.GetByAsync(It.IsAny<Expression<Func<ReservationModel, bool>>>()))
            .ReturnsAsync(new List<ReservationModel>
            {
                new ReservationModel { Id = 1, ParkingLotId = 1, Status = ReservationStatus.Confirmed, StartTime = start.AddMinutes(-1), EndTime = end.AddMinutes(1) }
            });

        _hotelRepoMock
            .Setup(r => r.GetByAsync(It.IsAny<Expression<Func<HotelPassModel, bool>>>()))
            .ReturnsAsync(new List<HotelPassModel>
            {
                new HotelPassModel { Id = 1, ParkingLotId = 1, Start = start.AddMinutes(-1), End = end.AddMinutes(-1), ExtraTime = TimeSpan.FromMinutes(10) }
            });

        var result = await _service.GetAvailableSpotsForPeriodAsync(lot.Id, start, end);

        Assert.AreEqual(ServiceStatus.Success, result.Status);
        Assert.AreEqual(0, result.Data);
    }

    #endregion
}