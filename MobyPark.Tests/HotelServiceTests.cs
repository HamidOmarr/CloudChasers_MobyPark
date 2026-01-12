using System.Linq.Expressions;

using MobyPark.DTOs.Hotel;
using MobyPark.Models;
using MobyPark.Models.Repositories.Interfaces;
using MobyPark.Services;
using MobyPark.Services.Results;

using Moq;

namespace MobyPark.Tests;

[TestClass]
public class HotelServiceTests
{
    #region Setup

    private Mock<IRepository<HotelModel>> _mockHotelRepo = null!;
    private Mock<IRepository<ParkingLotModel>> _mockLotRepo = null!;

    private HotelService _hotelService = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _mockHotelRepo = new Mock<IRepository<HotelModel>>();
        _mockLotRepo = new Mock<IRepository<ParkingLotModel>>();

        _hotelService = new HotelService(_mockHotelRepo.Object, _mockLotRepo.Object);
    }

    #endregion

    #region Create

    [TestMethod]
    public async Task CreateHotelAsync_ParkingLotNotFound_ReturnsNotFound()
    {
        var dto = new CreateHotelDto
        {
            Name = "Test Hotel",
            Address = "Street 1",
            IBAN = "NL00BANK0123456789",
            HotelParkingLotId = 5
        };

        _mockLotRepo
            .Setup(r => r.FindByIdAsync(dto.HotelParkingLotId))
            .ReturnsAsync((ParkingLotModel)null!);

        var result = await _hotelService.CreateHotelAsync(dto);

        Assert.AreEqual(ServiceStatus.NotFound, result.Status);
        Assert.IsTrue(result.Error?.Contains("There was no parking lot found with that id") ?? false);
        _mockHotelRepo.Verify(r => r.Add(It.IsAny<HotelModel>()), Times.Never);
        _mockHotelRepo.Verify(r => r.SaveChangesAsync(), Times.Never);
    }

    [TestMethod]
    public async Task CreateHotelAsync_AddressTaken_ReturnsConflict()
    {
        var dto = new CreateHotelDto
        {
            Name = "Test Hotel",
            Address = "Street 1",
            IBAN = "NL00BANK0123456789",
            HotelParkingLotId = 5
        };

        _mockLotRepo
            .Setup(r => r.FindByIdAsync(dto.HotelParkingLotId))
            .ReturnsAsync(new ParkingLotModel { Id = dto.HotelParkingLotId });

        _mockHotelRepo
            .Setup(r => r.GetByAsync(It.IsAny<Expression<Func<HotelModel, bool>>>()))
            .ReturnsAsync(new List<HotelModel> { new HotelModel { Address = dto.Address } });

        var result = await _hotelService.CreateHotelAsync(dto);

        Assert.AreEqual(ServiceStatus.Conflict, result.Status);
        Assert.IsTrue(result.Error?.Contains("Address already taken") ?? false);
        _mockHotelRepo.Verify(r => r.Add(It.IsAny<HotelModel>()), Times.Never);
        _mockHotelRepo.Verify(r => r.SaveChangesAsync(), Times.Never);
    }

    [TestMethod]
    public async Task CreateHotelAsync_ParkingLotTaken_ReturnsConflict()
    {
        var dto = new CreateHotelDto
        {
            Name = "Test Hotel",
            Address = "Street 1",
            IBAN = "NL00BANK0123456789",
            HotelParkingLotId = 5
        };

        _mockLotRepo
            .Setup(r => r.FindByIdAsync(dto.HotelParkingLotId))
            .ReturnsAsync(new ParkingLotModel { Id = dto.HotelParkingLotId });

        _mockHotelRepo
            .Setup(r => r.GetByAsync(x => x.Address == dto.Address))
            .ReturnsAsync(new List<HotelModel>());

        _mockHotelRepo
            .Setup(r => r.GetByAsync(x => x.HotelParkingLotId == dto.HotelParkingLotId))
            .ReturnsAsync(new List<HotelModel> { new HotelModel { HotelParkingLotId = dto.HotelParkingLotId } });

        var result = await _hotelService.CreateHotelAsync(dto);

        Assert.AreEqual(ServiceStatus.Conflict, result.Status);
        Assert.IsTrue(result.Error?.Contains("Parking lot already taken") ?? false);
        _mockHotelRepo.Verify(r => r.Add(It.IsAny<HotelModel>()), Times.Never);
        _mockHotelRepo.Verify(r => r.SaveChangesAsync(), Times.Never);
    }

    [TestMethod]
    public async Task CreateHotelAsync_ValidHotel_ReturnsOkAndSaves()
    {
        var dto = new CreateHotelDto
        {
            Name = "Test Hotel",
            Address = "Street 1",
            IBAN = "NL00BANK0123456789",
            HotelParkingLotId = 5
        };

        _mockLotRepo
            .Setup(r => r.FindByIdAsync(dto.HotelParkingLotId))
            .ReturnsAsync(new ParkingLotModel { Id = dto.HotelParkingLotId });

        _mockHotelRepo
            .Setup(r => r.GetByAsync(It.IsAny<Expression<Func<HotelModel, bool>>>()))
            .ReturnsAsync(new List<HotelModel>());

        _mockHotelRepo
            .Setup(r => r.Add(It.IsAny<HotelModel>()))
            .Callback<HotelModel>(h => h.Id = 123);

        var result = await _hotelService.CreateHotelAsync(dto);

        Assert.AreEqual(ServiceStatus.Success, result.Status);
        Assert.IsNotNull(result.Data);
        Assert.AreEqual(123, result.Data.Id);
        Assert.AreEqual(dto.Name, result.Data.Name);
        Assert.AreEqual(dto.Address, result.Data.Address);
        Assert.AreEqual(dto.HotelParkingLotId, result.Data.HotelParkingLotId);
        _mockHotelRepo.Verify(r => r.Add(It.IsAny<HotelModel>()), Times.Once);
        _mockHotelRepo.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    #endregion

    #region Patch

    [TestMethod]
    public async Task PatchHotelAsync_HotelNotFound_ReturnsNotFound()
    {
        var dto = new PatchHotelDto { Id = 1 };

        _mockHotelRepo
            .Setup(r => r.FindByIdAsync(dto.Id))
            .ReturnsAsync((HotelModel)null!);

        var result = await _hotelService.PatchHotelAsync(dto);

        Assert.AreEqual(ServiceStatus.NotFound, result.Status);
        Assert.IsTrue(result.Error?.Contains("No hotel found with that id") ?? false);
    }

    [TestMethod]
    public async Task PatchHotelAsync_ParkingLotTaken_ReturnsConflict()
    {
        var existing = new HotelModel
        {
            Id = 1,
            Name = "Old",
            Address = "Old Street",
            IBAN = "OLD",
            HotelParkingLotId = 5
        };

        var dto = new PatchHotelDto
        {
            Id = 1,
            HotelParkingLotId = 10
        };

        _mockHotelRepo
            .Setup(r => r.FindByIdAsync(existing.Id))
            .ReturnsAsync(existing);

        _mockHotelRepo
            .Setup(r => r.GetByAsync(It.IsAny<Expression<Func<HotelModel, bool>>>()))
            .ReturnsAsync(new List<HotelModel> { new HotelModel { HotelParkingLotId = dto.HotelParkingLotId.Value } });

        var result = await _hotelService.PatchHotelAsync(dto);

        Assert.AreEqual(ServiceStatus.Conflict, result.Status);
        Assert.IsTrue(result.Error?.Contains("Parking lot is already taken by another hotel") ?? false);
        _mockHotelRepo.Verify(r => r.Update(It.IsAny<HotelModel>()), Times.Never);
        _mockHotelRepo.Verify(r => r.SaveChangesAsync(), Times.Never);
    }

    [TestMethod]
    public async Task PatchHotelAsync_AddressTaken_ReturnsConflict()
    {
        var existing = new HotelModel
        {
            Id = 1,
            Name = "Old",
            Address = "Old Street",
            IBAN = "OLD",
            HotelParkingLotId = 5
        };

        var dto = new PatchHotelDto
        {
            Id = 1,
            Address = "New Street"
        };

        _mockHotelRepo
            .Setup(r => r.FindByIdAsync(existing.Id))
            .ReturnsAsync(existing);

        _mockHotelRepo
            .Setup(r => r.GetByAsync(It.IsAny<Expression<Func<HotelModel, bool>>>()))
            .ReturnsAsync(new List<HotelModel> { new HotelModel { Address = dto.Address } });

        var result = await _hotelService.PatchHotelAsync(dto);

        Assert.AreEqual(ServiceStatus.Conflict, result.Status);
        Assert.IsTrue(result.Error?.Contains("There is already another hotel registered at the newly given address") ?? false);
        _mockHotelRepo.Verify(r => r.Update(It.IsAny<HotelModel>()), Times.Never);
        _mockHotelRepo.Verify(r => r.SaveChangesAsync(), Times.Never);
    }

    [TestMethod]
    public async Task PatchHotelAsync_ValidUpdate_UpdatesAndReturnsOk()
    {
        var existing = new HotelModel
        {
            Id = 1,
            Name = "Old",
            Address = "Old Street",
            IBAN = "OLDIBAN",
            HotelParkingLotId = 5
        };

        var dto = new PatchHotelDto
        {
            Id = 1,
            Name = "New",
            Address = "New Street",
            IBAN = "NEWIBAN"
        };

        _mockHotelRepo
            .Setup(r => r.FindByIdAsync(existing.Id))
            .ReturnsAsync(existing);

        _mockHotelRepo
            .Setup(r => r.GetByAsync(It.IsAny<Expression<Func<HotelModel, bool>>>()))
            .ReturnsAsync(new List<HotelModel>());

        var result = await _hotelService.PatchHotelAsync(dto);

        Assert.AreEqual(ServiceStatus.Success, result.Status);
        Assert.IsNotNull(result.Data);
        Assert.AreEqual(existing.Id, result.Data.Id);
        Assert.AreEqual(dto.Name, result.Data.Name);
        Assert.AreEqual(dto.Address, result.Data.Address);
        Assert.AreEqual(dto.IBAN, result.Data.IBAN);
        Assert.AreEqual(existing.HotelParkingLotId, result.Data.HotelParkingLotId);
        _mockHotelRepo.Verify(r => r.Update(existing), Times.Once);
        _mockHotelRepo.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    #endregion

    #region Delete

    [TestMethod]
    public async Task DeleteHotelAsync_HotelNotFound_ReturnsNotFound()
    {
        long id = 1;

        _mockHotelRepo
            .Setup(r => r.FindByIdAsync(id))
            .ReturnsAsync((HotelModel)null!);

        var result = await _hotelService.DeleteHotelAsync(id);

        Assert.AreEqual(ServiceStatus.NotFound, result.Status);
        Assert.IsFalse(result.Data);
        _mockHotelRepo.Verify(r => r.Deletee(It.IsAny<HotelModel>()), Times.Never);
        _mockHotelRepo.Verify(r => r.SaveChangesAsync(), Times.Never);
    }

    [TestMethod]
    public async Task DeleteHotelAsync_HotelFound_DeletesAndReturnsTrue()
    {
        long id = 1;
        var existing = new HotelModel { Id = id };

        _mockHotelRepo
            .Setup(r => r.FindByIdAsync(id))
            .ReturnsAsync(existing);

        var result = await _hotelService.DeleteHotelAsync(id);

        Assert.AreEqual(ServiceStatus.Success, result.Status);
        Assert.IsTrue(result.Data);
        _mockHotelRepo.Verify(r => r.Deletee(existing), Times.Once);
        _mockHotelRepo.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    #endregion

    #region Get

    [TestMethod]
    public async Task GetAllHotelsAsync_ReturnsMappedList()
    {
        var hotels = new List<HotelModel>
        {
            new HotelModel { Id = 1, Name = "Hotel A", Address = "A Street", HotelParkingLotId = 10 },
            new HotelModel { Id = 2, Name = "Hotel B", Address = "B Street", HotelParkingLotId = 20 }
        };

        _mockHotelRepo
            .Setup(r => r.ReadAllAsync())
            .ReturnsAsync(hotels);

        var result = await _hotelService.GetAllHotelsAsync();

        Assert.AreEqual(ServiceStatus.Success, result.Status);
        Assert.IsNotNull(result.Data);
        Assert.AreEqual(2, result.Data.Count);
        Assert.AreEqual("Hotel A", result.Data[0].Name);
        Assert.AreEqual("Hotel B", result.Data[1].Name);
    }

    [TestMethod]
    public async Task GetHotelByIdAsync_HotelNotFound_ReturnsNotFound()
    {
        long id = 1;

        _mockHotelRepo
            .Setup(r => r.FindByIdAsync(id))
            .ReturnsAsync((HotelModel)null!);

        var result = await _hotelService.GetHotelByIdAsync(id);

        Assert.AreEqual(ServiceStatus.NotFound, result.Status);
        Assert.IsNull(result.Data);
        Assert.IsTrue(result.Error?.Contains("No hotel found with that id") ?? false);
    }

    [TestMethod]
    public async Task GetHotelByIdAsync_HotelFound_ReturnsOkWithDto()
    {
        long id = 1;
        var hotel = new HotelModel
        {
            Id = id,
            Name = "Hotel A",
            Address = "A Street",
            HotelParkingLotId = 10
        };

        _mockHotelRepo
            .Setup(r => r.FindByIdAsync(id))
            .ReturnsAsync(hotel);

        var result = await _hotelService.GetHotelByIdAsync(id);

        Assert.AreEqual(ServiceStatus.Success, result.Status);
        Assert.IsNotNull(result.Data);
        Assert.AreEqual(hotel.Id, result.Data.Id);
        Assert.AreEqual(hotel.Name, result.Data.Name);
        Assert.AreEqual(hotel.Address, result.Data.Address);
        Assert.AreEqual(hotel.HotelParkingLotId, result.Data.HotelParkingLotId);
    }

    #endregion
}