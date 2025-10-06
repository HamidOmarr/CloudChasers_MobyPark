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
    public async Task CreateUser_ValidInput_CreatesUserModel(string username, string password, string name)
    {
        // Arrange
        _mockUserAccess!
            .Setup(access => access.Create(It.IsAny<UserModel>()))
            .Callback<UserModel>(model => _ = model)
            .ReturnsAsync(true);

        var user = new UserModel
        {
            Username = username,
            Password = password,
            Name = name,
            Email = "email@example.com",
            Phone = "0612345678",
            BirthYear = 2000,
            CreatedAt = DateTime.UtcNow,
            Active = true
        };

        // Act
        var result = await _userService!.CreateUser(user);

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
    [DataRow(null, "password", "Name", "email@example.com", "0612345678", 2000)]
    [DataRow("", "password", "Name", "email@example.com", "0612345678", 2000)]
    [DataRow("user", null, "Name", "email@example.com", "0612345678", 2000)]
    [DataRow("user", "", "Name", "email@example.com", "0612345678", 2000)]
    [DataRow("user", "password", null, "email@example.com", "0612345678", 2000)]
    [DataRow("user", "password", "", "email@example.com", "0612345678", 2000)]
    [DataRow("user", "password", "Name", null, "0612345678", 2000)]
    [DataRow("user", "password", "Name", "", "0612345678", 2000)]
    [DataRow("user", "password", "Name", "email@example.com", null, 2000)]
    [DataRow("user", "password", "Name", "email@example.com", "", 2000)]
    [DataRow("user", "password", "Name", "email@example.com", "0612345678", 1899)]
    [DataRow("user", "password", "Name", "email@example.com", "0612345678", Int32.MaxValue)]
    [DataRow("user", "password", "Name", "email@example.com", "0612345678", 1900)]
    public async Task CreateUser_InvalidInput_ThrowsArgumentException(string username, string password, string name, string email, string phone, int birthYear)
    {
        var user = new UserModel
        {
            Username = username,
            Password = password,
            Name = name,
            Email = email,
            Phone = phone,
            BirthYear = birthYear,
            CreatedAt = DateTime.UtcNow,
            Active = true
        };

        await Assert.ThrowsExceptionAsync<ArgumentException>(async () =>
            await _userService!.CreateUser(user));

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
    public async Task CreateUser_WeakOrWhitespacePassword_ThrowsArgumentException(string username, string password, string name)
    {
        var user = new UserModel
        {
            Username = username,
            Password = password,
            Name = name,
            Email = "email@example.com",
            Phone = "0612345678",
            BirthYear = 2000,
            CreatedAt = DateTime.UtcNow,
            Active = true
        };

        await Assert.ThrowsExceptionAsync<ArgumentException>(async () =>
            await _userService!.CreateUser(user));

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
    public async Task CreateUser_InvalidUsernameOrName_ThrowsArgumentException(string username, string password, string name)
    {
        var user = new UserModel
        {
            Username = username,
            Password = password,
            Name = name,
            Email = "email@example.com",
            Phone = "0612345678",
            BirthYear = 2000,
            CreatedAt = DateTime.UtcNow,
            Active = true
        };

        await Assert.ThrowsExceptionAsync<ArgumentException>(async () =>
            await _userService!.CreateUser(user));

        _mockUserAccess!.Verify(access => access.Create(It.IsAny<UserModel>()), Times.Never);
    }

    [TestMethod]
    [DataRow("user@domain.com", "user@domain.com")]
    [DataRow("john@doe.org", "john@doe.org")]
    [DataRow("jane.doe@familyhome.com", "jane.doe@familyhome.com")]
    [DataRow("james123@gmail.com", "james123@gmail.com")]
    [DataRow("familie.spatiebalk@kpnmail.nl", "familie.spatiebalk@kpnmail.nl")]
    [DataRow("user@domain.TLDsLongerThanSixtyThreeCharactersCannotExistSoItIsTheLimitHere",
        "user@domain.tldslongerthansixtythreecharacterscannotexistsoitisthelimithere")]
    [DataRow("user+tag@domain.com", "user+tag@domain.com")]
    [DataRow("user_name@domain.com", "user_name@domain.com")]
    [DataRow("user-name@domain.com", "user-name@domain.com")]
    [DataRow("u@domain.com", "u@domain.com")]
    [DataRow("user@sub.domain.com", "user@sub.domain.com")]
    [DataRow("user@a.b.c.d.e.com", "user@a.b.c.d.e.com")]
    [DataRow("user@exämple.de", "user@xn--exmple-cua.de")]
    [DataRow("user@xn--fsq.com", "user@xn--fsq.com")]
    [DataRow("user%domain@domain.com", "user%domain@domain.com")]
    [DataRow("12345@numbers.com", "12345@numbers.com")]
    [DataRow("user@123.com", "user@123.com")]
    [DataRow(" user@domain.com ", "user@domain.com")]
    [DataRow("user--name@domain.com", "user--name@domain.com")]
    public async Task CreateUser_ValidEmail_CreatesUser(string inputEmail, string expectedNormalized)
    {
        // Arrange
        var user = new UserModel
        {
            Username = "validUser",
            Password = "W0rK!ngP@ss",
            Name = "Valid Name",
            Email = inputEmail,
            Phone = "+310612345678",
            BirthYear = 2000,
            CreatedAt = DateTime.UtcNow,
            Active = true
        };

        _mockUserAccess!.Setup(access => access.Create(It.IsAny<UserModel>())).ReturnsAsync(true).Verifiable();

        // Act
        var result = await _userService!.CreateUser(user);

        // Assert
        Assert.AreEqual(expectedNormalized, result.Email);
        _mockUserAccess.Verify(access => access.Create(It.Is<UserModel>(model => model.Email == expectedNormalized)), Times.Once);
    }

    [TestMethod]
    [DataRow("allMissing")]
    [DataRow("missingAtSymbol.com")]
    [DataRow("missingDot@com")]
    [DataRow("two@@ats.com")]
    [DataRow("spaces in@em ail.com")]
    [DataRow("")]
    [DataRow(" ")]
    [DataRow(".startdot@domain.com")]
    [DataRow("enddot.@domain.com")]
    [DataRow("double..dot@domain.com")]
    [DataRow("user@-domain.com")]
    [DataRow("user@domain-.com")]
    [DataRow("user!@domain.com")]
    [DataRow("user()@domain.com")]
    [DataRow("user@do(main).com")]
    [DataRow("user@")]
    [DataRow("user@.")]
    [DataRow("@domain.com")]
    [DataRow("user@domain.c")]
    [DataRow("\"quoted@local\"@domain.com")] // quoted local parts are valid but often unsupported. Disallowed for simplicity and to avoid injection risks.
    [DataRow("user\t@domain.com")]
    [DataRow("user\n@domain.com")]
    [DataRow("user@domain.TLDsLongerThanSixtyThreeCharactersCannotExistSoItIsTheLimitHereA")]
    [DataRow("user..name@domain.com")]
    [DataRow("user@domain!name.com")]
    [DataRow("user@domain#name.com")]
    [DataRow("user@domain$%.com")]
    public async Task CreateUser_InvalidEmail_ThrowsArgumentException(string email)
    {
        var user = new UserModel
        {
            Username = "validUser",
            Password = "W0rK!ngP@ss",
            Name = "Valid Name",
            Email = email,
            Phone = "+31612345678",
            BirthYear = 2000,
            CreatedAt = DateTime.UtcNow,
            Active = true
        };

        await Assert.ThrowsExceptionAsync<ArgumentException>(async () =>
            await _userService!.CreateUser(user));
        _mockUserAccess!.Verify(access => access.Create(It.IsAny<UserModel>()), Times.Never);
    }

    [TestMethod]
    [DataRow("012345678")]
    [DataRow("0612345678")]
    [DataRow("06 12345678")]
    [DataRow("+31 6 12345678")]
    [DataRow("+31612345678")]
    [DataRow("00316 12345678")]
    [DataRow("0031612345678")]
    [DataRow("(06)12345678")]
    [DataRow("06-12345678")]
    [DataRow("06-12-34-56-78")]
    [DataRow("+31 (0)6 12345678")]
    [DataRow("0031 06 12345678")]
    [DataRow("++31612345678")]
    [DataRow(" 06  1234  5678 ")]
    public async Task CreateUser_ValidPhoneFormats_CreatesUser(string phone)
    {
        // Arrange
        var user = new UserModel
        {
            Username = "validUser",
            Password = "W0rK!ngP@ss",
            Name = "Valid Name",
            Email = "valid@email.com",
            Phone = phone,
            BirthYear = 2000,
            CreatedAt = DateTime.UtcNow,
            Active = true
        };

        _mockUserAccess!
            .Setup(access => access.Create(It.IsAny<UserModel>()))
            .ReturnsAsync(true).Verifiable();

        // Act
        var result = await _userService!.CreateUser(user);

        // Assert
        Assert.AreEqual("+310612345678", result.Phone);
    }

    [TestMethod]
    [DataRow("12345678")]
    [DataRow("1234567890")]
    [DataRow("phone")]
    [DataRow("")]
    [DataRow(" ")]
    [DataRow("0123456789012345")]
    [DataRow("0123A56789")]
    public async Task CreateUser_InvalidPhoneFormats_ThrowsArgumentException(string phone)
    {
        var user = new UserModel
        {
            Username = "validUser",
            Password = "W0rK!ngP@ss",
            Name = "Valid Name",
            Email = "valid@email.com",
            Phone = phone,
            BirthYear = 2000,
            CreatedAt = DateTime.UtcNow,
            Active = true
        };
        await Assert.ThrowsExceptionAsync<ArgumentException>(async () =>
            await _userService!.CreateUser(user));
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
            Password = _userService!.HashPassword(password),
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
            Password = _userService!.HashPassword(password),
            Name = name,
            Role = "USER",
            Active = true,
            CreatedAt = DateTime.UtcNow
        };

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
            Password = password,
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

