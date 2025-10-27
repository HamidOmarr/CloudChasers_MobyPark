using MobyPark.Models;
using MobyPark.Models.Repositories;
using MobyPark.Models.Repositories.Interfaces;
using MobyPark.Services;
using MobyPark.Services.Results.UserPlate;
using Moq;
using System.Linq.Expressions;

namespace MobyPark.Tests;

[TestClass]
public sealed class UserPlateServiceTests
{
    #region Setup
    private Mock<IUserPlateRepository> _mockUserPlatesRepo = null!;
    private UserPlateService _userPlateService = null!;

    private const long DefaultUserId = 1L;
    private const string Plate1 = "AB-12-CD";
    private const string Plate2 = "WX-99-YZ";

    [TestInitialize]
    public void TestInitialize()
    {
        _mockUserPlatesRepo = new Mock<IUserPlateRepository>();
        _userPlateService = new UserPlateService(_mockUserPlatesRepo.Object);
    }

    #endregion

    #region AddLicensePlateToUser

    [TestMethod]
    [DataRow(DefaultUserId, Plate1)]
    public async Task AddLicensePlateToUser_FirstPlate_CreatesAsPrimary(long userId, string plate)
    {
        // Arrange
        string normalizedPlate = plate.ToUpper();
        long newId = 101L;

        _mockUserPlatesRepo.Setup(userPlateRepo => userPlateRepo.GetByUserIdAndPlate(userId, normalizedPlate)).ReturnsAsync((UserPlateModel?)null);
        _mockUserPlatesRepo.Setup(userPlateRepo => userPlateRepo.GetPlatesByUserId(userId)).ReturnsAsync(new List<UserPlateModel>());
        _mockUserPlatesRepo.Setup(userPlateRepo => userPlateRepo.CreateWithId(
            It.Is<UserPlateModel>(userPlate => userPlate.UserId == userId && userPlate.LicensePlateNumber == normalizedPlate && userPlate.IsPrimary))).ReturnsAsync((true, newId));

        // Act
        var result = await _userPlateService.AddLicensePlateToUser(userId, plate);

        // Assert
        Assert.IsInstanceOfType(result, typeof(CreateUserPlateResult.Success));
        var successResult = (CreateUserPlateResult.Success)result;
        Assert.AreEqual(newId, successResult.Plate.Id);
        Assert.IsTrue(successResult.Plate.IsPrimary);
    }

    [TestMethod]
    [DataRow(DefaultUserId, Plate2)]
    public async Task AddLicensePlateToUser_SecondPlate_CreatesAsNonPrimary(long userId, string plate)
    {
        // Arrange
        string normalizedPlate = plate.ToUpper();
        long newId = 102L;
        var existingPlate = new UserPlateModel { Id = 101L, UserId = userId, LicensePlateNumber = Plate1, IsPrimary = true };

        _mockUserPlatesRepo.Setup(userPlateRepo => userPlateRepo.GetByUserIdAndPlate(userId, normalizedPlate))
            .ReturnsAsync((UserPlateModel?)null);
        _mockUserPlatesRepo.Setup(userPlateRepo => userPlateRepo.GetPlatesByUserId(userId))
            .ReturnsAsync([existingPlate]);
        _mockUserPlatesRepo.Setup(userPlateRepo => userPlateRepo.CreateWithId(
                It.Is<UserPlateModel>(userPlate => userPlate.UserId == userId && userPlate.LicensePlateNumber == normalizedPlate && !userPlate.IsPrimary))).ReturnsAsync((true, newId));

        // Act
        var result = await _userPlateService.AddLicensePlateToUser(userId, plate);

        // Assert
        Assert.IsInstanceOfType(result, typeof(CreateUserPlateResult.Success));
        var successResult = (CreateUserPlateResult.Success)result;
        Assert.AreEqual(newId, successResult.Plate.Id);
        Assert.IsFalse(successResult.Plate.IsPrimary);
    }

    [TestMethod]
    [DataRow(DefaultUserId, Plate1)]
    public async Task AddLicensePlateToUser_AlreadyExists_ReturnsAlreadyExists(long userId, string plate)
    {
        // Arrange
        string normalizedPlate = plate.ToUpper();
        var existingPlate = new UserPlateModel { Id = 101L, UserId = userId, LicensePlateNumber = normalizedPlate };

        _mockUserPlatesRepo.Setup(userPlateRepo => userPlateRepo.GetByUserIdAndPlate(userId, normalizedPlate))
            .ReturnsAsync(existingPlate); // Link exists

        // Act
        var result = await _userPlateService.AddLicensePlateToUser(userId, plate);

        // Assert
        Assert.IsInstanceOfType(result, typeof(CreateUserPlateResult.AlreadyExists));
        _mockUserPlatesRepo.Verify(userPlateRepo => userPlateRepo.CreateWithId(It.IsAny<UserPlateModel>()), Times.Never);
    }

    [TestMethod]
    [DataRow(DefaultUserId, Plate1)]
    public async Task AddLicensePlateToUser_DbInsertionFails_ReturnsError(long userId, string plate)
    {
        // Arrange
        string normalizedPlate = plate.ToUpper();

        _mockUserPlatesRepo.Setup(userPlateRepo => userPlateRepo.GetByUserIdAndPlate(userId, normalizedPlate))
            .ReturnsAsync((UserPlateModel?)null);
        _mockUserPlatesRepo.Setup(userPlateRepo => userPlateRepo.GetPlatesByUserId(userId))
            .ReturnsAsync(new List<UserPlateModel>());
        _mockUserPlatesRepo.Setup(userPlateRepo => userPlateRepo.CreateWithId(It.IsAny<UserPlateModel>()))
            .ReturnsAsync((false, 0L)); // DB fails

        // Act
        var result = await _userPlateService.AddLicensePlateToUser(userId, plate);

        // Assert
        Assert.IsInstanceOfType(result, typeof(CreateUserPlateResult.Error));
        StringAssert.Contains(((CreateUserPlateResult.Error)result).Message, "Database insertion failed");
    }

    #endregion

    #region GetById

    [TestMethod]
    [DataRow(101L)]
    public async Task GetUserPlateById_Found_ReturnsSuccess(long id)
    {
        // Arrange
        var userPlate = new UserPlateModel { Id = id, UserId = DefaultUserId, LicensePlateNumber = Plate1 };
        _mockUserPlatesRepo.Setup(userPlateRepo => userPlateRepo.GetById<UserPlateModel>(id))
            .ReturnsAsync(userPlate);

        // Act
        var result = await _userPlateService.GetUserPlateById(id);

        // Assert
        Assert.IsInstanceOfType(result, typeof(GetUserPlateResult.Success));
        Assert.AreEqual(id, ((GetUserPlateResult.Success)result).Plate.Id);
    }

    [TestMethod]
    [DataRow(999L)]
    public async Task GetUserPlateById_NotFound_ReturnsNotFound(long id)
    {
        // Arrange
        _mockUserPlatesRepo.Setup(userPlateRepo => userPlateRepo.GetById<UserPlateModel>(id))
            .ReturnsAsync((UserPlateModel?)null);

        // Act
        var result = await _userPlateService.GetUserPlateById(id);

        // Assert
        Assert.IsInstanceOfType(result, typeof(GetUserPlateResult.NotFound));
    }

    #endregion

    #region GetByUserId

    [TestMethod]
    [DataRow(DefaultUserId, 2)]
    public async Task GetUserPlatesByUserId_Found_ReturnsSuccessList(long userId, int count)
    {
        // Arrange
        var plates = Enumerable.Range(1, count)
            .Select(i => new UserPlateModel { Id = i, UserId = userId, LicensePlateNumber = $"PLATE-{i}" })
            .ToList();
        _mockUserPlatesRepo.Setup(userPlateRepo => userPlateRepo.GetPlatesByUserId(userId))
            .ReturnsAsync(plates);

        // Act
        var result = await _userPlateService.GetUserPlatesByUserId(userId);

        // Assert
        Assert.IsInstanceOfType(result, typeof(GetUserPlateListResult.Success));
        Assert.AreEqual(count, ((GetUserPlateListResult.Success)result).Plates.Count);
    }

    [TestMethod]
    [DataRow(99L)]
    public async Task GetUserPlatesByUserId_NotFound_ReturnsNotFound(long userId)
    {
        // Arrange
        _mockUserPlatesRepo.Setup(userPlateRepo => userPlateRepo.GetPlatesByUserId(userId))
            .ReturnsAsync(new List<UserPlateModel>());

        // Act
        var result = await _userPlateService.GetUserPlatesByUserId(userId);

        // Assert
        Assert.IsInstanceOfType(result, typeof(GetUserPlateListResult.NotFound));
    }

    #endregion

    #region GetByPlate

    [TestMethod]
    [DataRow(Plate1, 1)]
    [DataRow(Plate2, 2)]
    public async Task GetUserPlatesByPlate_Found_ReturnsSuccessList(string plate, int count)
    {
        // Arrange
        string normalizedPlate = plate.ToUpper();
        var plates = Enumerable
            .Range(1, count)
            .Select(i => new UserPlateModel { Id = i, UserId = i, LicensePlateNumber = normalizedPlate })
            .ToList();
        _mockUserPlatesRepo.Setup(userPlateRepo => userPlateRepo.GetPlatesByPlate(normalizedPlate)).ReturnsAsync(plates);

        // Act
        var result = await _userPlateService.GetUserPlatesByPlate(plate);

        // Assert
        Assert.IsInstanceOfType(result, typeof(GetUserPlateListResult.Success));
        Assert.AreEqual(count, ((GetUserPlateListResult.Success)result).Plates.Count);
    }

    [TestMethod]
    [DataRow("NO-00-XX")]
    public async Task GetUserPlatesByPlate_NotFound_ReturnsNotFound(string plate)
    {
        // Arrange
        string normalizedPlate = plate.ToUpper();
        _mockUserPlatesRepo.Setup(userPlateRepo => userPlateRepo.GetPlatesByPlate(normalizedPlate)).ReturnsAsync(new List<UserPlateModel>());

        // Act
        var result = await _userPlateService.GetUserPlatesByPlate(plate);

        // Assert
        Assert.IsInstanceOfType(result, typeof(GetUserPlateListResult.NotFound));
    }

    #endregion

    #region GetPrimaryByUserId

    [TestMethod]
    [DataRow(DefaultUserId)]
    public async Task GetPrimaryUserPlateByUserId_Found_ReturnsSuccess(long userId)
    {
        // Arrange
        var primaryPlate = new UserPlateModel { Id = 101L, UserId = userId, LicensePlateNumber = Plate1, IsPrimary = true };
        _mockUserPlatesRepo.Setup(userPlateRepo => userPlateRepo.GetPrimaryPlateByUserId(userId)).ReturnsAsync(primaryPlate);

        // Act
        var result = await _userPlateService.GetPrimaryUserPlateByUserId(userId);

        // Assert
        Assert.IsInstanceOfType(result, typeof(GetUserPlateResult.Success));
        Assert.IsTrue(((GetUserPlateResult.Success)result).Plate.IsPrimary);
    }

    [TestMethod]
    [DataRow(99L)]
    public async Task GetPrimaryUserPlateByUserId_NotFound_ReturnsNotFound(long userId)
    {
        // Arrange
        _mockUserPlatesRepo.Setup(userPlateRepo => userPlateRepo.GetPrimaryPlateByUserId(userId)).ReturnsAsync((UserPlateModel?)null);

        // Act
        var result = await _userPlateService.GetPrimaryUserPlateByUserId(userId);

        // Assert
        Assert.IsInstanceOfType(result, typeof(GetUserPlateResult.NotFound));
    }

    #endregion

    #region GetByUserIdAndPlate

    [TestMethod]
    [DataRow(DefaultUserId, Plate1)]
    public async Task GetUserPlateByUserIdAndPlate_Found_ReturnsSuccess(long userId, string plate)
    {
        // Arrange
        string normalizedPlate = plate.ToUpper();
        var userPlate = new UserPlateModel { Id = 101L, UserId = userId, LicensePlateNumber = normalizedPlate };
        _mockUserPlatesRepo.Setup(userPlateRepo => userPlateRepo.GetByUserIdAndPlate(userId, normalizedPlate)).ReturnsAsync(userPlate);

        // Act
        var result = await _userPlateService.GetUserPlateByUserIdAndPlate(userId, plate);

        // Assert
        Assert.IsInstanceOfType(result, typeof(GetUserPlateResult.Success));
        Assert.AreEqual(normalizedPlate, ((GetUserPlateResult.Success)result).Plate.LicensePlateNumber);
    }

    [TestMethod]
    [DataRow(DefaultUserId, "NOT-LINKED")]
    [DataRow(99L, Plate1)]
    public async Task GetUserPlateByUserIdAndPlate_NotFound_ReturnsNotFound(long userId, string plate)
    {
        // Arrange
        string normalizedPlate = plate.ToUpper();
        _mockUserPlatesRepo.Setup(userPlateRepo => userPlateRepo.GetByUserIdAndPlate(userId, normalizedPlate)).ReturnsAsync((UserPlateModel?)null);

        // Act
        var result = await _userPlateService.GetUserPlateByUserIdAndPlate(userId, plate);

        // Assert
        Assert.IsInstanceOfType(result, typeof(GetUserPlateResult.NotFound));
    }

    #endregion

    #region GetAll

    [TestMethod]
    public async Task GetAllUserPlates_Found_ReturnsSuccessList()
    {
        // Arrange
        var plates = new List<UserPlateModel>
        {
            new() { Id = 1, UserId = 1, LicensePlateNumber = Plate1 },
            new() { Id = 2, UserId = 2, LicensePlateNumber = Plate2 }
        };
        _mockUserPlatesRepo.Setup(userPlateRepo => userPlateRepo.GetAll()).ReturnsAsync(plates);

        // Act
        var result = await _userPlateService.GetAllUserPlates();

        // Assert
        Assert.IsInstanceOfType(result, typeof(GetUserPlateListResult.Success));
        Assert.AreEqual(2, ((GetUserPlateListResult.Success)result).Plates.Count);
    }

    [TestMethod]
    public async Task GetAllUserPlates_NotFound_ReturnsNotFound()
    {
        // Arrange
        _mockUserPlatesRepo.Setup(userPlateRepo => userPlateRepo.GetAll()).ReturnsAsync(new List<UserPlateModel>());

        // Act
        var result = await _userPlateService.GetAllUserPlates();

        // Assert
        Assert.IsInstanceOfType(result, typeof(GetUserPlateListResult.NotFound));
    }

    #endregion

    #region Exists

    [TestMethod]
    [DataRow("id", new[] { "101" })]
    [DataRow("userplate", new[] { "1", Plate1 })]
    public async Task UserPlateExists_WhenExists_ReturnsExistsResult(string checkBy, string[] value)
    {
        // Arrange
        _mockUserPlatesRepo.Setup(userPlateRepo => userPlateRepo.Exists(It.IsAny<Expression<Func<UserPlateModel, bool>>>())).ReturnsAsync(true);

        // Act
        var result = await _userPlateService.UserPlateExists(checkBy, value);

        // Assert
        Assert.IsInstanceOfType(result, typeof(UserPlateExistsResult.Exists));
        _mockUserPlatesRepo.Verify(userPlateRepo => userPlateRepo.Exists(It.IsAny<Expression<Func<UserPlateModel, bool>>>()), Times.Once);
    }

    [TestMethod]
    [DataRow("id", new[] { "999" })]
    [DataRow("userplate", new[] { "1", "NON-EXISTENT" })]
    public async Task UserPlateExists_WhenNotExists_ReturnsNotExistsResult(string checkBy, string[] value)
    {
        // Arrange
        _mockUserPlatesRepo.Setup(userPlateRepo => userPlateRepo.Exists(It.IsAny<Expression<Func<UserPlateModel, bool>>>())).ReturnsAsync(false);

        // Act
        var result = await _userPlateService.UserPlateExists(checkBy, value);

        // Assert
        Assert.IsInstanceOfType(result, typeof(UserPlateExistsResult.NotExists));
        _mockUserPlatesRepo.Verify(userPlateRepo => userPlateRepo.Exists(It.IsAny<Expression<Func<UserPlateModel, bool>>>()), Times.Once);
    }

    [TestMethod]
    [DataRow("id", new[] { "abc" })]
    [DataRow("userplate", new[] { "1" })]
    [DataRow("userplate", new[] { "abc", Plate1 })]
    [DataRow("invalidCheck", new[] { "1" })]
    [DataRow("id", new[] { " " })]
    [DataRow("id", new string[] { })]
    public async Task UserPlateExists_InvalidInput_ReturnsInvalidInput(string checkBy, string[] value)
    {
        // Act
        var result = await _userPlateService.UserPlateExists(checkBy, value);

        // Assert
        Assert.IsInstanceOfType(result, typeof(UserPlateExistsResult.InvalidInput));
        _mockUserPlatesRepo.Verify(userPlateRepo => userPlateRepo.Exists(It.IsAny<Expression<Func<UserPlateModel, bool>>>()), Times.Never);
    }

    #endregion

    #region Count

    [TestMethod]
    [DataRow(0)]
    [DataRow(25)]
    public async Task GetUserPlatesCount_ReturnsCount(int count)
    {
        // Arrange
        _mockUserPlatesRepo.Setup(userPlateRepo => userPlateRepo.Count()).ReturnsAsync(count);

        // Act
        var result = await _userPlateService.GetUserPlatesCount();

        // Assert
        Assert.AreEqual(count, result);
    }

    #endregion

    #region ChangePrimaryUserPlate

    [TestMethod]
    [DataRow(DefaultUserId, Plate2)]
    public async Task ChangePrimaryUserPlate_Success_ReturnsSuccess(long userId, string newPrimaryPlate)
    {
        // Arrange
        string normalizedNewPrimary = newPrimaryPlate.ToUpper();
        var currentPrimary = new UserPlateModel { Id = 101L, UserId = userId, LicensePlateNumber = Plate1, IsPrimary = true };
        var newPrimary = new UserPlateModel { Id = 102L, UserId = userId, LicensePlateNumber = normalizedNewPrimary, IsPrimary = false };

        _mockUserPlatesRepo.Setup(userPlateRepo => userPlateRepo.GetPrimaryPlateByUserId(userId)).ReturnsAsync(currentPrimary);
        _mockUserPlatesRepo.Setup(userPlateRepo => userPlateRepo.GetByUserIdAndPlate(userId, normalizedNewPrimary)).ReturnsAsync(newPrimary);

        _mockUserPlatesRepo.Setup(userPlateRepo => userPlateRepo.Update(
                It.Is<UserPlateModel>(userPlate => userPlate.Id == currentPrimary.Id && !userPlate.IsPrimary),
                It.IsAny<UserPlateModel>())).ReturnsAsync(true);
        _mockUserPlatesRepo.Setup(userPlateRepo => userPlateRepo.Update(
                It.Is<UserPlateModel>(userPlate => userPlate.Id == newPrimary.Id && userPlate.IsPrimary),
                It.IsAny<UserPlateModel>())).ReturnsAsync(true);

        // Mock GetById for the internal UpdateUserPlate calls
        _mockUserPlatesRepo.Setup(userPlateRepo => userPlateRepo.GetById<UserPlateModel>(currentPrimary.Id)).ReturnsAsync(currentPrimary);
        _mockUserPlatesRepo.Setup(userPlateRepo => userPlateRepo.GetById<UserPlateModel>(newPrimary.Id)).ReturnsAsync(newPrimary);


        // Act
        var result = await _userPlateService.ChangePrimaryUserPlate(userId, newPrimaryPlate);

        // Assert
        Assert.IsInstanceOfType(result, typeof(UpdateUserPlateResult.Success));
        Assert.IsTrue(((UpdateUserPlateResult.Success)result).Plate.IsPrimary);
        Assert.AreEqual(normalizedNewPrimary, ((UpdateUserPlateResult.Success)result).Plate.LicensePlateNumber);
        Assert.IsFalse(currentPrimary.IsPrimary);
    }

    [TestMethod]
    [DataRow(99L, Plate1)]
    public async Task ChangePrimaryUserPlate_CurrentPrimaryNotFound_ReturnsInvalidOperation(long userId, string newPrimaryPlate)
    {
        // Arrange
        _mockUserPlatesRepo.Setup(userPlateRepo => userPlateRepo.GetPrimaryPlateByUserId(userId)).ReturnsAsync((UserPlateModel?)null);

        // Act
        var result = await _userPlateService.ChangePrimaryUserPlate(userId, newPrimaryPlate);

        // Assert
        Assert.IsInstanceOfType(result, typeof(UpdateUserPlateResult.InvalidOperation));
        StringAssert.Contains(((UpdateUserPlateResult.InvalidOperation)result).Message, "no primary plate");
    }

    [TestMethod]
    [DataRow(DefaultUserId, Plate1)]
    public async Task ChangePrimaryUserPlate_AlreadyPrimary_ReturnsInvalidOperation(long userId, string newPrimaryPlate)
    {
        // Arrange
        string normalizedPlate = newPrimaryPlate.ToUpper();
        var currentPrimary = new UserPlateModel { Id = 101L, UserId = userId, LicensePlateNumber = normalizedPlate, IsPrimary = true };

        _mockUserPlatesRepo.Setup(userPlateRepo => userPlateRepo.GetPrimaryPlateByUserId(userId)).ReturnsAsync(currentPrimary);

        // Act
        var result = await _userPlateService.ChangePrimaryUserPlate(userId, newPrimaryPlate);

        // Assert
        Assert.IsInstanceOfType(result, typeof(UpdateUserPlateResult.InvalidOperation));
        StringAssert.Contains(((UpdateUserPlateResult.InvalidOperation)result).Message, "already the primary plate");
    }

    [TestMethod]
    [DataRow(DefaultUserId, "NO-PL-00")]
    public async Task ChangePrimaryUserPlate_NewPrimaryNotFoundForUser_ReturnsNotFound(long userId, string newPrimaryPlate)
    {
        // Arrange
        string normalizedNewPrimary = newPrimaryPlate.ToUpper();
        var currentPrimary = new UserPlateModel { Id = 101L, UserId = userId, LicensePlateNumber = Plate1, IsPrimary = true };

        _mockUserPlatesRepo.Setup(userPlateRepo => userPlateRepo.GetPrimaryPlateByUserId(userId))
            .ReturnsAsync(currentPrimary);
        _mockUserPlatesRepo.Setup(userPlateRepo => userPlateRepo.GetByUserIdAndPlate(userId, normalizedNewPrimary))
            .ReturnsAsync((UserPlateModel?)null);

        // Act
        var result = await _userPlateService.ChangePrimaryUserPlate(userId, newPrimaryPlate);

        // Assert
        Assert.IsInstanceOfType(result, typeof(UpdateUserPlateResult.NotFound));
    }

    [TestMethod]
    [DataRow(DefaultUserId, Plate2)]
    public async Task ChangePrimaryUserPlate_UpdateNewFails_RollsBackAndReturnsError(long userId, string newPrimaryPlate)
    {
        // Arrange
        string normalizedNewPrimary = newPrimaryPlate.ToUpper();
        var currentPrimary = new UserPlateModel { Id = 101L, UserId = userId, LicensePlateNumber = Plate1, IsPrimary = true };
        var newPrimary = new UserPlateModel { Id = 102L, UserId = userId, LicensePlateNumber = normalizedNewPrimary, IsPrimary = false };

        _mockUserPlatesRepo.Setup(userPlateRepo => userPlateRepo.GetPrimaryPlateByUserId(userId)).ReturnsAsync(currentPrimary);
        _mockUserPlatesRepo.Setup(userPlateRepo => userPlateRepo.GetByUserIdAndPlate(userId, normalizedNewPrimary)).ReturnsAsync(newPrimary);
        _mockUserPlatesRepo.Setup(userPlateRepo => userPlateRepo.GetById<UserPlateModel>(currentPrimary.Id)).ReturnsAsync(currentPrimary);
        _mockUserPlatesRepo.Setup(userPlateRepo => userPlateRepo.GetById<UserPlateModel>(newPrimary.Id)).ReturnsAsync(newPrimary);
        _mockUserPlatesRepo.Setup(userPlateRepo => userPlateRepo.Update(It.Is<UserPlateModel>(userPlate => userPlate.Id == currentPrimary.Id && !userPlate.IsPrimary), It.IsAny<UserPlateModel>())).ReturnsAsync(true);
        _mockUserPlatesRepo.Setup(userPlateRepo => userPlateRepo.Update(It.Is<UserPlateModel>(userPlate => userPlate.Id == newPrimary.Id && userPlate.IsPrimary), It.IsAny<UserPlateModel>())).ReturnsAsync(false);
        _mockUserPlatesRepo.Setup(userPlateRepo => userPlateRepo.Update(It.Is<UserPlateModel>(userPlate => userPlate.Id == currentPrimary.Id && userPlate.IsPrimary), It.IsAny<UserPlateModel>())).ReturnsAsync(true);

        // Act
        var result = await _userPlateService.ChangePrimaryUserPlate(userId, newPrimaryPlate);

        // Assert
        Assert.IsInstanceOfType(result, typeof(UpdateUserPlateResult.Error));
        _mockUserPlatesRepo.Verify(userPlateRepo => userPlateRepo.Update(
            It.Is<UserPlateModel>(userPlate => userPlate.Id == currentPrimary.Id && userPlate.IsPrimary),
            It.IsAny<UserPlateModel>()), Times.Exactly(2));
        Assert.IsTrue(currentPrimary.IsPrimary);
    }

    #endregion

    #region Remove

    [TestMethod]
    [DataRow(DefaultUserId, Plate2)]
    public async Task RemoveUserPlate_RemovingNonPrimary_SoftDeletesAndReturnsSuccess(long userId, string plateToRemove)
    {
        // Arrange
        string normalizedPlate = plateToRemove.ToUpper();
        var plate = new UserPlateModel { Id = 102L, UserId = userId, LicensePlateNumber = normalizedPlate, IsPrimary = false };
        var primaryPlate = new UserPlateModel { Id = 101L, UserId = userId, LicensePlateNumber = Plate1, IsPrimary = true };
        var allPlates = new List<UserPlateModel> { primaryPlate, plate };

        _mockUserPlatesRepo.Setup(userPlateRepo => userPlateRepo.GetByUserIdAndPlate(userId, normalizedPlate)).ReturnsAsync(plate);
        _mockUserPlatesRepo.Setup(userPlateRepo => userPlateRepo.GetPlatesByUserId(userId)).ReturnsAsync(allPlates);

        _mockUserPlatesRepo.Setup(userPlateRepo => userPlateRepo.GetById<UserPlateModel>(plate.Id)).ReturnsAsync(plate);
        _mockUserPlatesRepo.Setup(userPlateRepo => userPlateRepo.Update(
                It.Is<UserPlateModel>(userPlate => userPlate.Id == plate.Id && userPlate.UserId == UserPlateRepository.DeletedUserId && !userPlate.IsPrimary),
                It.IsAny<UserPlateModel>()))
            .ReturnsAsync(true);

        // Act
        var result = await _userPlateService.RemoveUserPlate(userId, plateToRemove);

        // Assert
        Assert.IsInstanceOfType(result, typeof(DeleteUserPlateResult.Success));
        _mockUserPlatesRepo.Verify(userPlateRepo => userPlateRepo.Update(
            It.Is<UserPlateModel>(userPlate => userPlate.UserId == UserPlateRepository.DeletedUserId), It.IsAny<UserPlateModel>()), Times.Once);
        _mockUserPlatesRepo.Verify(userPlateRepo => userPlateRepo.Update(
            It.Is<UserPlateModel>(userPlate => userPlate.IsPrimary), It.IsAny<UserPlateModel>()), Times.Never);
    }

    [TestMethod]
    [DataRow(DefaultUserId, Plate1)]
    public async Task RemoveUserPlate_RemovingPrimary_SetsNewPrimaryAndSoftDeletes(long userId, string plateToRemove)
    {
        // Arrange
        string normalizedPlate = plateToRemove.ToUpper();
        var primaryPlate = new UserPlateModel { Id = 101L, UserId = userId, LicensePlateNumber = normalizedPlate, IsPrimary = true };
        var otherPlate = new UserPlateModel { Id = 102L, UserId = userId, LicensePlateNumber = Plate2, IsPrimary = false };
        var allPlates = new List<UserPlateModel> { primaryPlate, otherPlate };

        _mockUserPlatesRepo.Setup(userPlateRepo => userPlateRepo.GetByUserIdAndPlate(userId, normalizedPlate)).ReturnsAsync(primaryPlate);
        _mockUserPlatesRepo.Setup(userPlateRepo => userPlateRepo.GetPlatesByUserId(userId)).ReturnsAsync(allPlates);

        _mockUserPlatesRepo.Setup(userPlateRepo => userPlateRepo.GetById<UserPlateModel>(otherPlate.Id)).ReturnsAsync(otherPlate);
        _mockUserPlatesRepo.Setup(userPlateRepo => userPlateRepo.Update(
                It.Is<UserPlateModel>(userPlate => userPlate.Id == otherPlate.Id && userPlate.IsPrimary), It.IsAny<UserPlateModel>()))
             .Callback<UserPlateModel, object>((plateToUpdate, _) => plateToUpdate.IsPrimary = true).ReturnsAsync(true);

        _mockUserPlatesRepo.Setup(userPlateRepo => userPlateRepo.GetById<UserPlateModel>(primaryPlate.Id)).ReturnsAsync(primaryPlate);
        _mockUserPlatesRepo.Setup(userPlateRepo => userPlateRepo.Update(
                It.Is<UserPlateModel>(userPlate => userPlate.Id == primaryPlate.Id && userPlate.UserId == UserPlateRepository.DeletedUserId && !userPlate.IsPrimary),
                It.IsAny<UserPlateModel>())).ReturnsAsync(true);

        // Act
        var result = await _userPlateService.RemoveUserPlate(userId, plateToRemove);

        // Assert
        Assert.IsInstanceOfType(result, typeof(DeleteUserPlateResult.Success));
        Assert.IsTrue(otherPlate.IsPrimary);
        _mockUserPlatesRepo.Verify(userPlateRepo => userPlateRepo.Update(
            It.Is<UserPlateModel>(userPlate => userPlate.Id == otherPlate.Id && userPlate.IsPrimary),
            It.IsAny<UserPlateModel>()), Times.Once);
        _mockUserPlatesRepo.Verify(userPlateRepo => userPlateRepo.Update(
            It.Is<UserPlateModel>(userPlate => userPlate.UserId == UserPlateRepository.DeletedUserId),
            It.IsAny<UserPlateModel>()), Times.Once);
    }

    [TestMethod]
    [DataRow(DefaultUserId, "XX-99-XX")]
    public async Task RemoveUserPlate_PlateNotFoundForUser_ReturnsNotFound(long userId, string plate)
    {
        // Arrange
        string normalizedPlate = plate.ToUpper();
        _mockUserPlatesRepo.Setup(userPlateRepo => userPlateRepo.GetByUserIdAndPlate(userId, normalizedPlate)).ReturnsAsync((UserPlateModel?)null);

        // Act
        var result = await _userPlateService.RemoveUserPlate(userId, plate);

        // Assert
        Assert.IsInstanceOfType(result, typeof(DeleteUserPlateResult.NotFound));
    }

    [TestMethod]
    [DataRow(DefaultUserId, Plate1)]
    public async Task RemoveUserPlate_CannotRemoveLastPlate_ReturnsInvalidOperation(long userId, string plate)
    {
        // Arrange
        string normalizedPlate = plate.ToUpper();
        var onlyPlate = new UserPlateModel { Id = 101L, UserId = userId, LicensePlateNumber = normalizedPlate, IsPrimary = true };
        var allPlates = new List<UserPlateModel> { onlyPlate };

        _mockUserPlatesRepo.Setup(userPlateRepo => userPlateRepo.GetByUserIdAndPlate(userId, normalizedPlate)).ReturnsAsync(onlyPlate);
        _mockUserPlatesRepo.Setup(userPlateRepo => userPlateRepo.GetPlatesByUserId(userId)).ReturnsAsync(allPlates);

        // Act
        var result = await _userPlateService.RemoveUserPlate(userId, plate);

        // Assert
        Assert.IsInstanceOfType(result, typeof(DeleteUserPlateResult.InvalidOperation));
        StringAssert.Contains(((DeleteUserPlateResult.InvalidOperation)result).Message, "Cannot remove the only license plate");
    }

    [TestMethod]
    [DataRow(DefaultUserId, Plate1)]
    public async Task RemoveUserPlate_SetNewPrimaryFails_ReturnsError(long userId, string plateToRemove)
    {
        // Arrange
        string normalizedPlate = plateToRemove.ToUpper();
        var primaryPlate = new UserPlateModel { Id = 101L, UserId = userId, LicensePlateNumber = normalizedPlate, IsPrimary = true };
        var otherPlate = new UserPlateModel { Id = 102L, UserId = userId, LicensePlateNumber = Plate2, IsPrimary = false };
        var allPlates = new List<UserPlateModel> { primaryPlate, otherPlate };

        _mockUserPlatesRepo.Setup(userPlateRepo => userPlateRepo.GetByUserIdAndPlate(userId, normalizedPlate)).ReturnsAsync(primaryPlate);
        _mockUserPlatesRepo.Setup(userPlateRepo => userPlateRepo.GetPlatesByUserId(userId)).ReturnsAsync(allPlates);

        _mockUserPlatesRepo.Setup(userPlateRepo => userPlateRepo.GetById<UserPlateModel>(otherPlate.Id)).ReturnsAsync(otherPlate);
        _mockUserPlatesRepo.Setup(userPlateRepo => userPlateRepo.Update(
                It.Is<UserPlateModel>(userPlate => userPlate.Id == otherPlate.Id && userPlate.IsPrimary), It.IsAny<UserPlateModel>())).ReturnsAsync(false);

        // Act
        var result = await _userPlateService.RemoveUserPlate(userId, plateToRemove);

        // Assert
        Assert.IsInstanceOfType(result, typeof(DeleteUserPlateResult.Error));
        StringAssert.Contains(((DeleteUserPlateResult.Error)result).Message, "Failed to set new primary plate");
        _mockUserPlatesRepo.Verify(userPlateRepo => userPlateRepo.Update(
            It.Is<UserPlateModel>(userPlate => userPlate.UserId == UserPlateRepository.DeletedUserId), It.IsAny<UserPlateModel>()), Times.Never);
    }

    [TestMethod]
    [DataRow(DefaultUserId, Plate2)]
    public async Task RemoveUserPlate_SoftDeleteFails_ReturnsError(long userId, string plateToRemove)
    {
        // Arrange
        string normalizedPlate = plateToRemove.ToUpper();
        var plate = new UserPlateModel { Id = 102L, UserId = userId, LicensePlateNumber = normalizedPlate, IsPrimary = false };
        var primaryPlate = new UserPlateModel { Id = 101L, UserId = userId, LicensePlateNumber = Plate1, IsPrimary = true };
        var allPlates = new List<UserPlateModel> { primaryPlate, plate };

        _mockUserPlatesRepo.Setup(userPlateRepo => userPlateRepo.GetByUserIdAndPlate(userId, normalizedPlate)).ReturnsAsync(plate);
        _mockUserPlatesRepo.Setup(userPlateRepo => userPlateRepo.GetPlatesByUserId(userId)).ReturnsAsync(allPlates);

        _mockUserPlatesRepo.Setup(userPlateRepo => userPlateRepo.GetById<UserPlateModel>(plate.Id)).ReturnsAsync(plate);
        _mockUserPlatesRepo.Setup(userPlateRepo => userPlateRepo.Update(
                It.Is<UserPlateModel>(userPlate => userPlate.Id == plate.Id && userPlate.UserId == UserPlateRepository.DeletedUserId && !userPlate.IsPrimary),
                It.IsAny<UserPlateModel>())).ReturnsAsync(false);

        // Act
        var result = await _userPlateService.RemoveUserPlate(userId, plateToRemove);

        // Assert
        Assert.IsInstanceOfType(result, typeof(DeleteUserPlateResult.Error));
        StringAssert.Contains(((DeleteUserPlateResult.Error)result).Message, "Failed to soft-delete plate");
    }

    #endregion
}
