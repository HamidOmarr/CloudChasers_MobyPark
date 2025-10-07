using Microsoft.AspNetCore.Identity;
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
    private Mock<SessionService> _mockSessions;
    private UserService? _userService;
    private IPasswordHasher<UserModel> _hasher;
    

    [TestInitialize]
    public void TestInitialize()
    {
        _mockDataService = new Mock<IDataAccess>();
        _mockUserAccess = new Mock<IUserAccess>();
        _hasher = new PasswordHasher<UserModel>();
        _mockSessions    = new Mock<SessionService>();

        _mockDataService.Setup(ds => ds.Users).Returns(_mockUserAccess.Object);

        _userService = new UserService(_mockDataService.Object, _hasher, _mockSessions.Object);
    }

    [TestMethod]
    [DataRow("mypassword")]
    [DataRow("anotherSecret")]
    [DataRow("123456")]
    public void VerifyPassword_CorrectPassword_ReturnsTrue(string password)
    {
        // Arrange
        var user = new UserModel();
        var hash = _hasher.HashPassword(user, password);

        // Act
        var result = _hasher.VerifyHashedPassword(user, hash, password);

        // Assert
        Assert.AreEqual(PasswordVerificationResult.Success, result);
    }

    [TestMethod]
    [DataRow("mypassword", "wrongpassword")]
    [DataRow("correct123", "wrong123")]
    [DataRow("test", "TEST")] // case-sensitive
    public void VerifyPassword_WrongPassword_ReturnsFalse(string realPassword, string attempt)
    {
        // Arrange
        var user = new UserModel();
        var hash = _hasher.HashPassword(user, realPassword);

        // Act
        var result = _hasher.VerifyHashedPassword(user, hash, attempt);

        // Assert
        Assert.AreEqual(PasswordVerificationResult.Failed, result);
    }

    [TestMethod]
    [DataRow("user1", "P@ssword1", "Alice", "alice@gmail.com", "+31612345678", "2001-10-10")]
    [DataRow("user2", "Pass123$", "Bob", "bob@example.com", "+31611112222", "1998-03-15")]
    [DataRow("user3", "Abc12345!", "Charlie", "charlie@example.com", "+31622223333", "1995-07-22")]
    [DataRow("user4", "StrongPass1@", "David", "david@example.com", "+31633334444", "2000-01-05")]
    [DataRow("user5", "Pa ss1Ab2!", "Eve", "eve@example.com", "+31644445555", "1999-09-09")]
    [DataRow("user6", "A1b2C3d4$", "Frank", "frank@example.com", "+31655556666", "1997-12-31")]
    [DataRow("user7", "Aa1!Aa1!", "Grace", "grace@example.com", "+31666667777", "2002-04-18")]
    [DataRow("user8", "Complex#Password123", "Hannah", "hannah@example.com", "+31677778888", "2003-08-25")]
    [DataRow("user9", "Xy9*Zz8@", "Isaac", "isaac@example.com", "+31688889999", "1996-11-11")]
    [DataRow("user10", "GoodPass1@", "Jack", "jack@example.com", "+31699990000", "1994-06-06")]
    public async Task CreateUserAsync_ValidInput_CreatesUserModel(string username, string password, string name, string email, string phone, string birthdayString)
    {
        // Arrange
        var birthday = DateTime.Parse(birthdayString);
        _mockUserAccess!
            .Setup(access => access.Create(It.IsAny<UserModel>()))
            .Callback<UserModel>(model => _ = model)
            .ReturnsAsync(true);

        // Act
        var result = await _userService!.CreateUserAsync(username, password, name, email, phone, birthday);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(username, result.Username);
        Assert.AreEqual(name, result.Name);
        Assert.AreEqual("USER", result.Role);
        Assert.AreEqual(email, result.Email);
        Assert.AreEqual(phone, result.Phone);
        Assert.AreEqual(birthday.Year, result.BirthYear);
        Assert.IsTrue(result.Active);
        Assert.IsFalse(string.IsNullOrWhiteSpace(result.PasswordHash));
        Assert.IsTrue(result.CreatedAt <= DateTime.UtcNow);

        // Verify hash matches password
        var verifyResult = _hasher.VerifyHashedPassword(result, result.PasswordHash, password);
        Assert.AreEqual(PasswordVerificationResult.Success, verifyResult);

        // Verify persistence call
        _mockUserAccess.Verify(u => u.Create(It.Is<UserModel>(usr =>
            usr.Username == username &&
            usr.Name == name &&
            usr.Role == "USER"
        )), Times.Once);
    }


    [TestMethod]
    [DataRow(null, "password", "Name", "test@example.com", "+31612345678", "2000-01-01")]
    [DataRow("", "password", "Name", "test@example.com", "+31612345678", "2000-01-01")]
    [DataRow("user", null, "Name", "test@example.com", "+31612345678", "2000-01-01")]
    [DataRow("user", "", "Name", "test@example.com", "+31612345678", "2000-01-01")]
    [DataRow("user", "password", null, "test@example.com", "+31612345678", "2000-01-01")]
    [DataRow("user", "password", "", "test@example.com", "+31612345678", "2000-01-01")]
    public async Task CreateUserAsync_InvalidInput_ThrowsArgumentException(string username, string password, string name, string email, string phone, string birthdayString)
    {
        //arrange 
        var birthday = DateTime.Parse(birthdayString);
        
        //act + assert
        await Assert.ThrowsExceptionAsync<ArgumentException>(async () =>
            await _userService!.CreateUserAsync(username, password, name, email, phone, birthday));

        _mockUserAccess!.Verify(access => access.Create(It.IsAny<UserModel>()), Times.Never);
    }

    [TestMethod]
    [DataRow("user1", "short", "Name1", "user1@example.com", "+31611111111", "2000-01-01")]
    [DataRow("user2", "        ", "Name2", "user2@example.com", "+31622222222", "2000-01-01")]
    [DataRow("user3", "alllowercase1", "Name3", "user3@example.com", "+31633333333", "2000-01-01")]
    [DataRow("user4", "ALLUPPERCASE1", "Name4", "user4@example.com", "+31644444444", "2000-01-01")]
    [DataRow("user5", "NoNumbersHere", "Name5", "user5@example.com", "+31655555555", "2000-01-01")]
    [DataRow("user6", "12345678", "Name6", "user6@example.com", "+31666666666", "2000-01-01")]
    [DataRow("user7", "Password!", "Name7", "user7@example.com", "+31677777777", "2000-01-01")]
    [DataRow("user8", "PASS1234", "Name8", "user8@example.com", "+31688888888", "2000-01-01")]
    [DataRow("user9", "pass1234", "Name9", "user9@example.com", "+31699999999", "2000-01-01")]
    public async Task CreateUserAsync_WeakOrWhitespacePassword_ThrowsArgumentException(string username, string password, string name, string email, string phone, string birthdayString)
    {
        //arrange
        var birthday = DateTime.Parse(birthdayString);
        
        //act + assert
        await Assert.ThrowsExceptionAsync<ArgumentException>(async () =>
            await _userService!.CreateUserAsync(username, password, name, email, phone, birthday));

        _mockUserAccess!.Verify(access => access.Create(It.IsAny<UserModel>()), Times.Never);
    }

    [TestMethod]
    [DataRow(null, "Password1", "Alice", "alice@example.com", "+31611111111", "2000-01-01")]
    [DataRow("", "Password1", "Bob", "bob@example.com", "+31622222222", "2000-01-01")]
    [DataRow("   ", "Password1", "Charlie", "charlie@example.com", "+31633333333", "2000-01-01")]
    [DataRow("user1", "Password1", null, "diana@example.com", "+31644444444", "2000-01-01")]
    [DataRow("user2", "Password1", "", "emma@example.com", "+31655555555", "2000-01-01")]
    [DataRow("user3", "Password1", "   ", "frank@example.com", "+31666666666", "2000-01-01")]
    [DataRow("uuuuuuuuuuuuuuuuuuuuuuuuuuuuuuuuuuuuuuuuuu", "Password1", "LongName", "longuser@example.com", "+31677777777", "2000-01-01")] // very long username
    [DataRow("user4", "Password1", "NNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNN", "longname@example.com", "+31688888888", "2000-01-01")] // very long name
    public async Task CreateUserAsync_InvalidUsernameOrName_ThrowsArgumentException(string username, string password, string name, string email, string phone, string birthdayString)
    {
        //arrange
        var birthday = DateTime.Parse(birthdayString);
        
        //act + assert
        await Assert.ThrowsExceptionAsync<ArgumentException>(async () =>
            await _userService!.CreateUserAsync(username, password, name, email, phone, birthday));

        _mockUserAccess!.Verify(access => access.Create(It.IsAny<UserModel>()), Times.Never);
    }

    [TestMethod]
    [DataRow("user1", "ValidPass1@", "Alice", "USER", true)]
    [DataRow("user2", "Another1!", "Bob", "ADMIN", false)]
    [DataRow("user3", "Complex#Pass2", "Charlie", "MANAGER", true)]
    public async Task UpdateUser_ValidUser_CallsUpdateAndReturnsUser(
        string username,
        string password,
        string name,
        string role,
        bool active)
    {
        // Arrange
        var user = new UserModel
        {
            Username = username,
            PasswordHash = password,
            Name = name,
            Role = role,
            Active = active,
            CreatedAt = DateTime.UtcNow
        };

        _mockUserAccess!
            .Setup(access => access.Update(It.IsAny<UserModel>()))
            .ReturnsAsync(true).Verifiable();

        // Act
        var result = await _userService.UpdateUser(user);

        // Assert
        Assert.AreEqual(user, result);
        _mockUserAccess.Verify(access => access.Update(user), Times.Once);
    }

    [TestMethod]
    public async Task UpdateUser_NullUser_ThrowsArgumentNullException()
    {
        await Assert.ThrowsExceptionAsync<ArgumentNullException>(async () =>
            await _userService!.UpdateUser(null!));

        _mockUserAccess!.Verify(access => access.Update(It.IsAny<UserModel>()), Times.Never);
    }

    [TestMethod]
    [DataRow("userX", "FailPass1@", "Dave")]
    [DataRow("userY", "OtherFail2#", "Eve")]
    public async Task UpdateUser_WhenUpdateThrows_ExceptionPropagates(
        string username,
        string password,
        string name)
    {
        // Arrange
        var user = new UserModel
        {
            Username = username,
            Name = name,
            Role = "USER",
            Active = true,
            CreatedAt = DateTime.UtcNow
        };
        
        user.PasswordHash = _hasher.HashPassword(user, password); 

        _mockUserAccess!
            .Setup(access => access.Update(It.IsAny<UserModel>()))
            .ThrowsAsync(new InvalidOperationException("DB error"));

        // Act & Assert
        var ex = await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () =>
            await _userService.UpdateUser(user));

        Assert.AreEqual("DB error", ex.Message);
    }

    [TestMethod]
    [DataRow("", "plain", null, "ADMIN", false)]
    [DataRow("userWeird", "notHashed", "", "SUPERUSER", true)]
    [DataRow("   ", "123", "   ", "GUEST", false)]
    public async Task UpdateUser_DoesNotValidateUserFields(
        string username,
        string password,
        string name,
        string role,
        bool active)
    {
        // Arrange
        var user = new UserModel
        {
            Username = username,
            PasswordHash = password,
            Name = name,
            Role = role,
            Active = active,
            CreatedAt = DateTime.MinValue
        };

        _mockUserAccess!
            .Setup(access => access.Update(It.IsAny<UserModel>()))
            .ReturnsAsync(true).Verifiable();

        // Act
        var result = await _userService!.UpdateUser(user);

        // Assert
        Assert.AreEqual(user, result);
        _mockUserAccess.Verify(access => access.Update(user), Times.Once);
    }
}

