using Moq;
using MobyPark.Models;
using MobyPark.Models.Access;
using MobyPark.Models.DataService;
using MobyPark.Services;
using MobyPark.Services.Exceptions;

namespace MobyPark.Tests;

[TestClass]
public sealed class ParkingSessionStartTests
{
    private Mock<IDataAccess>? _mockDataAccess;
    private Mock<IParkingLotAccess>? _mockParkingLotAccess;
    private Mock<IParkingSessionAccess>? _mockSessionAccess;
    private Mock<IVehicleAccess>? _mockVehicleAccess;
    private Mock<IUserAccess>? _mockUserAccess;
    private Mock<IPaymentAccess>? _mockPaymentAccess;
    private Mock<IReservationAccess>? _mockReservationAccess;
    private Mock<PaymentPreauthService>? _mockPreauth;
    private Mock<GateService>? _mockGate;
    private ParkingSessionService? _parkingSessionService;

    [TestInitialize]
    public void TestInitialize()
    {
        _mockDataAccess = new Mock<IDataAccess>();
        _mockParkingLotAccess = new Mock<IParkingLotAccess>();
        _mockSessionAccess = new Mock<IParkingSessionAccess>();
        _mockVehicleAccess = new Mock<IVehicleAccess>();
        _mockUserAccess = new Mock<IUserAccess>();
        _mockPaymentAccess = new Mock<IPaymentAccess>();
        _mockReservationAccess = new Mock<IReservationAccess>();
        _mockPreauth = new Mock<PaymentPreauthService>();
        _mockGate = new Mock<GateService>();

        _mockDataAccess.Setup(d => d.ParkingLots).Returns(_mockParkingLotAccess.Object);
        _mockDataAccess.Setup(d => d.ParkingSessions).Returns(_mockSessionAccess.Object);
        _mockDataAccess.Setup(d => d.Vehicles).Returns(_mockVehicleAccess.Object);
        _mockDataAccess.Setup(d => d.Users).Returns(_mockUserAccess.Object);
        _mockDataAccess.Setup(d => d.Payments).Returns(_mockPaymentAccess.Object);
        _mockDataAccess.Setup(d => d.Reservations).Returns(_mockReservationAccess.Object);

        _parkingSessionService = new ParkingSessionService(_mockDataAccess.Object, _mockPreauth.Object, _mockGate.Object, null);

        _mockPreauth.Setup(p => p.PreauthorizeAsync(It.IsAny<string>(), It.IsAny<decimal>(), It.IsAny<bool>()))
            .ReturnsAsync((string _, decimal _, bool sim) => new PaymentPreauthService.PreauthResult { Approved = !sim, Reason = sim ? "Insufficient funds" : null });
        _mockGate.Setup(g => g.OpenGateAsync(It.IsAny<int>(), It.IsAny<string>())).ReturnsAsync(true);
    }

    private void SetupLot(int lotId, int capacity, int reserved)
    {
        var lot = new ParkingLotModel
        {
            Id = lotId,
            Capacity = capacity,
            Reserved = reserved,
            Tariff = 5,
            DayTariff = 20,
            Name = "Test",
            Location = "Loc",
            Address = "Addr",
            CreatedAt = DateOnly.FromDateTime(DateTime.UtcNow),
            Coordinates = new CoordinatesModel { Lat = 0, Lng = 0 }
        };
        _mockParkingLotAccess!.Setup(l => l.GetById(lotId)).ReturnsAsync(lot);
        _mockParkingLotAccess!.Setup(l => l.Update(It.IsAny<ParkingLotModel>())).ReturnsAsync(true);
    }

    [TestMethod]
    public async Task StartSession_Succeeds_WithExistingVehicleUser()
    {
        // Arrange
        SetupLot(1, 10, 0);
        var user = new UserModel { Id = 42, Username = "john" };
        var vehicle = new VehicleModel { UserId = user.Id, LicensePlate = "ABC123" };
    _mockVehicleAccess!.Setup(v => v.GetByLicensePlate("ABC123")).ReturnsAsync(vehicle);
    _mockUserAccess!.Setup(u => u.GetById(user.Id)).ReturnsAsync(user);
    _mockSessionAccess!.Setup(s => s.GetActiveByLicensePlate("ABC123")).ReturnsAsync((ParkingSessionModel?)null);
    _mockSessionAccess!.Setup(s => s.CreateWithId(It.IsAny<ParkingSessionModel>())).ReturnsAsync((true, 1001));

    // Act
    var session = await _parkingSessionService!.StartSession(1, "abc123", "cardtok", 12.5m, user.Username, false);

        // Assert
        Assert.AreEqual(1001, session.Id);
        Assert.AreEqual("ABC123", session.LicensePlate);
        Assert.AreEqual("john", session.User);
    _mockSessionAccess!.Verify(s => s.CreateWithId(It.IsAny<ParkingSessionModel>()), Times.Once);
    _mockParkingLotAccess!.Verify(l => l.Update(It.IsAny<ParkingLotModel>()), Times.AtLeastOnce);
    }

    [TestMethod]
    public async Task StartSession_Fails_WhenLotFull()
    {
        // Arrange
        SetupLot(1, 5, 5);
    // Act & Assert
    await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => _parkingSessionService!.StartSession(1, "XYZ999", "tok", 5m, null));
    }

    [TestMethod]
    public async Task StartSession_Fails_OnActiveSession()
    {
        // Arrange
        SetupLot(1, 10, 0);
    _mockSessionAccess!.Setup(s => s.GetActiveByLicensePlate("PLATE1")).ReturnsAsync(new ParkingSessionModel { Id = 7, LicensePlate = "PLATE1", Started = DateTime.UtcNow });
    // Act & Assert
    await Assert.ThrowsExceptionAsync<ActiveSessionAlreadyExistsException>(() => _parkingSessionService!.StartSession(1, "PLATE1", "tok", 5m, null));
    }

    [TestMethod]
    public async Task StartSession_Fails_OnInsufficientFunds()
    {
        // Arrange
        SetupLot(1, 10, 0);
    _mockSessionAccess!.Setup(s => s.GetActiveByLicensePlate("FUNDS1")).ReturnsAsync((ParkingSessionModel?)null);
    // Act & Assert
    await Assert.ThrowsExceptionAsync<UnauthorizedAccessException>(() => _parkingSessionService!.StartSession(1, "FUNDS1", "tok", 5m, null, simulateInsufficientFunds: true));
    }

    [TestMethod]
    public async Task StartSession_CreatesTemporaryUser_WhenUnknownPlate()
    {
        // Arrange
        SetupLot(1, 10, 0);
    _mockSessionAccess!.Setup(s => s.GetActiveByLicensePlate("NEW999")).ReturnsAsync((ParkingSessionModel?)null);
    _mockVehicleAccess!.Setup(v => v.GetByLicensePlate("NEW999")).ReturnsAsync((VehicleModel?)null);
    _mockUserAccess!.Setup(u => u.GetByUsername(It.IsAny<string>())).ReturnsAsync((UserModel?)null);
    _mockUserAccess!.Setup(u => u.CreateWithId(It.IsAny<UserModel>())).ReturnsAsync((true, 500));
    _mockSessionAccess!.Setup(s => s.CreateWithId(It.IsAny<ParkingSessionModel>())).ReturnsAsync((true, 321));

    // Act
    var session = await _parkingSessionService!.StartSession(1, "NEW999", "tok", 8m, null);

        // Assert
        Assert.AreEqual(321, session.Id);
        Assert.AreEqual("NEW999", session.LicensePlate);
    Assert.IsTrue(session.User.StartsWith("GUEST_"));
    _mockUserAccess.Verify(u => u.CreateWithId(It.IsAny<UserModel>()), Times.Once);
    }
}