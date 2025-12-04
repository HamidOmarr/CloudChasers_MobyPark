using MobyPark.DTOs.Reservation.Request;
using MobyPark.DTOs.ParkingLot.Request;
using MobyPark.Models;
using MobyPark.Models.Repositories.Interfaces;
using MobyPark.Services;
using MobyPark.Services.Interfaces;
using MobyPark.Services.Results;
using MobyPark.Services.Results.Price;
using MobyPark.Services.Results.UserPlate;
using MobyPark.Services.Results.Reservation;
using Moq;

namespace MobyPark.Tests;

[TestClass]
public sealed class ReservationCostEstimateTests
{
    private Mock<IReservationRepository> _mockReservationsRepo = null!;
    private Mock<IParkingLotService> _mockParkingLotsService = null!;
    private Mock<ILicensePlateService> _mockLicensePlatesService = null!;
    private Mock<IUserService> _mockUsersService = null!;
    private Mock<IUserPlateService> _mockUserPlatesService = null!;
    private Mock<IPricingService> _mockPricingService = null!;
    private ReservationService _service = null!;

    private const long UserId = 1L;
    private const string Plate = "ABC-123";
    private const long LotId = 1L;

    [TestInitialize]
    public void Init()
    {
        _mockReservationsRepo = new Mock<IReservationRepository>();
        _mockParkingLotsService = new Mock<IParkingLotService>();
        _mockLicensePlatesService = new Mock<ILicensePlateService>();
        _mockUsersService = new Mock<IUserService>();
        _mockUserPlatesService = new Mock<IUserPlateService>();
        _mockPricingService = new Mock<IPricingService>();

        _service = new ReservationService(
            _mockReservationsRepo.Object,
            _mockParkingLotsService.Object,
            _mockLicensePlatesService.Object,
            _mockUsersService.Object,
            _mockUserPlatesService.Object,
            _mockPricingService.Object);
    }

    private ReservationCostEstimateRequest MakeDto(DateTimeOffset? start = null, DateTimeOffset? end = null)
        => new ReservationCostEstimateRequest
        {
            ParkingLotId = LotId,
            LicensePlate = Plate,
            StartDate = start ?? DateTimeOffset.UtcNow.AddHours(1),
            EndDate = end ?? DateTimeOffset.UtcNow.AddHours(3)
        };

    [TestMethod]
    public async Task GetReservationCostEstimate_CapacityReached_ReturnsLotClosed()
    {
        // Arrange
        var dto = MakeDto();
        var lotDto = new ReadParkingLotDto { Id = LotId, Tariff = 5, Capacity = 1 };
        var userPlate = new UserPlateModel { UserId = UserId, LicensePlateNumber = Plate.ToUpper() };
        var existing = new ReservationModel
        {
            Id = 99,
            ParkingLotId = LotId,
            LicensePlateNumber = "XYZ-789",
            StartTime = dto.StartDate.AddMinutes(-10),
            EndTime = dto.StartDate.AddMinutes(10),
            Status = ReservationStatus.Confirmed
        };

        _mockParkingLotsService.Setup(s => s.GetParkingLotByIdAsync(LotId))
            .ReturnsAsync(ServiceResult<ReadParkingLotDto>.Ok(lotDto));
        _mockUserPlatesService.Setup(s => s.GetUserPlatesByUserId(UserId)).ReturnsAsync(new GetUserPlateListResult.Success(new List<UserPlateModel> { userPlate }));
        _mockReservationsRepo.Setup(s => s.GetByParkingLotId(LotId)).ReturnsAsync(new List<ReservationModel> { existing });

        // Act
        var result = await _service.GetReservationCostEstimate(dto, UserId);

        // Assert
        Assert.IsInstanceOfType(result, typeof(GetReservationCostEstimateResult.LotClosed));
        _mockPricingService.Verify(p => p.CalculateParkingCost(It.IsAny<ParkingLotModel>(), It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>()), Times.Never);
    }

    [TestMethod]
    public async Task GetReservationCostEstimate_Success_ReturnsPrice()
    {
        // Arrange
        var dto = MakeDto();
        var lotDto = new ReadParkingLotDto { Id = LotId, Tariff = 5, Capacity = 0 };
        var userPlate = new UserPlateModel { UserId = UserId, LicensePlateNumber = Plate.ToUpper() };

        _mockParkingLotsService.Setup(s => s.GetParkingLotByIdAsync(LotId))
            .ReturnsAsync(ServiceResult<ReadParkingLotDto>.Ok(lotDto));
        _mockUserPlatesService.Setup(s => s.GetUserPlatesByUserId(UserId)).ReturnsAsync(new GetUserPlateListResult.Success(new List<UserPlateModel> { userPlate }));
        _mockReservationsRepo.Setup(s => s.GetByParkingLotId(LotId)).ReturnsAsync(new List<ReservationModel>());
        _mockPricingService.Setup(p => p.CalculateParkingCost(It.Is<ParkingLotModel>(m => m.Id == LotId), dto.StartDate, dto.EndDate))
            .Returns(new CalculatePriceResult.Success(12m, 2, 0));

        // Act
        var result = await _service.GetReservationCostEstimate(dto, UserId);

        // Assert
        Assert.IsInstanceOfType(result, typeof(GetReservationCostEstimateResult.Success));
        Assert.AreEqual(12m, ((GetReservationCostEstimateResult.Success)result).EstimatedCost);
    }
}
