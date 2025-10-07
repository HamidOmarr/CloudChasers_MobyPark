using MobyPark.Models;
using MobyPark.Models.DataService;
using MobyPark.Models.Access;
using MobyPark.Services;
using Moq;

namespace MobyPark.Tests;

[TestClass]
public class VehicleServiceTests
{
    private Mock<IDataAccess>? _mockDataService;
    private Mock<IVehicleAccess>? _mockVehicleAccess;
    private VehicleService? _vehicleService;

    [TestInitialize]
    public void TestInitialize()
    {
        _mockDataService = new Mock<IDataAccess>();
        _mockVehicleAccess = new Mock<IVehicleAccess>();

        _mockDataService.Setup(ds => ds.Vehicles).Returns(_mockVehicleAccess.Object);
        _vehicleService = new VehicleService(_mockDataService.Object);
    }

    [TestMethod]
    [DataRow(1)]
    [DataRow(42)]
    [DataRow(999)]
    public async Task GetVehicleById_ExistingId_ReturnsVehicle(int vehicleId)
    {
        var vehicle = new VehicleModel { Id = vehicleId, LicensePlate = "ABC123" };
        _mockVehicleAccess!.Setup(v => v.GetById(vehicleId)).ReturnsAsync(vehicle);

        var result = await _vehicleService!.GetVehicleById(vehicleId);

        Assert.AreEqual(vehicle, result);
        _mockVehicleAccess.Verify(v => v.GetById(vehicleId), Times.Once);
    }

    [TestMethod]
    [DataRow(0)]
    [DataRow(-1)]
    public async Task GetVehicleById_NonExistingId_ThrowsKeyNotFoundException(int vehicleId)
    {
        _mockVehicleAccess!.Setup(v => v.GetById(vehicleId)).ReturnsAsync((VehicleModel?)null);

        await Assert.ThrowsExceptionAsync<KeyNotFoundException>(async () =>
            await _vehicleService!.GetVehicleById(vehicleId));
    }

    [TestMethod]
    [DataRow(1)]
    [DataRow(42)]
    public async Task GetVehicleByUserId_VehiclesExist_ReturnsList(int userId)
    {
        var vehicles = new List<VehicleModel>
        {
            new() { UserId = userId, LicensePlate = "ABC123" },
            new() { UserId = userId, LicensePlate = "XYZ789" }
        };

        _mockVehicleAccess!.Setup(v => v.GetByUserId(userId)).ReturnsAsync(vehicles);

        var result = await _vehicleService!.GetVehicleByUserId(userId);

        Assert.AreEqual(2, result.Count);
        _mockVehicleAccess.Verify(v => v.GetByUserId(userId), Times.Once);
    }

    [TestMethod]
    [DataRow(1)]
    public async Task GetVehicleByUserId_NoVehicles_ThrowsKeyNotFoundException(int userId)
    {
        _mockVehicleAccess!.Setup(v => v.GetByUserId(userId)).ReturnsAsync(new List<VehicleModel>());

        await Assert.ThrowsExceptionAsync<KeyNotFoundException>(async () =>
            await _vehicleService!.GetVehicleByUserId(userId));
    }

    [TestMethod]
    [DataRow("ABC123")]
    [DataRow("XYZ789")]
    public async Task GetVehicleByLicensePlate_Existing_ReturnsVehicle(string plate)
    {
        var vehicle = new VehicleModel { LicensePlate = plate };
        _mockVehicleAccess!.Setup(v => v.GetByLicensePlate(plate)).ReturnsAsync(vehicle);

        var result = await _vehicleService!.GetVehicleByLicensePlate(plate);

        Assert.AreEqual(vehicle, result);
        _mockVehicleAccess.Verify(v => v.GetByLicensePlate(plate), Times.Once);
    }

    [TestMethod]
    [DataRow("NOTFOUND")]
    public async Task GetVehicleByLicensePlate_NonExisting_ThrowsKeyNotFoundException(string plate)
    {
        _mockVehicleAccess!.Setup(v => v.GetByLicensePlate(plate)).ReturnsAsync((VehicleModel?)null);

        await Assert.ThrowsExceptionAsync<KeyNotFoundException>(async () =>
            await _vehicleService!.GetVehicleByLicensePlate(plate));
    }

    [TestMethod]
    [DataRow(1, "ABC123", "Toyota", "Corolla", "Blue", 2020)]
    [DataRow(2, "XYZ789", "Honda", "Civic", "Red", 2019)]
    public async Task CreateVehicle_ValidInput_CreatesVehicle(int userId, string plate, string make, string model, string color, int year)
    {
        _mockVehicleAccess!.Setup(v => v.Create(It.IsAny<VehicleModel>()))
            .ReturnsAsync(true).Verifiable();

        var vehicle = new VehicleModel
        {
            UserId = userId,
            LicensePlate = plate,
            Make = make,
            Model = model,
            Color = color,
            Year = year,
            CreatedAt = DateTime.UtcNow
        };

        var result = await _vehicleService!.CreateVehicle(vehicle);

        Assert.AreEqual(userId, result.UserId);
        Assert.AreEqual(plate, result.LicensePlate);
        Assert.AreEqual(make, result.Make);
        Assert.AreEqual(model, result.Model);
        Assert.AreEqual(color, result.Color);
        Assert.AreEqual(year, result.Year);
        Assert.IsTrue(result.CreatedAt <= DateTime.UtcNow);

        _mockVehicleAccess.Verify(v => v.CreateWithId(It.Is<VehicleModel>(veh =>
            veh.UserId == userId && veh.LicensePlate == plate)), Times.Once);
    }

    [TestMethod]
    [DataRow(null, "Toyota", "Corolla", "Blue", 2020)]
    [DataRow("ABC123", null, "Corolla", "Blue", 2020)]
    [DataRow("ABC123", "Toyota", null, "Blue", 2020)]
    [DataRow("ABC123", "Toyota", "Corolla", null, 2020)]
    public async Task CreateVehicle_NullParameter_ThrowsArgumentNullException(string plate, string make, string model, string color, int year)
    {
        var vehicle = new VehicleModel
        {
            UserId = 1,
            LicensePlate = plate,
            Make = make,
            Model = model,
            Color = color,
            Year = year,
            CreatedAt = DateTime.UtcNow
        };

        await Assert.ThrowsExceptionAsync<ArgumentNullException>(async () =>
            await _vehicleService!.CreateVehicle(vehicle));

        _mockVehicleAccess!.Verify(v => v.Create(It.IsAny<VehicleModel>()), Times.Never);
    }

    [TestMethod]
    [DataRow(0, "ABC123", "Toyota", "Corolla", "Blue", 2020)]
    [DataRow(1, "ABC123", "Toyota", "Corolla", "Blue", 0)]
    public async Task CreateVehicle_InvalidUserIdOrYear_ThrowsArgumentOutOfRangeException(int userId, string plate, string make, string model, string color, int year)
    {
        var vehicle = new VehicleModel
        {
            UserId = userId,
            LicensePlate = plate,
            Make = make,
            Model = model,
            Color = color,
            Year = year,
            CreatedAt = DateTime.UtcNow
        };

        await Assert.ThrowsExceptionAsync<ArgumentOutOfRangeException>(async () =>
            await _vehicleService!.CreateVehicle(vehicle));

        _mockVehicleAccess!.Verify(v => v.Create(It.IsAny<VehicleModel>()), Times.Never);
    }

    [TestMethod]
    [DataRow(1, "ABC123", "Toyota", "Corolla", "Blue", 2021)]
    [DataRow(2, "XYZ789", "Honda", "Civic", "Red", 2022)]
    public async Task UpdateVehicle_ValidInput_CallsUpdateAndReturnsVehicle(int userId, string plate, string make, string model, string color, int year)
    {
        _mockVehicleAccess!.Setup(v => v.Update(It.IsAny<VehicleModel>()))
            .ReturnsAsync(true).Verifiable();

        // create vehicle with updated values
        var vehicle = new VehicleModel
        {
            UserId = userId,
            LicensePlate = plate,
            Make = make,
            Model = model,
            Color = color,
            Year = year,
            CreatedAt = DateTime.UtcNow
        };

        var result = await _vehicleService!.UpdateVehicle(vehicle);

        Assert.AreEqual(userId, result.UserId);
        Assert.AreEqual(plate, result.LicensePlate);
        Assert.AreEqual(make, result.Make);
        Assert.AreEqual(model, result.Model);
        Assert.AreEqual(color, result.Color);
        Assert.AreEqual(year, result.Year);

        _mockVehicleAccess.Verify(v => v.Update(It.Is<VehicleModel>(veh =>
            veh.UserId == userId && veh.LicensePlate == plate)), Times.Once);
    }

    [TestMethod]
    [DataRow(1, null, "Toyota", "Corolla", "Blue", 2021)]
    [DataRow(1, "ABC123", null, "Corolla", "Blue", 2021)]
    [DataRow(1, "ABC123", "Toyota", null, "Blue", 2021)]
    [DataRow(1, "ABC123", "Toyota", "Corolla", null, 2021)]
    public async Task UpdateVehicle_NullParameter_ThrowsArgumentNullException(int userId, string plate, string make, string model, string color, int year)
    {
        var vehicle = new VehicleModel
        {
            UserId = userId,
            LicensePlate = plate,
            Make = make,
            Model = model,
            Color = color,
            Year = year,
            CreatedAt = DateTime.UtcNow
        };

        await Assert.ThrowsExceptionAsync<ArgumentNullException>(async () =>
            await _vehicleService!.UpdateVehicle(vehicle));

        _mockVehicleAccess!.Verify(v => v.Update(It.IsAny<VehicleModel>()), Times.Never);
    }

    [TestMethod]
    [DataRow(0, "ABC123", "Toyota", "Corolla", "Blue", 2021)]
    [DataRow(1, "ABC123", "Toyota", "Corolla", "Blue", 0)]
    public async Task UpdateVehicle_InvalidUserIdOrYear_ThrowsArgumentOutOfRangeException(int userId, string plate, string make, string model, string color, int year)
    {
        var vehicle = new VehicleModel
        {
            UserId = userId,
            LicensePlate = plate,
            Make = make,
            Model = model,
            Color = color,
            Year = year,
            CreatedAt = DateTime.UtcNow
        };

        await Assert.ThrowsExceptionAsync<ArgumentOutOfRangeException>(async () =>
            await _vehicleService!.UpdateVehicle(vehicle));

        _mockVehicleAccess!.Verify(v => v.Update(It.IsAny<VehicleModel>()), Times.Never);
    }

    [TestMethod]
    [DataRow(1)]
    [DataRow(42)]
    public async Task DeleteVehicle_ExistingVehicle_ReturnsTrue(int vehicleId)
    {
        var vehicle = new VehicleModel { Id = vehicleId };
        _mockVehicleAccess!.Setup(v => v.GetById(vehicleId)).ReturnsAsync(vehicle);
        _mockVehicleAccess.Setup(v => v.Delete(vehicleId)).ReturnsAsync(true);

        var result = await _vehicleService!.DeleteVehicle(vehicleId);

        Assert.IsTrue(result);
        _mockVehicleAccess.Verify(v => v.Delete(vehicleId), Times.Once);
    }

    [TestMethod]
    [DataRow(99)]
    public async Task DeleteVehicle_NonExisting_ThrowsKeyNotFoundException(int vehicleId)
    {
        _mockVehicleAccess!.Setup(v => v.GetById(vehicleId)).ReturnsAsync((VehicleModel?)null);

        await Assert.ThrowsExceptionAsync<KeyNotFoundException>(async () =>
            await _vehicleService!.DeleteVehicle(vehicleId));

        _mockVehicleAccess.Verify(v => v.Delete(It.IsAny<int>()), Times.Never);
    }
}
