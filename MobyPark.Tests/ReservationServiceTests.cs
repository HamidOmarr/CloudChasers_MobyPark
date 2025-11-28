using MobyPark.DTOs.Reservation.Request;
using MobyPark.Models;
using MobyPark.Models.Repositories.Interfaces;
using MobyPark.Services;
using MobyPark.Services.Interfaces;
using MobyPark.Services.Results.LicensePlate;
using MobyPark.Services.Results.ParkingLot;
using MobyPark.Services.Results.Price;
using MobyPark.Services.Results.Reservation;
using MobyPark.Services.Results.User;
using MobyPark.Services.Results.UserPlate;
using Moq;

namespace MobyPark.Tests;

[TestClass]
public sealed class ReservationServiceTests
{
    #region Setup
    private Mock<IReservationRepository> _mockReservationsRepo = null!;
    private Mock<IRepository<ParkingLotModel>> _mockParkingLotService = null!;
    private Mock<ILicensePlateService> _mockLicensePlatesService = null!;
    private Mock<IUserService> _mockUsersService = null!;
    private Mock<IUserPlateService> _mockUserPlatesService = null!;
    private Mock<IPricingService> _mockPricingService = null!;
    private ReservationService _reservationService = null!;
    private ParkingLotService _parkingLotService = null!;

    private const long RequestingUserId = 1L;
    private const long OtherUserId = 2L;
    private const long AdminUserId = 3L;
    private const string UserPlate = "ABC-123";
    private const string OtherUserPlate = "XYZ-789";
    private const long DefaultLotId = 1L;

    [TestInitialize]
    public void TestInitialize()
    {
        _mockReservationsRepo = new Mock<IReservationRepository>();
        _mockParkingLotService = new Mock<IRepository<ParkingLotModel>>();
        _mockLicensePlatesService = new Mock<ILicensePlateService>();
        _mockUsersService = new Mock<IUserService>();
        _mockUserPlatesService = new Mock<IUserPlateService>();
        _mockPricingService = new Mock<IPricingService>();

        _reservationService = new ReservationService(
            _mockReservationsRepo.Object,
            _parkingLotService = new ParkingLotService(_mockParkingLotService.Object),
            _mockLicensePlatesService.Object,
            _mockUsersService.Object,
            _mockUserPlatesService.Object,
            _mockPricingService.Object
        );
    }

    private CreateReservationDto CreateValidDto(long lotId = 1, string plate = UserPlate, DateTimeOffset? start = null, DateTimeOffset? end = null, string? username = null)
    {
        return new CreateReservationDto
        {
            ParkingLotId = lotId,
            LicensePlate = plate,
            StartDate = start ?? DateTimeOffset.UtcNow.AddHours(1),
            EndDate = end ?? DateTimeOffset.UtcNow.AddHours(3),
            Username = username
        };
    }

    private ReservationModel CreateReservationModel(long id = 1, long lotId = DefaultLotId, string plate = UserPlate, ReservationStatus status = ReservationStatus.Pending, DateTimeOffset? start = null, DateTimeOffset? end = null)
    {
        return new ReservationModel
        {
            Id = id,
            ParkingLotId = lotId,
            LicensePlateNumber = plate.ToUpper(),
            Status = status,
            StartTime = start ?? DateTimeOffset.UtcNow.AddHours(1),
            EndTime = end ?? DateTimeOffset.UtcNow.AddHours(3),
            CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-10)
        };
    }

    #endregion

    #region Create

    [TestMethod]
    [DataRow(DefaultLotId, UserPlate)]
    public async Task CreateReservation_Success_ReturnsSuccess(long lotId, string plate)
    {
        // Arrange
        var dto = CreateValidDto(lotId, plate);
        var lot = new ParkingLotModel { Id = lotId, Tariff = 5 };
        var licensePlate = new LicensePlateModel { LicensePlateNumber = plate.ToUpper() };
        var user = new UserModel { Id = RequestingUserId, Username = "requestingUser" };
        var userPlate = new UserPlateModel { UserId = RequestingUserId, LicensePlateNumber = plate.ToUpper() };
        var expectedCost = 10m;
        long newReservationId = 123L;

        _mockParkingLotService
            .Setup(repo => repo.FindByIdAsync(lotId))
            .ReturnsAsync(lot);

        _mockLicensePlatesService
            .Setup(s => s.GetByLicensePlate(plate.ToUpper()))
            .ReturnsAsync(new GetLicensePlateResult.Success(licensePlate));

        _mockUsersService
            .Setup(s => s.GetUserById(RequestingUserId))
            .ReturnsAsync(new GetUserResult.Success(user));

        _mockUserPlatesService
            .Setup(s => s.GetUserPlateByUserIdAndPlate(RequestingUserId, plate.ToUpper()))
            .ReturnsAsync(new GetUserPlateResult.Success(userPlate));

        _mockReservationsRepo
            .Setup(r => r.GetByLicensePlate(plate.ToUpper()))
            .ReturnsAsync(new List<ReservationModel>());

        _mockPricingService
            .Setup(s => s.CalculateParkingCost(
                It.IsAny<ParkingLotModel>(),
                dto.StartDate,
                dto.EndDate))
            .Returns(new CalculatePriceResult.Success(expectedCost, 2, 0));

        _mockReservationsRepo
            .Setup(r => r.CreateWithId(It.IsAny<ReservationModel>()))
            .ReturnsAsync((true, newReservationId));

        // Act
        var result = await _reservationService.CreateReservation(dto, RequestingUserId);

        // Assert
        Assert.IsInstanceOfType(result, typeof(CreateReservationResult.Success));
        Assert.AreEqual(newReservationId, ((CreateReservationResult.Success)result).Reservation.Id);

        _mockReservationsRepo.Verify(
            r => r.CreateWithId(It.Is<ReservationModel>(res => res.LicensePlateNumber == plate.ToUpper())),
            Times.Once);
    }

    [TestMethod]
    [DataRow(DefaultLotId, UserPlate, "otherUser", true)]
    public async Task CreateReservation_AdminForOtherUser_Success_ReturnsSuccess(
        long lotId, string plate, string targetUsername, bool isAdmin)
    {
        // Arrange
        var dto = CreateValidDto(lotId, plate, username: targetUsername);
        var lot = new ParkingLotModel { Id = lotId, Tariff = 5 };
        var licensePlate = new LicensePlateModel { LicensePlateNumber = plate.ToUpper() };
        var targetUser = new UserModel { Id = OtherUserId, Username = targetUsername };
        var userPlate = new UserPlateModel { UserId = OtherUserId, LicensePlateNumber = plate.ToUpper() };
        var expectedCost = 10m;
        long newReservationId = 124L;

        _mockParkingLotService
            .Setup(repo => repo.FindByIdAsync(lotId))
            .ReturnsAsync(lot);

        _mockLicensePlatesService
            .Setup(s => s.GetByLicensePlate(plate.ToUpper()))
            .ReturnsAsync(new GetLicensePlateResult.Success(licensePlate));

        _mockUsersService
            .Setup(s => s.GetUserByUsername(targetUsername))
            .ReturnsAsync(new GetUserResult.Success(targetUser));

        _mockUsersService
            .Setup(s => s.GetUserById(OtherUserId))
            .ReturnsAsync(new GetUserResult.Success(targetUser));

        _mockUserPlatesService
            .Setup(s => s.GetUserPlateByUserIdAndPlate(OtherUserId, plate.ToUpper()))
            .ReturnsAsync(new GetUserPlateResult.Success(userPlate));

        _mockReservationsRepo
            .Setup(r => r.GetByLicensePlate(plate.ToUpper()))
            .ReturnsAsync(new List<ReservationModel>());

        _mockPricingService
            .Setup(s => s.CalculateParkingCost(
                It.IsAny<ParkingLotModel>(),
                dto.StartDate,
                dto.EndDate))
            .Returns(new CalculatePriceResult.Success(expectedCost, 2, 0));

        _mockReservationsRepo
            .Setup(r => r.CreateWithId(It.IsAny<ReservationModel>()))
            .ReturnsAsync((true, newReservationId));

        // Act
        var result = await _reservationService.CreateReservation(dto, AdminUserId, isAdmin);

        // Assert
        Assert.IsInstanceOfType(result, typeof(CreateReservationResult.Success));
        Assert.AreEqual(newReservationId, ((CreateReservationResult.Success)result).Reservation.Id);
    }

    [TestMethod]
    [DataRow(99L)]
    public async Task CreateReservation_LotNotFound_ReturnsLotNotFound(long lotId)
    {
        // Arrange
        var dto = CreateValidDto(lotId);

        _mockParkingLotService
            .Setup(repo => repo.FindByIdAsync(lotId))
            .ReturnsAsync((ParkingLotModel?)null);

        // Act
        var result = await _reservationService.CreateReservation(dto, RequestingUserId);

        // Assert
        Assert.IsInstanceOfType(result, typeof(CreateReservationResult.LotNotFound));
    }

    [TestMethod]
    public async Task CreateReservation_EndDateBeforeStartDate_ReturnsLotNotFound()
    {
        // Arrange
        var dto = CreateValidDto(start: DateTimeOffset.UtcNow.AddHours(2), end: DateTimeOffset.UtcNow.AddHours(1));

        // Act
        var result = await _reservationService.CreateReservation(dto, RequestingUserId);

        // Assert
        Assert.IsInstanceOfType(result, typeof(CreateReservationResult.LotNotFound));
    }

    [TestMethod]
    public async Task CreateReservation_StartDateInPast_ReturnsLotNotFound()
    {
        // Arrange
        var dto = CreateValidDto(start: DateTimeOffset.UtcNow.AddMinutes(-5));

        // Act
        var result = await _reservationService.CreateReservation(dto, RequestingUserId);

        // Assert
        // ValidateInputAndFetchLot returns null -> service returns LotNotFound
        Assert.IsInstanceOfType(result, typeof(CreateReservationResult.LotNotFound));
    }

    [TestMethod]
    [DataRow("UNKNOWN-PLATE")]
    public async Task CreateReservation_PlateNotFound_ReturnsPlateNotFound(string plate)
    {
        // Arrange
        var dto = CreateValidDto(plate: plate);
        var lot = new ParkingLotModel { Id = DefaultLotId, Tariff = 5 };

        _mockParkingLotService
            .Setup(repo => repo.FindByIdAsync(DefaultLotId))
            .ReturnsAsync(lot);

        _mockLicensePlatesService
            .Setup(s => s.GetByLicensePlate(plate.ToUpper()))
            .ReturnsAsync(new GetLicensePlateResult.NotFound("Not found"));

        // Act
        var result = await _reservationService.CreateReservation(dto, RequestingUserId);

        // Assert
        Assert.IsInstanceOfType(result, typeof(CreateReservationResult.PlateNotFound));
    }

    [TestMethod]
    [DataRow("otherUser", false)]
    public async Task CreateReservation_UserCreatesForOther_ReturnsForbidden(string targetUsername, bool isAdmin)
    {
        // Arrange
        var dto = CreateValidDto(username: targetUsername);
        var lot = new ParkingLotModel { Id = DefaultLotId, Tariff = 5 };
        var licensePlate = new LicensePlateModel { LicensePlateNumber = UserPlate.ToUpper() };

        _mockParkingLotService
            .Setup(repo => repo.FindByIdAsync(DefaultLotId))
            .ReturnsAsync(lot);

        _mockLicensePlatesService
            .Setup(s => s.GetByLicensePlate(UserPlate.ToUpper()))
            .ReturnsAsync(new GetLicensePlateResult.Success(licensePlate));

        // Act
        var result = await _reservationService.CreateReservation(dto, RequestingUserId, isAdmin);

        // Assert
        Assert.IsInstanceOfType(result, typeof(CreateReservationResult.Forbidden));
    }

    [TestMethod]
    [DataRow("nonExistentUser", true)]
    public async Task CreateReservation_AdminForNonExistentUser_ReturnsUserNotFound(string targetUsername, bool isAdmin)
    {
        // Arrange
        var dto = CreateValidDto(username: targetUsername);
        var lot = new ParkingLotModel { Id = DefaultLotId, Tariff = 5 };
        var licensePlate = new LicensePlateModel { LicensePlateNumber = UserPlate.ToUpper() };

        _mockParkingLotService
            .Setup(repo => repo.FindByIdAsync(DefaultLotId))
            .ReturnsAsync(lot);

        _mockLicensePlatesService
            .Setup(s => s.GetByLicensePlate(UserPlate.ToUpper()))
            .ReturnsAsync(new GetLicensePlateResult.Success(licensePlate));

        _mockUsersService
            .Setup(s => s.GetUserByUsername(targetUsername))
            .ReturnsAsync(new GetUserResult.NotFound());

        // Act
        var result = await _reservationService.CreateReservation(dto, AdminUserId, isAdmin);

        // Assert
        Assert.IsInstanceOfType(result, typeof(CreateReservationResult.UserNotFound));
    }

    [TestMethod]
    [DataRow(OtherUserPlate)]
    public async Task CreateReservation_UserPlateNotOwned_ReturnsPlateNotOwned(string plate)
    {
        // Arrange
        var dto = CreateValidDto(plate: plate);
        var lot = new ParkingLotModel { Id = DefaultLotId, Tariff = 5 };
        var licensePlate = new LicensePlateModel { LicensePlateNumber = plate.ToUpper() };
        var user = new UserModel { Id = RequestingUserId, Username = "requestingUser" };

        _mockParkingLotService
            .Setup(repo => repo.FindByIdAsync(DefaultLotId))
            .ReturnsAsync(lot);

        _mockLicensePlatesService
            .Setup(s => s.GetByLicensePlate(plate.ToUpper()))
            .ReturnsAsync(new GetLicensePlateResult.Success(licensePlate));

        _mockUsersService
            .Setup(s => s.GetUserById(RequestingUserId))
            .ReturnsAsync(new GetUserResult.Success(user));

        _mockUserPlatesService
            .Setup(s => s.GetUserPlateByUserIdAndPlate(RequestingUserId, plate.ToUpper()))
            .ReturnsAsync(new GetUserPlateResult.NotFound());

        // Act
        var result = await _reservationService.CreateReservation(dto, RequestingUserId);

        // Assert
        Assert.IsInstanceOfType(result, typeof(CreateReservationResult.PlateNotOwned));
    }

    [TestMethod]
    public async Task CreateReservation_OverlappingReservationExists_ReturnsAlreadyExists()
    {
        // Arrange
        var dto = CreateValidDto();
        var lot = new ParkingLotModel { Id = DefaultLotId, Tariff = 5 };
        var licensePlate = new LicensePlateModel { LicensePlateNumber = UserPlate.ToUpper() };
        var user = new UserModel { Id = RequestingUserId };
        var userPlate = new UserPlateModel { UserId = RequestingUserId, LicensePlateNumber = UserPlate.ToUpper() };
        var existingReservation = CreateReservationModel(
            id: 99,
            start: dto.StartDate.AddHours(-1),
            end: dto.StartDate.AddHours(1),
            status: ReservationStatus.Confirmed);

        _mockParkingLotService
            .Setup(repo => repo.FindByIdAsync(DefaultLotId))
            .ReturnsAsync(lot);

        _mockLicensePlatesService
            .Setup(s => s.GetByLicensePlate(UserPlate.ToUpper()))
            .ReturnsAsync(new GetLicensePlateResult.Success(licensePlate));

        _mockUsersService
            .Setup(s => s.GetUserById(RequestingUserId))
            .ReturnsAsync(new GetUserResult.Success(user));

        _mockUserPlatesService
            .Setup(s => s.GetUserPlateByUserIdAndPlate(RequestingUserId, UserPlate.ToUpper()))
            .ReturnsAsync(new GetUserPlateResult.Success(userPlate));

        _mockReservationsRepo
            .Setup(r => r.GetByLicensePlate(UserPlate.ToUpper()))
            .ReturnsAsync(new List<ReservationModel> { existingReservation });

        // Act
        var result = await _reservationService.CreateReservation(dto, RequestingUserId);

        // Assert
        Assert.IsInstanceOfType(result, typeof(CreateReservationResult.AlreadyExists));
    }

    [TestMethod]
    public async Task CreateReservation_PricingFails_ReturnsError()
    {
        // Arrange
        var dto = CreateValidDto();
        var lot = new ParkingLotModel { Id = DefaultLotId, Tariff = 5 };
        var licensePlate = new LicensePlateModel { LicensePlateNumber = UserPlate.ToUpper() };
        var user = new UserModel { Id = RequestingUserId };
        var userPlate = new UserPlateModel { UserId = RequestingUserId, LicensePlateNumber = UserPlate.ToUpper() };

        _mockParkingLotService
            .Setup(repo => repo.FindByIdAsync(DefaultLotId))
            .ReturnsAsync(lot);

        _mockLicensePlatesService
            .Setup(s => s.GetByLicensePlate(UserPlate.ToUpper()))
            .ReturnsAsync(new GetLicensePlateResult.Success(licensePlate));

        _mockUsersService
            .Setup(s => s.GetUserById(RequestingUserId))
            .ReturnsAsync(new GetUserResult.Success(user));

        _mockUserPlatesService
            .Setup(s => s.GetUserPlateByUserIdAndPlate(RequestingUserId, UserPlate.ToUpper()))
            .ReturnsAsync(new GetUserPlateResult.Success(userPlate));

        _mockReservationsRepo
            .Setup(r => r.GetByLicensePlate(UserPlate.ToUpper()))
            .ReturnsAsync(new List<ReservationModel>());

        _mockPricingService
            .Setup(s => s.CalculateParkingCost(
                It.IsAny<ParkingLotModel>(),
                dto.StartDate,
                dto.EndDate))
            .Returns(new CalculatePriceResult.Error("Pricing error"));

        // Act
        var result = await _reservationService.CreateReservation(dto, RequestingUserId);

        // Assert
        Assert.IsInstanceOfType(result, typeof(CreateReservationResult.Error));
        StringAssert.Contains(((CreateReservationResult.Error)result).Message, "Failed to calculate reservation cost");
    }

    [TestMethod]
    public async Task CreateReservation_PersistenceFails_ReturnsError()
    {
        // Arrange
        var dto = CreateValidDto();
        var lot = new ParkingLotModel { Id = DefaultLotId, Tariff = 5 };
        var licensePlate = new LicensePlateModel { LicensePlateNumber = UserPlate.ToUpper() };
        var user = new UserModel { Id = RequestingUserId };
        var userPlate = new UserPlateModel { UserId = RequestingUserId, LicensePlateNumber = UserPlate.ToUpper() };
        var expectedCost = 10m;

        _mockParkingLotService
            .Setup(repo => repo.FindByIdAsync(DefaultLotId))
            .ReturnsAsync(lot);

        _mockLicensePlatesService
            .Setup(s => s.GetByLicensePlate(UserPlate.ToUpper()))
            .ReturnsAsync(new GetLicensePlateResult.Success(licensePlate));

        _mockUsersService
            .Setup(s => s.GetUserById(RequestingUserId))
            .ReturnsAsync(new GetUserResult.Success(user));

        _mockUserPlatesService
            .Setup(s => s.GetUserPlateByUserIdAndPlate(RequestingUserId, UserPlate.ToUpper()))
            .ReturnsAsync(new GetUserPlateResult.Success(userPlate));

        _mockReservationsRepo
            .Setup(r => r.GetByLicensePlate(UserPlate.ToUpper()))
            .ReturnsAsync(new List<ReservationModel>());

        _mockPricingService
            .Setup(s => s.CalculateParkingCost(
                It.IsAny<ParkingLotModel>(),
                dto.StartDate,
                dto.EndDate))
            .Returns(new CalculatePriceResult.Success(expectedCost, 2, 0));

        _mockReservationsRepo
            .Setup(r => r.CreateWithId(It.IsAny<ReservationModel>()))
            .ReturnsAsync((false, 0L));

        // Act
        var result = await _reservationService.CreateReservation(dto, RequestingUserId);

        // Assert
        Assert.IsInstanceOfType(result, typeof(CreateReservationResult.Error));
        StringAssert.Contains(((CreateReservationResult.Error)result).Message, "Failed to save reservation");
    }

    #endregion

    #region GetById

    [TestMethod]
    [DataRow(123L, UserPlate)]
    public async Task GetReservationById_UserOwnsPlate_ReturnsSuccess(long id, string plate)
    {
        // Arrange
        var reservation = CreateReservationModel(id: id, plate: plate);
        var userPlate = new UserPlateModel { UserId = RequestingUserId, LicensePlateNumber = plate.ToUpper() };
        _mockReservationsRepo.Setup(reservationRepo => reservationRepo.GetById<ReservationModel>(id)).ReturnsAsync(reservation);
        _mockUserPlatesService.Setup(uPlateService => uPlateService.GetUserPlateByUserIdAndPlate(RequestingUserId, plate.ToUpper())).ReturnsAsync(new GetUserPlateResult.Success(userPlate));

        // Act
        var result = await _reservationService.GetReservationById(id, RequestingUserId);

        // Assert
        Assert.IsInstanceOfType(result, typeof(GetReservationResult.Success));
        Assert.AreEqual(id, ((GetReservationResult.Success)result).Reservation.Id);
    }

    [TestMethod]
    [DataRow(999L)]
    public async Task GetReservationById_ReservationNotFound_ReturnsNotFound(long id)
    {
        // Arrange
        _mockReservationsRepo.Setup(reservationRepo => reservationRepo.GetById<ReservationModel>(id)).ReturnsAsync((ReservationModel?)null);

        // Act
        var result = await _reservationService.GetReservationById(id, RequestingUserId);

        // Assert
        Assert.IsInstanceOfType(result, typeof(GetReservationResult.NotFound));
    }

    [TestMethod]
    [DataRow(123L, OtherUserPlate)]
    public async Task GetReservationById_UserDoesNotOwnPlate_ReturnsNotFound(long id, string plate)
    {
        // Arrange
        var reservation = CreateReservationModel(id: id, plate: plate);
        _mockReservationsRepo.Setup(reservationRepo => reservationRepo.GetById<ReservationModel>(id)).ReturnsAsync(reservation);
        _mockUserPlatesService.Setup(uPlateService => uPlateService.GetUserPlateByUserIdAndPlate(RequestingUserId, plate.ToUpper())).ReturnsAsync(new GetUserPlateResult.NotFound());

        // Act
        var result = await _reservationService.GetReservationById(id, RequestingUserId);

        // Assert
        Assert.IsInstanceOfType(result, typeof(GetReservationResult.NotFound));
    }

    #endregion

    #region GetByParkingLotId

    [TestMethod]
    [DataRow(DefaultLotId)]
    public async Task GetReservationsByParkingLotId_UserHasMatchingPlates_ReturnsSuccessList(long lotId)
    {
        // Arrange
        var res1 = CreateReservationModel(id: 1, lotId: lotId, plate: UserPlate);
        var res2 = CreateReservationModel(id: 2, lotId: lotId, plate: OtherUserPlate);
        var userPlate = new UserPlateModel { UserId = RequestingUserId, LicensePlateNumber = UserPlate };

        _mockReservationsRepo.Setup(reservationRepo => reservationRepo.GetByParkingLotId(lotId)).ReturnsAsync(new List<ReservationModel> { res1, res2 });
        _mockUserPlatesService.Setup(uPlateService => uPlateService.GetUserPlatesByUserId(RequestingUserId)).ReturnsAsync(new GetUserPlateListResult.Success(new List<UserPlateModel> { userPlate }));

        // Act
        var result = await _reservationService.GetReservationsByParkingLotId(lotId, RequestingUserId);

        // Assert
        Assert.IsInstanceOfType(result, typeof(GetReservationListResult.Success));
        var list = ((GetReservationListResult.Success)result).Reservations;
        Assert.AreEqual(1, list.Count);
        Assert.AreEqual(UserPlate, list[0].LicensePlateNumber);
    }

    [TestMethod]
    [DataRow(DefaultLotId)]
    public async Task GetReservationsByParkingLotId_NoReservationsAtLot_ReturnsNotFound(long lotId)
    {
        // Arrange
        _mockReservationsRepo.Setup(reservationRepo => reservationRepo.GetByParkingLotId(lotId)).ReturnsAsync(new List<ReservationModel>());

        // Act
        var result = await _reservationService.GetReservationsByParkingLotId(lotId, RequestingUserId);

        // Assert
        Assert.IsInstanceOfType(result, typeof(GetReservationListResult.NotFound));
    }

    [TestMethod]
    [DataRow(DefaultLotId)]
    public async Task GetReservationsByParkingLotId_UserHasNoPlates_ReturnsNotFound(long lotId)
    {
        // Arrange
        var res1 = CreateReservationModel(id: 1, lotId: lotId, plate: UserPlate);
        _mockReservationsRepo.Setup(reservationRepo => reservationRepo.GetByParkingLotId(lotId)).ReturnsAsync(new List<ReservationModel> { res1 });
        _mockUserPlatesService.Setup(uPlateService => uPlateService.GetUserPlatesByUserId(RequestingUserId)).ReturnsAsync(new GetUserPlateListResult.NotFound());

        // Act
        var result = await _reservationService.GetReservationsByParkingLotId(lotId, RequestingUserId);

        // Assert
        Assert.IsInstanceOfType(result, typeof(GetReservationListResult.NotFound));
    }

    [TestMethod]
    [DataRow(DefaultLotId)]
    public async Task GetReservationsByParkingLotId_UserHasNoMatchingPlates_ReturnsNotFound(long lotId)
    {
        // Arrange
        var res1 = CreateReservationModel(id: 1, lotId: lotId, plate: OtherUserPlate);
        var userPlate = new UserPlateModel { UserId = RequestingUserId, LicensePlateNumber = UserPlate };

        _mockReservationsRepo.Setup(reservationRepo => reservationRepo.GetByParkingLotId(lotId)).ReturnsAsync(new List<ReservationModel> { res1 });
        _mockUserPlatesService.Setup(uPlateService => uPlateService.GetUserPlatesByUserId(RequestingUserId)).ReturnsAsync(new GetUserPlateListResult.Success(new List<UserPlateModel> { userPlate }));

        // Act
        var result = await _reservationService.GetReservationsByParkingLotId(lotId, RequestingUserId);

        // Assert
        Assert.IsInstanceOfType(result, typeof(GetReservationListResult.NotFound));
    }

    #endregion

    #region GetByLicensePlate

    [TestMethod]
    [DataRow(UserPlate)]
    public async Task GetReservationsByLicensePlate_UserOwnsPlate_ReturnsSuccessList(string plate)
    {
        // Arrange
        var normalized = plate.ToUpper();
        var res1 = CreateReservationModel(id: 1, plate: normalized);
        var userPlate = new UserPlateModel { UserId = RequestingUserId, LicensePlateNumber = normalized };

        _mockUserPlatesService.Setup(uPlateService => uPlateService.GetUserPlateByUserIdAndPlate(RequestingUserId, normalized)).ReturnsAsync(new GetUserPlateResult.Success(userPlate));
        _mockReservationsRepo.Setup(reservationRepo => reservationRepo.GetByLicensePlate(normalized)).ReturnsAsync(new List<ReservationModel> { res1 });

        // Act
        var result = await _reservationService.GetReservationsByLicensePlate(plate, RequestingUserId);

        // Assert
        Assert.IsInstanceOfType(result, typeof(GetReservationListResult.Success));
        Assert.AreEqual(1, ((GetReservationListResult.Success)result).Reservations.Count);
    }

    [TestMethod]
    [DataRow(OtherUserPlate)]
    public async Task GetReservationsByLicensePlate_UserDoesNotOwnPlate_ReturnsNotFound(string plate)
    {
        // Arrange
        var normalized = plate.ToUpper();
        _mockUserPlatesService.Setup(uPlateService => uPlateService.GetUserPlateByUserIdAndPlate(RequestingUserId, normalized)).ReturnsAsync(new GetUserPlateResult.NotFound());

        // Act
        var result = await _reservationService.GetReservationsByLicensePlate(plate, RequestingUserId);

        // Assert
        Assert.IsInstanceOfType(result, typeof(GetReservationListResult.NotFound));
        _mockReservationsRepo.Verify(reservationRepo => reservationRepo.GetByLicensePlate(It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    [DataRow(UserPlate)]
    public async Task GetReservationsByLicensePlate_NoReservationsForPlate_ReturnsNotFound(string plate)
    {
        // Arrange
        var normalized = plate.ToUpper();
        var userPlate = new UserPlateModel { UserId = RequestingUserId, LicensePlateNumber = normalized };

        _mockUserPlatesService.Setup(uPlateService => uPlateService.GetUserPlateByUserIdAndPlate(RequestingUserId, normalized)).ReturnsAsync(new GetUserPlateResult.Success(userPlate));
        _mockReservationsRepo.Setup(reservationRepo => reservationRepo.GetByLicensePlate(normalized)).ReturnsAsync(new List<ReservationModel>());

        // Act
        var result = await _reservationService.GetReservationsByLicensePlate(plate, RequestingUserId);

        // Assert
        Assert.IsInstanceOfType(result, typeof(GetReservationListResult.NotFound));
    }

    #endregion

    #region GetByStatus

    [TestMethod]
    [DataRow("Pending", ReservationStatus.Pending)]
    [DataRow("CONFIRMED", ReservationStatus.Confirmed)]
    public async Task GetReservationsByStatus_UserHasMatchingPlates_ReturnsSuccessList(string statusString, ReservationStatus statusEnum)
    {
        // Arrange
        var res1 = CreateReservationModel(id: 1, plate: UserPlate, status: statusEnum);
        var res2 = CreateReservationModel(id: 2, plate: OtherUserPlate, status: statusEnum);

        var userPlate = new UserPlateModel { UserId = RequestingUserId, LicensePlateNumber = UserPlate };
        _mockReservationsRepo.Setup(reservationRepo => reservationRepo.GetByStatus(statusEnum)).ReturnsAsync(new List<ReservationModel> { res1, res2 });
        _mockUserPlatesService.Setup(uPlateService => uPlateService.GetUserPlatesByUserId(RequestingUserId)).ReturnsAsync(new GetUserPlateListResult.Success(new List<UserPlateModel> { userPlate }));

        // Act
        var result = await _reservationService.GetReservationsByStatus(statusString, RequestingUserId);

        // Assert
        Assert.IsInstanceOfType(result, typeof(GetReservationListResult.Success));
        var list = ((GetReservationListResult.Success)result).Reservations;
        Assert.AreEqual(1, list.Count);
        Assert.AreEqual(UserPlate, list[0].LicensePlateNumber);
        Assert.AreEqual(statusEnum, list[0].Status);
    }

    [TestMethod]
    [DataRow("Pending")]
    public async Task GetReservationsByStatus_NoReservationsWithStatus_ReturnsNotFound(string statusString)
    {
        // Arrange
        ReservationStatus statusEnum = Enum.Parse<ReservationStatus>(statusString, true);
        _mockReservationsRepo.Setup(reservationRepo => reservationRepo.GetByStatus(statusEnum)).ReturnsAsync(new List<ReservationModel>());

        // Act
        var result = await _reservationService.GetReservationsByStatus(statusString, RequestingUserId);

        // Assert
        Assert.IsInstanceOfType(result, typeof(GetReservationListResult.NotFound));
    }

    [TestMethod]
    [DataRow("Pending")]
    public async Task GetReservationsByStatus_UserHasNoPlates_ReturnsNotFound(string statusString)
    {
        // Arrange
        ReservationStatus statusEnum = Enum.Parse<ReservationStatus>(statusString, true);
        var res1 = CreateReservationModel(id: 1, plate: UserPlate, status: statusEnum);
        _mockReservationsRepo.Setup(reservationRepo => reservationRepo.GetByStatus(statusEnum)).ReturnsAsync(new List<ReservationModel> { res1 });
        _mockUserPlatesService.Setup(uPlateService => uPlateService.GetUserPlatesByUserId(RequestingUserId)).ReturnsAsync(new GetUserPlateListResult.NotFound());

        // Act
        var result = await _reservationService.GetReservationsByStatus(statusString, RequestingUserId);

        // Assert
        Assert.IsInstanceOfType(result, typeof(GetReservationListResult.NotFound));
    }

    [TestMethod]
    [DataRow("Pending")]
    public async Task GetReservationsByStatus_UserHasNoMatchingPlates_ReturnsNotFound(string statusString)
    {
        // Arrange
        ReservationStatus statusEnum = Enum.Parse<ReservationStatus>(statusString, true);
        var res1 = CreateReservationModel(id: 1, plate: OtherUserPlate, status: statusEnum);
        var userPlate = new UserPlateModel { UserId = RequestingUserId, LicensePlateNumber = UserPlate };

        _mockReservationsRepo.Setup(reservationRepo => reservationRepo.GetByStatus(statusEnum)).ReturnsAsync(new List<ReservationModel> { res1 });
        _mockUserPlatesService.Setup(uPlateService => uPlateService.GetUserPlatesByUserId(RequestingUserId)).ReturnsAsync(new GetUserPlateListResult.Success(new List<UserPlateModel> { userPlate }));

        // Act
        var result = await _reservationService.GetReservationsByStatus(statusString, RequestingUserId);

        // Assert
        Assert.IsInstanceOfType(result, typeof(GetReservationListResult.NotFound));
    }

    [TestMethod]
    [DataRow("InvalidStatus")]
    [DataRow(" ")]
    public async Task GetReservationsByStatus_InvalidStatus_ReturnsInvalidInput(string statusString)
    {
        // Act
        var result = await _reservationService.GetReservationsByStatus(statusString, RequestingUserId);

        // Assert
        Assert.IsInstanceOfType(result, typeof(GetReservationListResult.InvalidInput));
    }

    #endregion

    #region GetAll

    [TestMethod]
    public async Task GetAllReservations_Found_ReturnsSuccessList()
    {
        // Arrange
        var list = new List<ReservationModel> { CreateReservationModel(id: 1), CreateReservationModel(id: 2) };
        _mockReservationsRepo.Setup(reservationRepo => reservationRepo.GetAll()).ReturnsAsync(list);

        // Act
        var result = await _reservationService.GetAllReservations();

        // Assert
        Assert.IsInstanceOfType(result, typeof(GetReservationListResult.Success));
        Assert.AreEqual(2, ((GetReservationListResult.Success)result).Reservations.Count);
    }

    [TestMethod]
    public async Task GetAllReservations_NotFound_ReturnsNotFound()
    {
        // Arrange
        _mockReservationsRepo.Setup(reservationRepo => reservationRepo.GetAll()).ReturnsAsync(new List<ReservationModel>());

        // Act
        var result = await _reservationService.GetAllReservations();

        // Assert
        Assert.IsInstanceOfType(result, typeof(GetReservationListResult.NotFound));
    }

    #endregion

    #region Count

    [TestMethod]
    [DataRow(0)]
    [DataRow(42)]
    public async Task CountReservations_ReturnsCount(int count)
    {
        // Arrange
        _mockReservationsRepo.Setup(reservationRepo => reservationRepo.Count()).ReturnsAsync(count);

        // Act
        var result = await _reservationService.CountReservations();

        // Assert
        Assert.AreEqual(count, result);
    }

    #endregion

    #region Update

    [TestMethod]
    [DataRow(1L, ReservationStatus.Confirmed)]
    public async Task UpdateReservation_StatusChange_ReturnsSuccess(long id, ReservationStatus newStatus)
    {
        // Arrange
        var existing = CreateReservationModel(id: id, status: ReservationStatus.Pending);
        var dto = new UpdateReservationDto { Status = newStatus };
        var userPlate = new UserPlateModel { UserId = RequestingUserId, LicensePlateNumber = existing.LicensePlateNumber };

        _mockReservationsRepo.Setup(reservationRepo => reservationRepo.GetById<ReservationModel>(id)).ReturnsAsync(existing);
        _mockUserPlatesService.Setup(uPlateService => uPlateService.GetUserPlateByUserIdAndPlate(RequestingUserId, existing.LicensePlateNumber)).ReturnsAsync(new GetUserPlateResult.Success(userPlate));
        _mockReservationsRepo.Setup(reservationRepo => reservationRepo.Update(It.IsAny<ReservationModel>(), dto)).ReturnsAsync(true);

        // Act
        var result = await _reservationService.UpdateReservation(id, RequestingUserId, dto);

        // Assert
        Assert.IsInstanceOfType(result, typeof(UpdateReservationResult.Success));
        Assert.AreEqual(newStatus, ((UpdateReservationResult.Success)result).Reservation.Status);
        _mockPricingService.Verify(pricingService => pricingService.CalculateParkingCost(It.IsAny<ParkingLotModel>(), It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>()), Times.Never);
    }

    [TestMethod]
    [DataRow(1L)]
    public async Task UpdateReservation_DatesChange_RecalculatesCostAndReturnsSuccess(long id)
    {
        var startTime = DateTimeOffset.UtcNow.AddHours(2);
        var endTime = DateTimeOffset.UtcNow.AddHours(5);
        var existing = CreateReservationModel(
            id: id,
            status: ReservationStatus.Pending,
            start: DateTimeOffset.UtcNow.AddHours(1),
            end: DateTimeOffset.UtcNow.AddHours(3));

        var dto = new UpdateReservationDto
        {
            StartTime = startTime,
            EndTime = endTime
        };

        var userPlate = new UserPlateModel
        {
            UserId = RequestingUserId,
            LicensePlateNumber = existing.LicensePlateNumber
        };

        var lot = new ParkingLotModel
        {
            Id = existing.ParkingLotId,
            Tariff = 5
        };

        var newCost = 15m;

        _mockReservationsRepo
            .Setup(r => r.GetById<ReservationModel>(id))
            .ReturnsAsync(existing);

        _mockUserPlatesService
            .Setup(s => s.GetUserPlateByUserIdAndPlate(RequestingUserId, existing.LicensePlateNumber))
            .ReturnsAsync(new GetUserPlateResult.Success(userPlate));

        _mockParkingLotService
            .Setup(repo => repo.FindByIdAsync(existing.ParkingLotId))
            .ReturnsAsync(lot);

        _mockPricingService
            .Setup(p => p.CalculateParkingCost(
                It.IsAny<ParkingLotModel>(),
                startTime,
                endTime))
            .Returns(new CalculatePriceResult.Success(newCost, 3, 0));

        _mockReservationsRepo
            .Setup(r => r.Update(It.IsAny<ReservationModel>(), dto))
            .ReturnsAsync(true);

        // Act
        var result = await _reservationService.UpdateReservation(id, RequestingUserId, dto);

        // Assert
        Assert.IsInstanceOfType(result, typeof(UpdateReservationResult.Success));
        var successResult = (UpdateReservationResult.Success)result;
        Assert.AreEqual(startTime, successResult.Reservation.StartTime);
        Assert.AreEqual(endTime, successResult.Reservation.EndTime);
        Assert.AreEqual(newCost, successResult.Reservation.Cost);
    }

    [TestMethod]
    [DataRow(1L, ReservationStatus.Pending)]
    public async Task UpdateReservation_NoChanges_ReturnsNoChangesMade(long id, ReservationStatus status)
    {
        // Arrange
        var existing = CreateReservationModel(id: id, status: status);
        var dto = new UpdateReservationDto { Status = status };
        var userPlate = new UserPlateModel { UserId = RequestingUserId, LicensePlateNumber = existing.LicensePlateNumber };

        _mockReservationsRepo.Setup(reservationRepo => reservationRepo.GetById<ReservationModel>(id)).ReturnsAsync(existing);
        _mockUserPlatesService.Setup(uPlateService => uPlateService.GetUserPlateByUserIdAndPlate(RequestingUserId, existing.LicensePlateNumber)).ReturnsAsync(new GetUserPlateResult.Success(userPlate));

        // Act
        var result = await _reservationService.UpdateReservation(id, RequestingUserId, dto);

        // Assert
        Assert.IsInstanceOfType(result, typeof(UpdateReservationResult.NoChangesMade));
    }


    [TestMethod]
    [DataRow(99L)]
    public async Task UpdateReservation_NotFound_ReturnsNotFound(long id)
    {
        // Arrange
        var dto = new UpdateReservationDto { Status = ReservationStatus.Cancelled };
        _mockReservationsRepo.Setup(reservationRepo => reservationRepo.GetById<ReservationModel>(id)).ReturnsAsync((ReservationModel?)null);

        // Act
        var result = await _reservationService.UpdateReservation(id, RequestingUserId, dto);

        // Assert
        Assert.IsInstanceOfType(result, typeof(UpdateReservationResult.NotFound));
    }

    [TestMethod]
    [DataRow(1L)]
    public async Task UpdateReservation_CannotChangeCompletedStatus_ReturnsError(long id)
    {
        // Arrange
        var existing = CreateReservationModel(id: id, status: ReservationStatus.Completed);
        var dto = new UpdateReservationDto { Status = ReservationStatus.Cancelled };
        var userPlate = new UserPlateModel { UserId = RequestingUserId, LicensePlateNumber = existing.LicensePlateNumber };

        _mockReservationsRepo.Setup(reservationRepo => reservationRepo.GetById<ReservationModel>(id)).ReturnsAsync(existing);
        _mockUserPlatesService.Setup(uPlateService => uPlateService.GetUserPlateByUserIdAndPlate(RequestingUserId, existing.LicensePlateNumber)).ReturnsAsync(new GetUserPlateResult.Success(userPlate));

        // Act
        var result = await _reservationService.UpdateReservation(id, RequestingUserId, dto);

        // Assert
        Assert.IsInstanceOfType(result, typeof(UpdateReservationResult.Error));
        StringAssert.Contains(((UpdateReservationResult.Error)result).Message, "Cannot change the status of a completed reservation.");
    }

    [TestMethod]
    [DataRow(1L)]
    public async Task UpdateReservation_CannotChangeStartedReservationDate_ReturnsError(long id)
    {
        // Arrange
        var startTimeInPast = DateTimeOffset.UtcNow.AddHours(-2);
        var existing = CreateReservationModel(id: id, status: ReservationStatus.Confirmed, start: startTimeInPast);
        var dto = new UpdateReservationDto { StartTime = DateTimeOffset.UtcNow.AddHours(1) };
        var userPlate = new UserPlateModel { UserId = RequestingUserId, LicensePlateNumber = existing.LicensePlateNumber };

        _mockReservationsRepo.Setup(reservationRepo => reservationRepo.GetById<ReservationModel>(id)).ReturnsAsync(existing);
        _mockUserPlatesService.Setup(uPlateService => uPlateService.GetUserPlateByUserIdAndPlate(RequestingUserId, existing.LicensePlateNumber)).ReturnsAsync(new GetUserPlateResult.Success(userPlate));

        // Act
        var result = await _reservationService.UpdateReservation(id, RequestingUserId, dto);

        // Assert
        Assert.IsInstanceOfType(result, typeof(UpdateReservationResult.Error));
        StringAssert.Contains(((UpdateReservationResult.Error)result).Message, "Cannot change dates of a reservation that has already started.");
    }

    [TestMethod]
    [DataRow(1L)]
    public async Task UpdateReservation_EndTimeBeforeStartTime_ReturnsError(long id)
    {
        // Arrange
        var startTime = DateTimeOffset.UtcNow.AddHours(5);
        var invalidEndTime = DateTimeOffset.UtcNow.AddHours(4);
        var existing = CreateReservationModel(id: id, start: startTime);
        var dto = new UpdateReservationDto { EndTime = invalidEndTime };
        var userPlate = new UserPlateModel { UserId = RequestingUserId, LicensePlateNumber = existing.LicensePlateNumber };

        _mockReservationsRepo.Setup(reservationRepo => reservationRepo.GetById<ReservationModel>(id)).ReturnsAsync(existing);
        _mockUserPlatesService.Setup(uPlateService => uPlateService.GetUserPlateByUserIdAndPlate(RequestingUserId, existing.LicensePlateNumber)).ReturnsAsync(new GetUserPlateResult.Success(userPlate));

        // Act
        var result = await _reservationService.UpdateReservation(id, RequestingUserId, dto);

        // Assert
        Assert.IsInstanceOfType(result, typeof(UpdateReservationResult.Error));
        StringAssert.Contains(((UpdateReservationResult.Error)result).Message, "End time must be after the start time.");
    }

    [TestMethod]
    [DataRow(1L)]
    public async Task UpdateReservation_PricingFails_ReturnsError(long id)
    {
        // Arrange
        var startTime = DateTime.UtcNow.AddHours(2);
        var endTime = DateTime.UtcNow.AddHours(5);
        var existing = CreateReservationModel(id: id);
        var dto = new UpdateReservationDto { StartTime = startTime, EndTime = endTime };

        var userPlate = new UserPlateModel
        {
            UserId = RequestingUserId,
            LicensePlateNumber = existing.LicensePlateNumber
        };

        var lot = new ParkingLotModel
        {
            Id = existing.ParkingLotId,
            Tariff = 5
        };

        _mockReservationsRepo
            .Setup(r => r.GetById<ReservationModel>(id))
            .ReturnsAsync(existing);

        _mockUserPlatesService
            .Setup(s => s.GetUserPlateByUserIdAndPlate(RequestingUserId, existing.LicensePlateNumber))
            .ReturnsAsync(new GetUserPlateResult.Success(userPlate));

        _mockParkingLotService
            .Setup(repo => repo.FindByIdAsync(existing.ParkingLotId))
            .ReturnsAsync(lot);

        _mockPricingService
            .Setup(p => p.CalculateParkingCost(
                It.IsAny<ParkingLotModel>(),
                startTime,
                endTime))
            .Returns(new CalculatePriceResult.Error("Pricing failed"));

        // Act
        var result = await _reservationService.UpdateReservation(id, RequestingUserId, dto);

        // Assert
        Assert.IsInstanceOfType(result, typeof(UpdateReservationResult.Error));
        StringAssert.Contains(((UpdateReservationResult.Error)result).Message, "Failed to recalculate cost");
    }

    [TestMethod]
    [DataRow(1L)]
    public async Task UpdateReservation_DbUpdateFails_ReturnsError(long id)
    {
        // Arrange
        var existing = CreateReservationModel(id: id, status: ReservationStatus.Pending);
        var dto = new UpdateReservationDto { Status = ReservationStatus.Confirmed };
        var userPlate = new UserPlateModel { UserId = RequestingUserId, LicensePlateNumber = existing.LicensePlateNumber };

        _mockReservationsRepo.Setup(reservationRepo => reservationRepo.GetById<ReservationModel>(id)).ReturnsAsync(existing);
        _mockUserPlatesService.Setup(uPlateService => uPlateService.GetUserPlateByUserIdAndPlate(RequestingUserId, existing.LicensePlateNumber)).ReturnsAsync(new GetUserPlateResult.Success(userPlate));
        _mockReservationsRepo.Setup(reservationRepo => reservationRepo.Update(It.IsAny<ReservationModel>(), dto)).ReturnsAsync(false);

        // Act
        var result = await _reservationService.UpdateReservation(id, RequestingUserId, dto);

        // Assert
        Assert.IsInstanceOfType(result, typeof(UpdateReservationResult.Error));
        StringAssert.Contains(((UpdateReservationResult.Error)result).Message, "Database update failed");
    }

    #endregion

    #region Delete

    [TestMethod]
    [DataRow(1L)]
    public async Task DeleteReservation_Success_ReturnsSuccess(long id)
    {
        // Arrange
        var reservation = CreateReservationModel(id: id, status: ReservationStatus.Pending);
        var userPlate = new UserPlateModel { UserId = RequestingUserId, LicensePlateNumber = reservation.LicensePlateNumber };

        _mockReservationsRepo.Setup(reservationRepo => reservationRepo.GetById<ReservationModel>(id)).ReturnsAsync(reservation);
        _mockUserPlatesService.Setup(uPlateService => uPlateService.GetUserPlateByUserIdAndPlate(RequestingUserId, reservation.LicensePlateNumber)).ReturnsAsync(new GetUserPlateResult.Success(userPlate));
        _mockReservationsRepo.Setup(reservationRepo => reservationRepo.Delete(reservation)).ReturnsAsync(true);

        // Act
        var result = await _reservationService.DeleteReservation(id, RequestingUserId);

        // Assert
        Assert.IsInstanceOfType(result, typeof(DeleteReservationResult.Success));
        _mockReservationsRepo.Verify(reservationRepo => reservationRepo.Delete(reservation), Times.Once);
    }

    [TestMethod]
    [DataRow(99L)]
    public async Task DeleteReservation_NotFound_ReturnsNotFound(long id)
    {
        // Arrange
        _mockReservationsRepo.Setup(reservationRepo => reservationRepo.GetById<ReservationModel>(id)).ReturnsAsync((ReservationModel?)null);

        // Act
        var result = await _reservationService.DeleteReservation(id, RequestingUserId);

        // Assert
        Assert.IsInstanceOfType(result, typeof(DeleteReservationResult.NotFound));
    }

    [TestMethod]
    [DataRow(1L)]
    public async Task DeleteReservation_UserDoesNotOwn_ReturnsNotFound(long id)
    {
        // Arrange
        var reservation = CreateReservationModel(id: id, plate: OtherUserPlate); // Different plate
        _mockReservationsRepo.Setup(reservationRepo => reservationRepo.GetById<ReservationModel>(id)).ReturnsAsync(reservation);
        _mockUserPlatesService.Setup(uPlateService => uPlateService.GetUserPlateByUserIdAndPlate(RequestingUserId, OtherUserPlate)).ReturnsAsync(new GetUserPlateResult.NotFound());

        // Act
        var result = await _reservationService.DeleteReservation(id, RequestingUserId);

        // Assert
        Assert.IsInstanceOfType(result, typeof(DeleteReservationResult.NotFound));
    }

    [TestMethod]
    [DataRow(1L)]
    public async Task DeleteReservation_CannotDeleteCompleted_ReturnsError(long id)
    {
        // Arrange
        var reservation = CreateReservationModel(id: id, status: ReservationStatus.Completed); // Completed status
        var userPlate = new UserPlateModel { UserId = RequestingUserId, LicensePlateNumber = reservation.LicensePlateNumber };

        _mockReservationsRepo.Setup(reservationRepo => reservationRepo.GetById<ReservationModel>(id)).ReturnsAsync(reservation);
        _mockUserPlatesService.Setup(uPlateService => uPlateService.GetUserPlateByUserIdAndPlate(RequestingUserId, reservation.LicensePlateNumber)).ReturnsAsync(new GetUserPlateResult.Success(userPlate));

        // Act
        var result = await _reservationService.DeleteReservation(id, RequestingUserId);

        // Assert
        Assert.IsInstanceOfType(result, typeof(DeleteReservationResult.Error));
        StringAssert.Contains(((DeleteReservationResult.Error)result).Message, "Cannot delete a completed reservation");
        _mockReservationsRepo.Verify(reservationRepo => reservationRepo.Delete(It.IsAny<ReservationModel>()), Times.Never);
    }

    [TestMethod]
    [DataRow(1L)]
    public async Task DeleteReservation_DbDeleteFails_ReturnsError(long id)
    {
        // Arrange
        var reservation = CreateReservationModel(id: id, status: ReservationStatus.Pending);
        var userPlate = new UserPlateModel { UserId = RequestingUserId, LicensePlateNumber = reservation.LicensePlateNumber };

        _mockReservationsRepo.Setup(reservationRepo => reservationRepo.GetById<ReservationModel>(id)).ReturnsAsync(reservation);
        _mockUserPlatesService.Setup(uPlateService => uPlateService.GetUserPlateByUserIdAndPlate(RequestingUserId, reservation.LicensePlateNumber)).ReturnsAsync(new GetUserPlateResult.Success(userPlate));
        _mockReservationsRepo.Setup(reservationRepo => reservationRepo.Delete(reservation)).ReturnsAsync(false);

        // Act
        var result = await _reservationService.DeleteReservation(id, RequestingUserId);

        // Assert
        Assert.IsInstanceOfType(result, typeof(DeleteReservationResult.Error));
        StringAssert.Contains(((DeleteReservationResult.Error)result).Message, "Database deletion failed");
    }

    #endregion
}