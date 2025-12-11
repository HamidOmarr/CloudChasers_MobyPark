using MobyPark.DTOs.LicensePlate.Request;
using MobyPark.Models;
using MobyPark.Models.Repositories.Interfaces;
using MobyPark.Services;
using MobyPark.Services.Results.LicensePlate;
using Moq;

namespace MobyPark.Tests;

[TestClass]
public class LicensePlateServiceTests
{
    #region Setup
    private Mock<ILicensePlateRepository> _mockLicensePlatesRepo = null!;
    private LicensePlateService _licensePlateService = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _mockLicensePlatesRepo = new Mock<ILicensePlateRepository>();
        _licensePlateService = new LicensePlateService(_mockLicensePlatesRepo.Object);
    }

    #endregion

    
    #region Create

    [TestMethod]
    [DataRow("AB-12-CD")]
    [DataRow("WX-99-YZ")]
    public async Task CreateLicensePlate_ValidNewPlate_ReturnsSuccess(string plateNumber)
    {
        // Arrange
        var dto = new CreateLicensePlateDto { LicensePlate = plateNumber };
        string expectedNormalized = plateNumber.ToUpper();

        _mockLicensePlatesRepo.Setup(licensePlateRepo => licensePlateRepo.GetByNumber(expectedNormalized)).ReturnsAsync((LicensePlateModel?)null);
        _mockLicensePlatesRepo.Setup(licensePlateRepo => licensePlateRepo.Create(
                It.Is<LicensePlateModel>(plateModel => plateModel.LicensePlateNumber == expectedNormalized))).ReturnsAsync(true);

        // Act
        var result = await _licensePlateService.CreateLicensePlate(dto);

        // Assert
        Assert.IsInstanceOfType(result, typeof(CreateLicensePlateResult.Success));
        var successResult = (CreateLicensePlateResult.Success)result;
        Assert.AreEqual(expectedNormalized, successResult.plate.LicensePlateNumber);
        _mockLicensePlatesRepo.Verify(licensePlateRepo => licensePlateRepo.Create(It.IsAny<LicensePlateModel>()), Times.Once);
    }

    [TestMethod]
    [DataRow("AA-11-BB")]
    [DataRow("XX-99-XX")]
    public async Task CreateLicensePlate_PlateAlreadyExists_ReturnsAlreadyExists(string plateNumber)
    {
        // Arrange
        var dto = new CreateLicensePlateDto { LicensePlate = plateNumber };
        string expectedNormalized = plateNumber.ToUpper();
        var existingPlate = new LicensePlateModel { LicensePlateNumber = expectedNormalized };

        _mockLicensePlatesRepo.Setup(licensePlateRepo => licensePlateRepo.GetByNumber(expectedNormalized)).ReturnsAsync(existingPlate);

        // Act
        var result = await _licensePlateService.CreateLicensePlate(dto);

        // Assert
        Assert.IsInstanceOfType(result, typeof(CreateLicensePlateResult.AlreadyExists));
        _mockLicensePlatesRepo.Verify(licensePlateRepo => licensePlateRepo.Create(It.IsAny<LicensePlateModel>()), Times.Never);
    }

    [TestMethod]
    [DataRow("DB-00-XX")]
    public async Task CreateLicensePlate_DbInsertionFails_ReturnsError(string plateNumber)
    {
        // Arrange
        var dto = new CreateLicensePlateDto { LicensePlate = plateNumber };
        string expectedNormalized = plateNumber.ToUpper();

        _mockLicensePlatesRepo.Setup(licensePlateRepo => licensePlateRepo.GetByNumber(expectedNormalized)).ReturnsAsync((LicensePlateModel?)null);
        _mockLicensePlatesRepo.Setup(licensePlateRepo => licensePlateRepo.Create(It.IsAny<LicensePlateModel>())).ReturnsAsync(false);

        // Act
        var result = await _licensePlateService.CreateLicensePlate(dto);

        // Assert
        Assert.IsInstanceOfType(result, typeof(CreateLicensePlateResult.Error));
        StringAssert.Contains(((CreateLicensePlateResult.Error)result).Message, "Database insertion failed");
    }

    [TestMethod]
    [DataRow("TH-42-ER")]
    public async Task CreateLicensePlate_RepositoryThrows_ReturnsError(string plateNumber)
    {
        // Arrange
        var dto = new CreateLicensePlateDto { LicensePlate = plateNumber };
        string expectedNormalized = plateNumber.ToUpper();

        _mockLicensePlatesRepo.Setup(licensePlateRepo => licensePlateRepo.GetByNumber(expectedNormalized)).ReturnsAsync((LicensePlateModel?)null);
        _mockLicensePlatesRepo.Setup(licensePlateRepo => licensePlateRepo.Create(It.IsAny<LicensePlateModel>())).ThrowsAsync(new InvalidOperationException("DB connection error"));

        // Act
        var result = await _licensePlateService.CreateLicensePlate(dto);

        // Assert
        Assert.IsInstanceOfType(result, typeof(CreateLicensePlateResult.Error));
        StringAssert.Contains(((CreateLicensePlateResult.Error)result).Message, "DB connection error");
    }

    #endregion

    #region GetBy

    [TestMethod]
    [DataRow("FOUND-ME")]
    [DataRow("PLATE-456")]
    public async Task GetByLicensePlate_PlateExists_ReturnsSuccess(string plateNumber)
    {
        // Arrange
        string expectedNormalized = plateNumber.ToUpper();
        var expectedPlate = new LicensePlateModel { LicensePlateNumber = expectedNormalized };

        _mockLicensePlatesRepo.Setup(licensePlateRepo => licensePlateRepo.GetByNumber(expectedNormalized))
            .ReturnsAsync(expectedPlate);

        // Act
        var result = await _licensePlateService.GetByLicensePlate(plateNumber);

        // Assert
        Assert.IsInstanceOfType(result, typeof(GetLicensePlateResult.Success));
        var successResult = (GetLicensePlateResult.Success)result;
        Assert.AreEqual(expectedNormalized, successResult.Plate.LicensePlateNumber);
        _mockLicensePlatesRepo.Verify(licensePlateRepo => licensePlateRepo.GetByNumber(expectedNormalized), Times.Once);
    }

    [TestMethod]
    [DataRow("not-found")]
    [DataRow("MISSING")]
    public async Task GetByLicensePlate_PlateNotFound_ReturnsNotFound(string plateNumber)
    {
        // Arrange
        string expectedNormalized = plateNumber.ToUpper();

        _mockLicensePlatesRepo.Setup(licensePlateRepo => licensePlateRepo.GetByNumber(expectedNormalized)).ReturnsAsync((LicensePlateModel?)null);

        // Act
        var result = await _licensePlateService.GetByLicensePlate(plateNumber);

        // Assert
        Assert.IsInstanceOfType(result, typeof(GetLicensePlateResult.NotFound));
        _mockLicensePlatesRepo.Verify(licensePlateRepo => licensePlateRepo.GetByNumber(expectedNormalized), Times.Once);
    }

    #endregion
}
