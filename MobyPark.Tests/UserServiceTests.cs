using Microsoft.AspNetCore.Identity;
using MobyPark.Models;
using MobyPark.Models.Access;
using MobyPark.Models.DataService;
using MobyPark.Models.Requests.User;
using MobyPark.Services;
using MobyPark.Services.Results.User;
using Moq;

namespace MobyPark.Tests;

[TestClass]
public sealed class UserServiceTests
{
    private Mock<IDataAccess>? _mockDataService;
    private Mock<IUserAccess>? _mockUserAccess;
    private Mock<SessionService>? _mockSessions;
    private UserService? _userService;
    private IPasswordHasher<UserModel>? _hasher;


    [TestInitialize]
    public void TestInitialize()
    {
        _mockDataService = new Mock<IDataAccess>();
        _mockUserAccess = new Mock<IUserAccess>();
        _hasher = new PasswordHasher<UserModel>();
        _mockSessions = new Mock<SessionService>();
        _mockDataService.Setup(ds => ds.Users).Returns(_mockUserAccess.Object);
        _userService = new UserService(_mockDataService.Object, _hasher, _mockSessions.Object);
    }

    [TestMethod]
    [DataRow("mypassword")]
    [DataRow("anotherSecret")]
    [DataRow("123456")]
    public void VerifyPassword_CorrectPassword_ReturnsSuccess(string password)
    {
        // Arrange
        var user = new UserModel();
        var hash = _hasher!.HashPassword(user, password);

        // Act
        var result = _hasher.VerifyHashedPassword(user, hash, password);

        // Assert
        Assert.AreEqual(PasswordVerificationResult.Success, result);
    }

    [TestMethod]
    [DataRow("mypassword", "wrongpassword")]
    [DataRow("correct123", "wrong123")]
    [DataRow("test", "TEST")] // case-sensitive
    public void VerifyPassword_WrongPassword_ReturnsFailed(string realPassword, string attempt)
    {
        // Arrange
        var user = new UserModel();
        var hash = _hasher!.HashPassword(user, realPassword);

        // Act
        var result = _hasher.VerifyHashedPassword(user, hash, attempt);

        // Assert
        Assert.AreEqual(PasswordVerificationResult.Failed, result);
    }

    [TestMethod]
    [DataRow("user1", "P@ssword1", "Alice", "alice@gmail.com", "+310612345678", "2001-10-10")]
    [DataRow("user2", "Pass123$", "Bob", "bob@example.com", "+310611112222", "1998-03-15")]
    [DataRow("user3", "Abc12345!", "Charlie", "charlie@example.com", "+310622223333", "1995-07-22")]
    [DataRow("user4", "StrongPass1@", "David", "david@example.com", "+310633334444", "2000-01-05")]
    [DataRow("user5", "Pa ss1Ab2!", "Eve", "eve@example.com", "+310644445555", "1999-09-09")]
    [DataRow("user6", "A1b2C3d4$", "Frank", "frank@example.com", "+310655556666", "1997-12-31")]
    [DataRow("user7", "Aa1!Aa1!", "Grace", "grace@example.com", "+310666667777", "2002-04-18")]
    [DataRow("user8", "Complex#Password123", "Hannah", "hannah@example.com", "+310677778888", "2003-08-25")]
    [DataRow("user9", "Xy9*Zz8@", "Isaac", "isaac@example.com", "+310688889999", "1996-11-11")]
    [DataRow("user10", "GoodPass1@", "Jack", "jack@example.com", "+310699990000", "1994-06-06")]
    public async Task CreateUserAsync_ValidInput_CreatesUserModel(string username, string password, string name, string email, string phone, string birthdayString)
    {
        // Arrange
        var birthday = DateTime.Parse(birthdayString);

        _mockUserAccess!
            .Setup(access => access.CreateWithId(It.IsAny<UserModel>()))
            .ReturnsAsync((true, 1));

        var request = new RegisterRequest
        {
            Username = username,
            Password = password,
            ConfirmPassword = password,
            Name = name,
            Email = email,
            Phone = phone,
            Birthday = birthday
        };

        // Act
        var result = await _userService!.CreateUserAsync(request);

        // Assert
        Assert.IsInstanceOfType(result, typeof(RegisterResult.Success));

        // Extract the created user
        var success = result as RegisterResult.Success;
        var createdUser = success!.User;

        Assert.IsNotNull(createdUser);
        Assert.AreEqual(username, createdUser.Username);
        Assert.AreEqual(name, createdUser.Name);
        Assert.AreEqual("USER", createdUser.Role);
        Assert.AreEqual(email.ToLowerInvariant(), createdUser.Email); // email is normalized
        Assert.AreEqual(phone, createdUser.Phone);                   // phone normalized in CleanPhone
        Assert.AreEqual(birthday.Year, createdUser.BirthYear);
        Assert.IsTrue(createdUser.Active);
        Assert.IsFalse(string.IsNullOrWhiteSpace(createdUser.PasswordHash));
        Assert.IsTrue(createdUser.CreatedAt <= DateTime.UtcNow);

        // Verify hash matches password
        var verifyResult = _hasher!.VerifyHashedPassword(createdUser, createdUser.PasswordHash, password);
        Assert.AreEqual(PasswordVerificationResult.Success, verifyResult);

        // Verify persistence call
        _mockUserAccess.Verify(u => u.CreateWithId(It.Is<UserModel>(usr =>
            usr.Username == username &&
            usr.Name == name &&
            usr.Role == "USER"
        )), Times.Once);
    }


    [TestMethod]
    [DataRow(null, "password", "Name", "email@example.com", "0612345678", "2000-01-01")]
    [DataRow("", "password", "Name", "email@example.com", "0612345678", "2000-01-01")]
    [DataRow("user", null, "Name", "email@example.com", "0612345678", "2000-01-01")]
    [DataRow("user", "", "Name", "email@example.com", "0612345678", "2000-01-01")]
    [DataRow("user", "password", null, "email@example.com", "0612345678", "2000-01-01")]
    [DataRow("user", "password", "", "email@example.com", "0612345678", "2000-01-01")]
    [DataRow("user", "password", "Name", null, "0612345678", "2000-01-01")]
    [DataRow("user", "password", "Name", "", "0612345678", "2000-01-01")]
    [DataRow("user", "password", "Name", "email@example.com", null, "2000-01-01")]
    [DataRow("user", "password", "Name", "email@example.com", "", "2000-01-01")]
    [DataRow("user", "password", "Name", "email@example.com", "0612345678", "2000-01-01")]
    [DataRow("user", "password", "Name", "email@example.com", "0612345678", "3000-01-01")]
    [DataRow("user", "password", "Name", "email@example.com", "0612345678", "2000-01-01")]
    public async Task CreateUserAsync_InvalidInput_ReturnsInvalidDataResponse(string username, string password, string name, string email, string phone, string birthdayString)
    {
        // Arrange
        var birthday = DateTime.Parse(birthdayString);

        var request = new RegisterRequest
        {
            Username = username,
            Password = password ?? "",
            ConfirmPassword = password ?? "",
            Name = name,
            Email = email,
            Phone = phone,
            Birthday = birthday
        };

        // Act
        var result = await _userService!.CreateUserAsync(request);

        // Assert
        Assert.IsInstanceOfType(result, typeof(RegisterResult.InvalidData));
        _mockUserAccess!.Verify(access => access.CreateWithId(It.IsAny<UserModel>()), Times.Never);
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
    public async Task CreateUserAsync_WeakOrWhitespacePassword_ReturnsInvalidData(
        string username, string password, string name, string email, string phone, string birthdayString)
    {
        // Arrange
        var birthday = DateTime.Parse(birthdayString);

        var request = new RegisterRequest
        {
            Username = username,
            Password = password,
            ConfirmPassword = password,
            Name = name,
            Email = email,
            Phone = phone,
            Birthday = birthday
        };

        // Act
        var result = await _userService!.CreateUserAsync(request);

        // Assert
        Assert.IsInstanceOfType(result, typeof(RegisterResult.InvalidData));
        _mockUserAccess!.Verify(access => access.CreateWithId(It.IsAny<UserModel>()), Times.Never);
    }

    [TestMethod]
    [DataRow(null, "Password1", "Alice", "alice@example.com", "+310611111111", "2000-01-01")]
    [DataRow("", "Password1", "Bob", "bob@example.com", "+310622222222", "2000-01-01")]
    [DataRow("   ", "Password1", "Charlie", "charlie@example.com", "+310633333333", "2000-01-01")]
    [DataRow("user1", "Password1", null, "diana@example.com", "+310644444444", "2000-01-01")]
    [DataRow("user2", "Password1", "", "emma@example.com", "+310655555555", "2000-01-01")]
    [DataRow("user3", "Password1", "   ", "frank@example.com", "+310666666666", "2000-01-01")]
    [DataRow("uuuuuuuuuuuuuuuuuuuuuuuuuuuuuuuuuuuuuuuuuu", "Password1", "LongName", "longuser@example.com", "+310677777777", "2000-01-01")] // very long username
    [DataRow("user4", "Password1", "NNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNN", "longname@example.com", "+310688888888", "2000-01-01")] // very long name
    public async Task CreateUserAsync_InvalidUsernameOrName_ReturnsInvalidDataResponse(string username, string password, string name, string email, string phone, string birthdayString)
    {
        //arrange
        var birthday = DateTime.Parse(birthdayString);
        var request = new RegisterRequest
        {
            Username = username,
            Password = password,
            ConfirmPassword = password,
            Name = name,
            Email = email,
            Phone = phone,
            Birthday = birthday
        };

        //act + assert
        var result = await _userService!.CreateUserAsync(request);

        // Assert
        Assert.IsInstanceOfType(result, typeof(RegisterResult.InvalidData));
        _mockUserAccess!.Verify(access => access.CreateWithId(It.IsAny<UserModel>()), Times.Never);
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

        var birthday = DateTime.Parse("2000-01-01");

        var request = new RegisterRequest
        {
            Username = "John.Doe",
            Password = "StrongPass1@",
            ConfirmPassword = "StrongPass1@",
            Name = "John Doe",
            Email = inputEmail,
            Phone = "+31612345678",
            Birthday = birthday
        };

        _mockUserAccess!.Setup(access => access.CreateWithId(It.IsAny<UserModel>())).ReturnsAsync((true, 1)).Verifiable();

        // Act
        var result = await _userService!.CreateUserAsync(request);

        // Assert
        Assert.IsInstanceOfType(result, typeof(RegisterResult.Success));
        var success = result as RegisterResult.Success;
        var createdUser = success!.User;
        Assert.IsNotNull(createdUser);
        Assert.AreEqual(expectedNormalized, createdUser.Email);
        _mockUserAccess.Verify(access => access.CreateWithId(It.Is<UserModel>(model => model.Email == expectedNormalized)), Times.Once);
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
    [DataRow("user@domain!name.com")]
    [DataRow("user@domain#name.com")]
    [DataRow("user@domain$%.com")]
    public async Task CreateUser_InvalidEmail_ReturnsInvalidDataResponse(string email)
    {
        var birthday = DateTime.Parse("2000-01-01");

        var request = new RegisterRequest
        {
            Username = "John.Doe",
            Password = "StrongPass1@",
            ConfirmPassword = "StrongPass1@",
            Name = "John Doe",
            Email = email,
            Phone = "+31612345678",
            Birthday = birthday
        };

        // Act
        var result = await _userService!.CreateUserAsync(request);

        // Assert
        Assert.IsInstanceOfType(result, typeof(RegisterResult.InvalidData));
        _mockUserAccess!.Verify(access => access.CreateWithId(It.IsAny<UserModel>()), Times.Never);
    }

    [TestMethod]
    [DataRow("0612345678", "+310612345678")]
    [DataRow("06 12345678", "+310612345678")]
    [DataRow("+31 6 12345678", "+310612345678")]
    [DataRow("+31612345678", "+310612345678")]
    [DataRow("00316 12345678", "+310612345678")]
    [DataRow("0031612345678", "+310612345678")]
    [DataRow("(06)12345678", "+310612345678")]
    [DataRow("06-12345678", "+310612345678")]
    [DataRow("06-12-34-56-78", "+310612345678")]
    [DataRow("612345678", "+310612345678")]
    [DataRow("+31 (0)6 12345678", "+310612345678")]
    [DataRow("0031 06 12345678", "+310612345678")]
    [DataRow("++31612345678", "+310612345678")]
    [DataRow(" 06  1234  5678 ", "+310612345678")]
    [DataRow("+310612345678", "+310612345678")]
    [DataRow("0101234567", "+310101234567")]
    [DataRow("010 1234567", "+310101234567")]
    [DataRow("+31 10 1234567", "+310101234567")]
    [DataRow("+31101234567", "+310101234567")]
    [DataRow("0031 10 1234567", "+310101234567")]
    [DataRow("0031515123456", "+310515123456")]
    [DataRow("(0515)123456", "+310515123456")]
    [DataRow("0515-123456", "+310515123456")]
    [DataRow("0515-12-34-56", "+310515123456")]
    [DataRow("515123456", "+310515123456")]
    [DataRow("+31 (0)76 1234567", "+310761234567")]
    [DataRow("0031 076 1234567", "+310761234567")]
    [DataRow("++310761234567", "+310761234567")]
    [DataRow(" 076  1234  567 ", "+310761234567")]
    [DataRow("+310761234567", "+310761234567")]
    // All separators will be stripped out. These are valid but rare/hacky/weird/not recommended formats, but included for robustness.
    [DataRow("06.1234.5678", "+310612345678")]    //        .
    [DataRow("06#1234#5678", "+310612345678")]    //        #
    [DataRow("06/1234/5678", "+310612345678")]    //        /
    [DataRow("06_1234_5678", "+310612345678")]    //        _
    [DataRow("06*1234*5678", "+310612345678")]    //        *
    [DataRow("06+1234+5678", "+310612345678")]    //        +
    [DataRow("06=1234=5678", "+310612345678")]    //        =
    [DataRow("06@1234@5678", "+310612345678")]    //        @
    [DataRow("06!1234!5678", "+310612345678")]    //        !
    [DataRow("06$1234$5678", "+310612345678")]    //        $
    [DataRow("06%1234%5678", "+310612345678")]    //        %
    [DataRow("06^1234^5678", "+310612345678")]    //        ^
    [DataRow("06&1234&5678", "+310612345678")]    //        &
    [DataRow("06{1234}5678", "+310612345678")]    //        {}
    [DataRow("06[1234]5678", "+310612345678")]    //        []
    [DataRow("06|1234|5678", "+310612345678")]    //        |
    [DataRow("06;1234;5678", "+310612345678")]    //        ;
    [DataRow("06:1234:5678", "+310612345678")]    //        :
    [DataRow("06'1234'5678", "+310612345678")]    //        '
    [DataRow("06\"1234\"5678", "+310612345678")]  //        "
    [DataRow("06<1234>5678", "+310612345678")]    //        <>
    [DataRow("06,1234,5678", "+310612345678")]    //        ,
    [DataRow("06?1234?5678", "+310612345678")]    //        ?
    [DataRow("06`1234`5678", "+310612345678")]    //        `
    [DataRow("06~1234~5678", "+310612345678")]    //        ~
    public async Task CreateUser_ValidPhoneFormats_CreatesUser(string phone, string expected)
    {
        // Arrange
        var birthday = DateTime.Parse("2000-01-01");

        var request = new RegisterRequest
        {
            Username = "John.Doe",
            Password = "StrongPass1@",
            ConfirmPassword = "StrongPass1@",
            Name = "John Doe",
            Email = "john@doe.com",
            Phone = phone,
            Birthday = birthday
        };

        _mockUserAccess!.Setup(access => access.CreateWithId(It.IsAny<UserModel>())).ReturnsAsync((true, 1)).Verifiable();

        _mockUserAccess!
            .Setup(access => access.Create(It.IsAny<UserModel>()))
            .ReturnsAsync(true).Verifiable();

        // Act
        var result = await _userService!.CreateUserAsync(request);

        // Assert
        Assert.IsInstanceOfType(result, typeof(RegisterResult.Success));
        var success = result as RegisterResult.Success;
        var createdUser = success!.User;
        Assert.IsNotNull(createdUser);
        Assert.AreEqual(expected, createdUser.Phone);
        _mockUserAccess.Verify(access => access.CreateWithId(It.Is<UserModel>(model => model.Phone == expected)), Times.Once);
    }

    [TestMethod]
    [DataRow("061234567")]
    [DataRow("012345678901")]
    [DataRow("A061234567")]
    [DataRow("061234A567")]
    [DataRow("061234567A")]
    [DataRow("+32 612345678")]
    public async Task CreateUser_InvalidPhoneFormats_ReturnsInvalidDataResponse(string phone)
    {
        var birthday = DateTime.Parse("2000-01-01");

        var request = new RegisterRequest
        {
            Username = "John.Doe",
            Password = "StrongPass1@",
            ConfirmPassword = "StrongPass1@",
            Name = "John Doe",
            Email = "john@doe.com",
            Phone = phone,
            Birthday = birthday
        };

        var result = await _userService!.CreateUserAsync(request);

        // Assert
        Assert.IsInstanceOfType(result, typeof(RegisterResult.InvalidData));
        _mockUserAccess!.Verify(access => access.CreateWithId(It.IsAny<UserModel>()), Times.Never);
    }

    [TestMethod]
    [DataRow("user1", "ValidPass1@", "Alice", "USER", true)]
    [DataRow("user2", "Another1!", "Bob", "ADMIN", false)]
    [DataRow("user3", "Complex#Pass2", "Charlie", "MANAGER", true)]
    public async Task UpdateUser_ValidUser_CallsUpdateAndReturnsUser(string username, string password, string name, string role, bool active)
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
        var result = await _userService!.UpdateUser(user);

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
    public async Task UpdateUser_WhenUpdateThrows_ExceptionPropagates(string username, string password, string name)
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

        user.PasswordHash = _hasher!.HashPassword(user, password);

        _mockUserAccess!
            .Setup(access => access.Update(It.IsAny<UserModel>()))
            .ThrowsAsync(new InvalidOperationException("DB error"));

        // Act & Assert
        var ex = await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () =>
            await _userService!.UpdateUser(user));

        Assert.AreEqual("DB error", ex.Message);
    }

    [TestMethod]
    [DataRow("", "plain", null, "ADMIN", false)]
    [DataRow("userWeird", "notHashed", "", "SUPERUSER", true)]
    [DataRow("   ", "123", "   ", "GUEST", false)]
    public async Task UpdateUser_DoesNotValidateUserFields(string username, string password, string name, string role, bool active)
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

