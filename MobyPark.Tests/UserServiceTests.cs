using MobyPark.Models;
using MobyPark.Models.Access;
using MobyPark.Models.DataService;
using MobyPark.Services;
using Moq;

namespace MobyPark.Tests;

[TestClass]
public sealed class UserServiceTests
{
    private Mock<IDataAccess>? _mockDataService;
    private Mock<IUserAccess>? _mockUserAccess;
    private UserService? _userService;

    [TestInitialize]
    public void TestInitialize()
    {
        _mockDataService = new Mock<IDataAccess>();
        _mockUserAccess = new Mock<IUserAccess>();

        _mockDataService.Setup(ds => ds.Users).Returns(_mockUserAccess.Object);

        _userService = new UserService(_mockDataService.Object);
    }

    [TestMethod]
    [DataRow("password123")]
    [DataRow("admin!@#")]
    [DataRow("hello world")]
    [DataRow("  spaced  ")]
    [DataRow("")]
    public void HashPassword_ConsistentAndDeterministic(string password)
    {
        // Act
        var hash1 = _userService!.HashPassword(password);
        var hash2 = _userService.HashPassword(password);

        // Assert
        Assert.AreEqual(hash1, hash2);
        Assert.AreNotEqual(password, hash1);
        Assert.IsFalse(string.IsNullOrWhiteSpace(hash1));
    }

    [TestMethod]
    [DataRow("mypassword")]
    [DataRow("anotherSecret")]
    [DataRow("123456")]
    public void VerifyPassword_CorrectPassword_ReturnsTrue(string password)
    {
        // Arrange
        var hash = _userService!.HashPassword(password);

        // Act
        var result = _userService.VerifyPassword(password, hash);

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    [DataRow("mypassword", "wrongpassword")]
    [DataRow("correct123", "wrong123")]
    [DataRow("test", "TEST")] // case-sensitive
    public void VerifyPassword_WrongPassword_ReturnsFalse(string realPassword, string attempt)
    {
        // Arrange
        var hash = _userService!.HashPassword(realPassword);

        // Act
        var result = _userService.VerifyPassword(attempt, hash);

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    [DataRow("user1", "P@ssword1", "Alice")]
    [DataRow("user2", "Pass123$", "Bob")]
    [DataRow("user3", "Abc12345!", "Charlie")]
    [DataRow("user4", "StrongPass1@", "David")]
    [DataRow("user5", "Pa ss1Ab2!", "Eve")]
    [DataRow("user6", "A1b2C3d4$", "Frank")]
    [DataRow("user7", "Aa1!Aa1!", "Grace")]
    [DataRow("user8", "Complex#Password123", "Hannah")]
    [DataRow("user9", "Xy9*Zz8@", "Isaac")]
    [DataRow("user10", "GoodPass1@", "Jack")]
    public async Task CreateUserAsync_ValidInput_CreatesUserModel(string username, string password, string name)
    {
        // Arrange
        _mockUserAccess!
            .Setup(access => access.Create(It.IsAny<UserModel>()))
            .Callback<UserModel>(model => _ = model)
            .ReturnsAsync(true);

        // Act
        var result = await _userService!.CreateUserAsync(username, password, name);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(username, result.Username);
        Assert.AreEqual(name, result.Name);
        Assert.AreEqual("USER", result.Role);
        Assert.IsTrue(result.Active);
        Assert.IsFalse(string.IsNullOrWhiteSpace(result.Password));
        Assert.IsTrue(result.CreatedAt <= DateTime.UtcNow);

        // Verify hash matches password
        Assert.IsTrue(_userService.VerifyPassword(password, result.Password));

        // Verify persistence call
        _mockUserAccess.Verify(u => u.Create(It.Is<UserModel>(usr =>
            usr.Username == username &&
            usr.Name == name &&
            usr.Role == "USER"
        )), Times.Once);
    }


    [TestMethod]
    [DataRow(null, "password", "Name")]
    [DataRow("", "password", "Name")]
    [DataRow("user", null, "Name")]
    [DataRow("user", "", "Name")]
    [DataRow("user", "password", null)]
    [DataRow("user", "password", "")]
    public async Task CreateUserAsync_InvalidInput_ThrowsArgumentException(string username, string password, string name)
    {
        await Assert.ThrowsExceptionAsync<ArgumentException>(async () =>
            await _userService!.CreateUserAsync(username, password, name));

        _mockUserAccess!.Verify(access => access.Create(It.IsAny<UserModel>()), Times.Never);
    }

    [TestMethod]
    [DataRow("user1", "short", "Name1")]
    [DataRow("user2", "        ", "Name2")]
    [DataRow("user3", "alllowercase1", "Name3")]
    [DataRow("user4", "ALLUPPERCASE1", "Name4")]
    [DataRow("user5", "NoNumbersHere", "Name5")]
    [DataRow("user6", "12345678", "Name6")]
    [DataRow("user7", "Password!", "Name7")]
    [DataRow("user8", "PASS1234", "Name8")]
    [DataRow("user9", "pass1234", "Name9")]
    public async Task CreateUserAsync_WeakOrWhitespacePassword_ThrowsArgumentException(string username, string password, string name)
    {
        await Assert.ThrowsExceptionAsync<ArgumentException>(async () =>
            await _userService!.CreateUserAsync(username, password, name));

        _mockUserAccess!.Verify(access => access.Create(It.IsAny<UserModel>()), Times.Never);
    }

    [TestMethod]
    [DataRow(null, "Password1", "Alice")]
    [DataRow("", "Password1", "Bob")]
    [DataRow("   ", "Password1", "Charlie")]
    [DataRow("user1", "Password1", null)]
    [DataRow("user2", "Password1", "")]
    [DataRow("user3", "Password1", "   ")]
    [DataRow("uuuuuuuuuuuuuuuuuuuuuuuuuuuuuuuuuuuuuuuuuu", "Password1", "LongName")]  // very long username
    [DataRow("user4", "Password1", "NNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNN")]     // very long name
    public async Task CreateUserAsync_InvalidUsernameOrName_ThrowsArgumentException(string username, string password, string name)
    {
        await Assert.ThrowsExceptionAsync<ArgumentException>(async () =>
            await _userService!.CreateUserAsync(username, password, name));

        _mockUserAccess!.Verify(access => access.Create(It.IsAny<UserModel>()), Times.Never);
    }
}

