using Microsoft.AspNetCore.Identity;
using MobyPark.DTOs.User.Request;
using MobyPark.Models;
using MobyPark.Models.Repositories.Interfaces;
using MobyPark.Services;
using MobyPark.Services.Results.Session;
using MobyPark.Services.Results.User;
using Moq;
using MobyPark.Services.Interfaces;

namespace MobyPark.Tests;

[TestClass]
public sealed class UserServiceTests
{
    #region Setup
    private Mock<IUserRepository> _mockUsersRepo = null!;
    private Mock<IUserPlateRepository> _mockUserPlatesRepo = null!;
    private Mock<IParkingSessionRepository> _mockParkingSessionsRepo = null!;
    private Mock<IRoleRepository> _mockRolesRepo = null!;
    private Mock<IRepository<HotelModel>> _mockHotelRepo = null!;
    private Mock<IRepository<BusinessModel>> _mockBusinessRepo = null!;
    private Mock<IPasswordHasher<UserModel>> _mockHasher = null!;
    private Mock<ISessionService> _mockSessionService = null!;
    private UserService _userService = null!;

    private readonly UserModel _defaultUser = new()
    {
        Id = 1,
        Username = "testuser",
        Email = "test@user.com",
        PasswordHash = "hashed_password",
        Role = new RoleModel { Id = 6, Name = "USER" },
        RoleId = 6
    };

    private readonly UserModel _adminUser = new()
    {
        Id = 2,
        Username = "admin",
        Email = "admin@user.com",
        PasswordHash = "hashed_password",
        Role = new RoleModel { Id = 2, Name = "ADMIN" },
        RoleId = 1
    };

    private readonly UserModel _itManagerUser = new()
    {
        Id = 3,
        Username = "itmanager",
        Email = "itmanager@user.com",
        PasswordHash = "hashed_password",
        Role = new RoleModel { Id = 2, Name = "IT MANAGER" },
        RoleId = 2
    };

    [TestInitialize]
    public void TestInitialize()
    {
        _mockUsersRepo = new Mock<IUserRepository>();
        _mockUserPlatesRepo = new Mock<IUserPlateRepository>();
        _mockParkingSessionsRepo = new Mock<IParkingSessionRepository>();
        _mockRolesRepo = new Mock<IRoleRepository>();
        _mockBusinessRepo = new Mock<IRepository<BusinessModel>>();
        _mockHotelRepo = new Mock<IRepository<HotelModel>>();
        _mockHasher = new Mock<IPasswordHasher<UserModel>>();
        _mockSessionService = new Mock<ISessionService>();

        _userService = new UserService(
            _mockUsersRepo.Object,
            _mockUserPlatesRepo.Object,
            _mockParkingSessionsRepo.Object,
            _mockRolesRepo.Object,
            _mockHasher.Object,
            _mockSessionService.Object, _mockHotelRepo.Object, _mockBusinessRepo.Object
        );
    }

    #endregion

    #region Create

    [TestMethod]
    [DataRow("user1", "P@ssword1", "Alice", "alice@gmail.com", "+310612345678", "2001-10-10")]
    public async Task CreateUserAsync_ValidInput_CreatesUserModel(string username, string password, string name, string email, string phone, string birthdayString)
    {
        // Arrange
        var birthday = DateTimeOffset.Parse(birthdayString + "T00:00:00Z");
        var dto = new RegisterDto
        {
            Username = username, Password = password, ConfirmPassword = password,
            FirstName = name, LastName = name, Email = email, Phone = phone, Birthday = birthday
        };

        _mockUsersRepo.Setup(userRepo => userRepo.GetByUsername(username)).ReturnsAsync((UserModel?)null);
        _mockUsersRepo.Setup(userRepo => userRepo.CreateWithId(It.IsAny<UserModel>())).ReturnsAsync((true, 1L));
        _mockHasher.Setup(hasher => hasher.HashPassword(It.IsAny<UserModel>(), password)).Returns("hashed_password");


        var createdUser = new UserModel
        {
            Id = 1L,
            Username = username,
            Email = email,
            Phone = "+31612345678",
            Role = new RoleModel { Id = 6, Name = "USER" },
            RoleId = 6
        };
        _mockUsersRepo.Setup(userRepo => userRepo.GetByIdWithRoleAndPermissions(1L))
            .ReturnsAsync(createdUser);

        // Act
        var result = await _userService.CreateUserAsync(dto);

        // Assert
        Assert.IsInstanceOfType(result, typeof(RegisterResult.Success));
        var successResult = (RegisterResult.Success)result;
        Assert.AreEqual(1L, successResult.User.Id);
        Assert.AreEqual(username, successResult.User.Username);
        Assert.AreEqual(email, successResult.User.Email);
        Assert.AreEqual("+31612345678", successResult.User.Phone);

        _mockUsersRepo.Verify(userRepo => userRepo.CreateWithId(It.Is<UserModel>(u => u.Username == username)), Times.Once);
        _mockUserPlatesRepo.Verify(uPlateRepo => uPlateRepo.AddPlateToUser(It.IsAny<long>(), It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    [DataRow("user1", "P@ssword1", "plate123")]
    public async Task CreateUserAsync_ValidInputWithPlate_AddsPlateToUser(string username, string password, string plate)
    {
        // Arrange
        var dto = new RegisterDto
        {
            Username = username, Password = password, ConfirmPassword = password,
            FirstName = "Test", LastName = "User", Email = "test@test.com", Phone = "0612345678",
            Birthday = DateTimeOffset.UtcNow.AddYears(-20),
            LicensePlate = plate
        };

        _mockUsersRepo.Setup(userRepo => userRepo.GetByUsername(username)).ReturnsAsync((UserModel?)null);
        _mockUsersRepo.Setup(userRepo => userRepo.CreateWithId(It.IsAny<UserModel>())).ReturnsAsync((true, 1L));
        _mockHasher.Setup(hasher => hasher.HashPassword(It.IsAny<UserModel>(), password)).Returns("hashed_password");
        _mockUserPlatesRepo.Setup(uPlateRepo => uPlateRepo.GetPlatesByPlate(plate.ToUpper())).ReturnsAsync(new List<UserPlateModel>());

        var createdUser = new UserModel
        {
            Id = 1L,
            Username = username,
            Email = "test@test.com",
            Phone = "+31612345678",
            Role = new RoleModel { Id = 6, Name = "USER" },
            RoleId = 6
        };
        _mockUsersRepo.Setup(userRepo => userRepo.GetByIdWithRoleAndPermissions(1L))
            .ReturnsAsync(createdUser);

        // Act
        var result = await _userService.CreateUserAsync(dto);

        // Assert
        Assert.IsInstanceOfType(result, typeof(RegisterResult.Success));
        _mockUserPlatesRepo.Verify(uPlateRepo => uPlateRepo.AddPlateToUser(1L, plate.ToUpper()), Times.Once);
    }

    [TestMethod]
    [DataRow("user1")]
    public async Task CreateUserAsync_UsernameTaken_ReturnsUsernameTaken(string username)
    {
        // Arrange
        var dto = new RegisterDto { Username = username, Password = "ValidPassword1!" };
        _mockUsersRepo.Setup(userRepo => userRepo.GetByUsername(username)).ReturnsAsync(_defaultUser);

        // Act
        var result = await _userService.CreateUserAsync(dto);

        // Assert
        Assert.IsInstanceOfType(result, typeof(RegisterResult.UsernameTaken));
    }

    [TestMethod]
    [DataRow("short")]
    [DataRow("alllowercase1")]
    [DataRow("ALLUPPERCASE1")]
    [DataRow("NoNumbersHere")]
    [DataRow("12345678")]
    [DataRow("Password!")]
    [DataRow("PASS1234")]
    public async Task CreateUserAsync_WeakPassword_ReturnsInvalidData(string password)
    {
        // Arrange
        var dto = new RegisterDto
        {
            Username = "user", Password = password, ConfirmPassword = password,
            FirstName = "Test", LastName = "User", Email = "test@test.com", Phone = "0612345678",
            Birthday = DateTimeOffset.UtcNow.AddYears(-20)
        };
        _mockUsersRepo.Setup(userRepo => userRepo.GetByUsername("user")).ReturnsAsync((UserModel?)null);

        // Act
        var result = await _userService.CreateUserAsync(dto);

        // Assert
        Assert.IsInstanceOfType(result, typeof(RegisterResult.InvalidData));
        StringAssert.Contains(((RegisterResult.InvalidData)result).Message, "Password does not meet complexity");
    }

    #endregion

    #region Login

    [TestMethod]
    [DataRow("testuser", "password123")]
    public async Task Login_ValidUsername_ReturnsSuccess(string identifier, string password)
    {
        // Arrange
        var dto = new LoginDto { Identifier = identifier, Password = password };
        _mockUsersRepo.Setup(userRepo => userRepo.GetByEmail(identifier)).ReturnsAsync((UserModel?)null);
        _mockUsersRepo.Setup(userRepo => userRepo.GetByUsername(identifier)).ReturnsAsync(_defaultUser);
        _mockHasher.Setup(hasher => hasher.VerifyHashedPassword(_defaultUser, "hashed_password", password))
            .Returns(PasswordVerificationResult.Success);

        _mockUsersRepo.Setup(userRepo => userRepo.GetByIdWithRoleAndPermissions(_defaultUser.Id))
            .ReturnsAsync(_defaultUser);

        _mockSessionService.Setup(s => s.CreateSession(_defaultUser))
            .Returns(new CreateJwtResult.Success("test_token"));

        // Act
        var result = await _userService.Login(dto);

        // Assert
        Assert.IsInstanceOfType(result, typeof(LoginResult.Success));
        Assert.AreEqual("test_token", ((LoginResult.Success)result).Response.Token);
    }

    [TestMethod]
    [DataRow("test@user.com", "password123")]
    public async Task Login_ValidEmail_ReturnsSuccess(string identifier, string password)
    {
        // Arrange
        var dto = new LoginDto { Identifier = identifier, Password = password };
        _mockUsersRepo.Setup(userRepo => userRepo.GetByEmail(identifier)).ReturnsAsync(_defaultUser);
        _mockHasher.Setup(hasher => hasher.VerifyHashedPassword(_defaultUser, "hashed_password", password))
            .Returns(PasswordVerificationResult.Success);

        _mockUsersRepo.Setup(userRepo => userRepo.GetByIdWithRoleAndPermissions(_defaultUser.Id))
            .ReturnsAsync(_defaultUser);

        _mockSessionService.Setup(s => s.CreateSession(_defaultUser))
            .Returns(new CreateJwtResult.Success("test_token"));

        // Act
        var result = await _userService.Login(dto);

        // Assert
        Assert.IsInstanceOfType(result, typeof(LoginResult.Success));
        Assert.AreEqual("test_token", ((LoginResult.Success)result).Response.Token);
        _mockUsersRepo.Verify(userRepo => userRepo.GetByUsername(It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    [DataRow("nouser", "password123")]
    public async Task Login_UserNotFound_ReturnsInvalidCredentials(string identifier, string password)
    {
        // Arrange
        var dto = new LoginDto { Identifier = identifier, Password = password };
        _mockUsersRepo.Setup(userRepo => userRepo.GetByEmail(identifier)).ReturnsAsync((UserModel?)null);
        _mockUsersRepo.Setup(userRepo => userRepo.GetByUsername(identifier)).ReturnsAsync((UserModel?)null);

        // Act
        var result = await _userService.Login(dto);

        // Assert
        Assert.IsInstanceOfType(result, typeof(LoginResult.InvalidCredentials));
    }

    [TestMethod]
    [DataRow("testuser", "wrongpassword")]
    public async Task Login_WrongPassword_ReturnsInvalidCredentials(string identifier, string password)
    {
        // Arrange
        var dto = new LoginDto { Identifier = identifier, Password = password };
        _mockUsersRepo.Setup(userRepo => userRepo.GetByEmail(identifier)).ReturnsAsync((UserModel?)null);
        _mockUsersRepo.Setup(userRepo => userRepo.GetByUsername(identifier)).ReturnsAsync(_defaultUser);
        _mockHasher.Setup(hasher => hasher.VerifyHashedPassword(_defaultUser, "hashed_password", password))
            .Returns(PasswordVerificationResult.Failed);

        // Act
        var result = await _userService.Login(dto);

        // Assert
        Assert.IsInstanceOfType(result, typeof(LoginResult.InvalidCredentials));
    }

    [TestMethod]
    [DataRow("testuser", "password123")]
    public async Task Login_TokenCreationFails_ReturnsError(string identifier, string password)
    {
        // Arrange
        var dto = new LoginDto { Identifier = identifier, Password = password };
        _mockUsersRepo.Setup(userRepo => userRepo.GetByEmail(identifier)).ReturnsAsync((UserModel?)null);
        _mockUsersRepo.Setup(userRepo => userRepo.GetByUsername(identifier)).ReturnsAsync(_defaultUser);
        _mockHasher.Setup(hasher => hasher.VerifyHashedPassword(_defaultUser, "hashed_password", password))
            .Returns(PasswordVerificationResult.Success);

        _mockSessionService.Setup(session => session.CreateSession(_defaultUser))
            .Returns(new CreateJwtResult.ConfigError("JWT Key not configured."));

        // Act
        var result = await _userService.Login(dto);

        // Assert
        Assert.IsInstanceOfType(result, typeof(LoginResult.Error));
        StringAssert.Contains(((LoginResult.Error)result).Message, "Failed to create authentication token");
    }

    #endregion

    #region GetById

    [TestMethod]
    [DataRow(1L)]
    public async Task GetUserById_Found_ReturnsSuccess(long id)
    {
        // Arrange
        _mockUsersRepo.Setup(userRepo => userRepo.GetById<UserModel>(id)).ReturnsAsync(_defaultUser);

        // Act
        var result = await _userService.GetUserById(id);

        // Assert
        Assert.IsInstanceOfType(result, typeof(GetUserResult.Success));
    }

    [TestMethod]
    [DataRow(99L)]
    public async Task GetUserById_NotFound_ReturnsNotFound(long id)
    {
        // Arrange
        _mockUsersRepo.Setup(userRepo => userRepo.GetById<UserModel>(id)).ReturnsAsync((UserModel?)null);

        // Act
        var result = await _userService.GetUserById(id);

        // Assert
        Assert.IsInstanceOfType(result, typeof(GetUserResult.NotFound));
    }

    #endregion

    #region GetByVariousCriteria

    [TestMethod]
    [DataRow("testuser")]
    public async Task GetUserByUsername_Found_ReturnsSuccess(string username)
    {
        // Arrange
        _mockUsersRepo.Setup(userRepo => userRepo.GetByUsername(username)).ReturnsAsync(_defaultUser);

        // Act
        var result = await _userService.GetUserByUsername(username);

        // Assert
        Assert.IsInstanceOfType(result, typeof(GetUserResult.Success));
        Assert.AreEqual(username, ((GetUserResult.Success)result).User.Username);
    }

    [TestMethod]
    [DataRow("nonexistentuser")]
    public async Task GetUserByUsername_NotFound_ReturnsNotFound(string username)
    {
        // Arrange
        _mockUsersRepo.Setup(userRepo => userRepo.GetByUsername(username)).ReturnsAsync((UserModel?)null);

        // Act
        var result = await _userService.GetUserByUsername(username);

        // Assert
        Assert.IsInstanceOfType(result, typeof(GetUserResult.NotFound));
    }

    [TestMethod]
    [DataRow("test@user.com")]
    public async Task GetUserByEmail_Found_ReturnsSuccess(string email)
    {
        // Arrange
        _mockUsersRepo.Setup(userRepo => userRepo.GetByEmail(email)).ReturnsAsync(_defaultUser);

        // Act
        var result = await _userService.GetUserByEmail(email);

        // Assert
        Assert.IsInstanceOfType(result, typeof(GetUserResult.Success));
        Assert.AreEqual(email, ((GetUserResult.Success)result).User.Email);
    }

    [TestMethod]
    [DataRow("no@email.com")]
    public async Task GetUserByEmail_NotFound_ReturnsNotFound(string email)
    {
        // Arrange
        _mockUsersRepo.Setup(userRepo => userRepo.GetByEmail(email)).ReturnsAsync((UserModel?)null);

        // Act
        var result = await _userService.GetUserByEmail(email);

        // Assert
        Assert.IsInstanceOfType(result, typeof(GetUserResult.NotFound));
    }

    [TestMethod]
    public async Task GetAllUsers_Found_ReturnsSuccessList()
    {
        // Arrange
        var list = new List<UserModel> { _defaultUser, _adminUser };
        _mockUsersRepo.Setup(userRepo => userRepo.GetAll()).ReturnsAsync(list);

        // Act
        var result = await _userService.GetAllUsers();

        // Assert
        Assert.IsInstanceOfType(result, typeof(GetUserListResult.Success));
        Assert.AreEqual(2, ((GetUserListResult.Success)result).Users.Count);
    }

    [TestMethod]
    public async Task GetAllUsers_NotFound_ReturnsNotFound()
    {
        // Arrange
        _mockUsersRepo.Setup(userRepo => userRepo.GetAll()).ReturnsAsync(new List<UserModel>());

        // Act
        var result = await _userService.GetAllUsers();

        // Assert
        Assert.IsInstanceOfType(result, typeof(GetUserListResult.NotFound));
    }

    #endregion

    #region Count

    [TestMethod]
    [DataRow(42)]
    public async Task CountUsers_ReturnsCount(int count)
    {
        // Arrange
        _mockUsersRepo.Setup(userRepo => userRepo.Count()).ReturnsAsync(count);
        // Act
        var result = await _userService.CountUsers();
        // Assert
        Assert.AreEqual(count, result);
    }

    #endregion

    #region Delete

    [TestMethod]
    [DataRow(1L)]
    public async Task DeleteUser_Success_ReturnsSuccess(long id)
    {
        // Arrange
        _mockUsersRepo.Setup(userRepo => userRepo.GetById<UserModel>(id)).ReturnsAsync(_defaultUser);
        _mockUsersRepo.Setup(userRepo => userRepo.Delete(_defaultUser)).ReturnsAsync(true);
        // Act
        var result = await _userService.DeleteUser(id);
        // Assert
        Assert.IsInstanceOfType(result, typeof(DeleteUserResult.Success));
    }

    [TestMethod]
    [DataRow(99L)]
    public async Task DeleteUser_NotFound_ReturnsNotFound(long id)
    {
        // Arrange
        _mockUsersRepo.Setup(userRepo => userRepo.GetById<UserModel>(id)).ReturnsAsync((UserModel?)null);
        // Act
        var result = await _userService.DeleteUser(id);
        // Assert
        Assert.IsInstanceOfType(result, typeof(DeleteUserResult.NotFound));
    }

    [TestMethod]
    [DataRow(1L)]
    public async Task DeleteUser_DbDeleteFails_ReturnsError(long id)
    {
        // Arrange
        _mockUsersRepo.Setup(userRepo => userRepo.GetById<UserModel>(id)).ReturnsAsync(_defaultUser);
        _mockUsersRepo.Setup(userRepo => userRepo.Delete(_defaultUser)).ReturnsAsync(false);
        // Act
        var result = await _userService.DeleteUser(id);
        // Assert
        Assert.IsInstanceOfType(result, typeof(DeleteUserResult.Error));
    }

    #endregion

    #region UpdateUserProfile

    [TestMethod]
    [DataRow(1L, "newuser", "new@email.com", "+31687654321")]
    public async Task UpdateUserProfile_ValidChanges_ReturnsSuccess(long id, string newUsername, string newEmail, string newPhone)
    {
        // Arrange
        var dto = new UpdateUserDto { Username = newUsername, Email = newEmail, Phone = newPhone };
        _mockUsersRepo.Setup(userRepo => userRepo.GetById<UserModel>(id)).ReturnsAsync(_defaultUser);
        _mockUsersRepo.Setup(userRepo => userRepo.GetByUsername(newUsername)).ReturnsAsync((UserModel?)null);
        _mockUsersRepo.Setup(userRepo => userRepo.GetByEmail(newEmail)).ReturnsAsync((UserModel?)null);
        _mockUsersRepo.Setup(userRepo => userRepo.Update(It.IsAny<UserModel>(), It.IsAny<UserModel>())).ReturnsAsync(true);

        // Act
        var result = await _userService.UpdateUserProfile(id, dto);

        // Assert
        Assert.IsInstanceOfType(result, typeof(UpdateUserResult.Success));
    }

    [TestMethod]
    [DataRow(1L, "newuser")]
    public async Task UpdateUserProfile_UsernameTaken_ReturnsUsernameTaken(long id, string newUsername)
    {
        // Arrange
        var dto = new UpdateUserDto { Username = newUsername };
        _mockUsersRepo.Setup(userRepo => userRepo.GetById<UserModel>(id)).ReturnsAsync(_defaultUser);
        _mockUsersRepo.Setup(userRepo => userRepo.GetByUsername(newUsername)).ReturnsAsync(new UserModel { Id = 99L });

        // Act
        var result = await _userService.UpdateUserProfile(id, dto);

        // Assert
        Assert.IsInstanceOfType(result, typeof(UpdateUserResult.UsernameTaken));
    }

    [TestMethod]
    [DataRow(1L, "new@email.com")]
    public async Task UpdateUserProfile_EmailTaken_ReturnsEmailTaken(long id, string newEmail)
    {
        // Arrange
        var dto = new UpdateUserDto { Email = newEmail };
        _mockUsersRepo.Setup(userRepo => userRepo.GetById<UserModel>(id)).ReturnsAsync(_defaultUser);
        _mockUsersRepo.Setup(userRepo => userRepo.GetByEmail(newEmail)).ReturnsAsync(new UserModel { Id = 99L });

        // Act
        var result = await _userService.UpdateUserProfile(id, dto);

        // Assert
        Assert.IsInstanceOfType(result, typeof(UpdateUserResult.EmailTaken));
    }

    [TestMethod]
    [DataRow(1L, "weak")]
    public async Task UpdateUserProfile_InvalidPassword_ReturnsInvalidData(long id, string newPassword)
    {
        // Arrange
        var dto = new UpdateUserDto { Password = newPassword };
        _mockUsersRepo.Setup(userRepo => userRepo.GetById<UserModel>(id)).ReturnsAsync(_defaultUser);

        // Act
        var result = await _userService.UpdateUserProfile(id, dto);

        // Assert
        Assert.IsInstanceOfType(result, typeof(UpdateUserResult.InvalidData));
        StringAssert.Contains(((UpdateUserResult.InvalidData)result).Message, "Password");
    }

    #endregion

    #region UpdateIdentity

    [TestMethod]
    [DataRow(1L, "NewFirst", "NewLast", "1990-01-01")]
    public async Task UpdateUserIdentity_ValidChanges_ReturnsSuccess(long id, string first, string last, string bday)
    {
        // Arrange
        var birthday = DateTimeOffset.Parse(bday + "T00:00:00Z");
        var dto = new UpdateUserIdentityDto { FirstName = first, LastName = last, Birthday = birthday };
        _mockUsersRepo.Setup(userRepo => userRepo.GetById<UserModel>(id)).ReturnsAsync(_defaultUser);
        _mockUsersRepo.Setup(userRepo => userRepo.Update(It.IsAny<UserModel>(), It.IsAny<UserModel>())).ReturnsAsync(true);

        // Act
        var result = await _userService.UpdateUserIdentity(id, dto);

        // Assert
        Assert.IsInstanceOfType(result, typeof(UpdateUserResult.Success));
    }

    [TestMethod]
    [DataRow(1L, "2100-01-01")]
    [DataRow(1L, "1899-01-01")]
    public async Task UpdateUserIdentity_InvalidBirthday_ReturnsInvalidData(long id, string bday)
    {
        // Arrange
        var birthday = DateTimeOffset.Parse(bday + "T00:00:00Z");
        var dto = new UpdateUserIdentityDto { Birthday = birthday };
        _mockUsersRepo.Setup(userRepo => userRepo.GetById<UserModel>(id)).ReturnsAsync(_defaultUser);

        // Act
        var result = await _userService.UpdateUserIdentity(id, dto);

        // Assert
        Assert.IsInstanceOfType(result, typeof(UpdateUserResult.InvalidData));
    }

    [TestMethod]
    [DataRow(1L)]
    public async Task UpdateUserIdentity_BirthdayTooYoung_ReturnsInvalidData(long id)
    {
        // Arrange
        var birthday = DateTimeOffset.UtcNow.AddYears(-10);
        var dto = new UpdateUserIdentityDto { Birthday = birthday };
        _mockUsersRepo.Setup(userRepo => userRepo.GetById<UserModel>(id)).ReturnsAsync(_defaultUser);

        // Act
        var result = await _userService.UpdateUserIdentity(id, dto);

        // Assert
        Assert.IsInstanceOfType(result, typeof(UpdateUserResult.InvalidData));
        StringAssert.Contains(((UpdateUserResult.InvalidData)result).Message, "at least 16");
    }

    [TestMethod]
    [DataRow(99L)]
    public async Task UpdateUserIdentity_UserNotFound_ReturnsNotFound(long id)
    {
        // Arrange
        var dto = new UpdateUserIdentityDto();
        _mockUsersRepo.Setup(userRepo => userRepo.GetById<UserModel>(id)).ReturnsAsync((UserModel?)null);

        // Act
        var result = await _userService.UpdateUserIdentity(id, dto);

        // Assert
        Assert.IsInstanceOfType(result, typeof(UpdateUserResult.NotFound));
    }

    #endregion

    #region UpdateRole

    [TestMethod]
    [DataRow(1L, 2L)]
    public async Task UpdateUserRole_ValidChange_ReturnsSuccess(long id, long newRoleId)
    {
        // Arrange
        var dto = new UpdateUserRoleDto { RoleId = newRoleId };
        var newRole = new RoleModel { Id = newRoleId, Name = "MODERATOR" };
        _mockUsersRepo.Setup(userRepo => userRepo.GetById<UserModel>(id)).ReturnsAsync(_defaultUser);
        _mockRolesRepo.Setup(userRepo => userRepo.GetById<RoleModel>(newRoleId)).ReturnsAsync(newRole);
        _mockUsersRepo.Setup(userRepo => userRepo.Update(It.IsAny<UserModel>(), It.IsAny<UserModel>())).ReturnsAsync(true);

        // Act
        var result = await _userService.UpdateUserRole(id, dto);

        // Assert
        Assert.IsInstanceOfType(result, typeof(UpdateUserResult.Success));
    }

    [TestMethod]
    [DataRow(1L, 1L)]
    public async Task UpdateUserRole_NoChanges_ReturnsNoChangesMade(long id, long newRoleId)
    {
        // Arrange
        var dto = new UpdateUserRoleDto { RoleId = newRoleId };
        _mockUsersRepo.Setup(userRepo => userRepo.GetById<UserModel>(id)).ReturnsAsync(_adminUser);

        // Act
        var result = await _userService.UpdateUserRole(id, dto);

        // Assert
        Assert.IsInstanceOfType(result, typeof(UpdateUserResult.NoChangesMade));
    }

    [TestMethod]
    [DataRow(1L, 99L)]
    public async Task UpdateUserRole_RoleNotFound_ReturnsInvalidData(long id, long newRoleId)
    {
        // Arrange
        var dto = new UpdateUserRoleDto { RoleId = newRoleId };
        _mockUsersRepo.Setup(userRepo => userRepo.GetById<UserModel>(id)).ReturnsAsync(_defaultUser);
        _mockRolesRepo.Setup(roleRepo => roleRepo.GetById<RoleModel>(newRoleId)).ReturnsAsync((RoleModel?)null);

        // Act
        var result = await _userService.UpdateUserRole(id, dto);

        // Assert
        Assert.IsInstanceOfType(result, typeof(UpdateUserResult.InvalidData));
        StringAssert.Contains(((UpdateUserResult.InvalidData)result).Message, "does not exist");
    }

    [TestMethod]
    [DataRow(2L, 3L)]
    public async Task UpdateUserRole_CannotChangeAdmin_ReturnsInvalidData(long id, long newRoleId)
    {
        // Arrange
        var dto = new UpdateUserRoleDto { RoleId = newRoleId };
        var newRole = new RoleModel { Id = newRoleId, Name = "IT MANAGER" };

        _mockUsersRepo.Setup(userRepo => userRepo.GetById<UserModel>(id)).ReturnsAsync(_adminUser);
        _mockRolesRepo.Setup(roleRepo => roleRepo.GetById<RoleModel>(newRoleId)).ReturnsAsync(newRole);

        // Act
        var result = await _userService.UpdateUserRole(id, dto);

        Assert.IsInstanceOfType(result, typeof(UpdateUserResult.InvalidData));
        StringAssert.Contains(((UpdateUserResult.InvalidData)result).Message, "Cannot change role of an ADMIN");
    }

    #endregion

    #region Validation

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
    [DataRow("USER@DOMAIN.COM", "USER@domain.com")] // Added from my previous example
    public async Task CreateUserAsync_ValidEmailFormats_CreatesUserWithNormalizedEmail(string inputEmail, string expectedNormalized)
    {
        // Arrange
        var dto = new RegisterDto
        {
            Username = "ValidUser", Password = "ValidPassword1!", ConfirmPassword = "ValidPassword1!",
            FirstName = "Test", LastName = "User", Email = inputEmail, Phone = "0612345678",
            Birthday = DateTimeOffset.UtcNow.AddYears(-20)
        };

        _mockUsersRepo.Setup(userRepo => userRepo.GetByUsername("ValidUser")).ReturnsAsync((UserModel?)null);
        _mockUsersRepo.Setup(userRepo => userRepo.CreateWithId(It.IsAny<UserModel>())).ReturnsAsync((true, 1L));
        _mockHasher.Setup(hasher => hasher.HashPassword(It.IsAny<UserModel>(), "ValidPassword1!")).Returns("hashed_password");

        // Act
        var result = await _userService.CreateUserAsync(dto);

        // Assert
        Assert.IsInstanceOfType(result, typeof(RegisterResult.Success));
        _mockUsersRepo.Verify(userRepo => userRepo.CreateWithId(It.Is<UserModel>(user => user.Email == expectedNormalized)), Times.Once);
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
    public async Task CreateUserAsync_InvalidEmailFormats_ReturnsInvalidData(string email)
    {
        // Arrange
        var dto = new RegisterDto
        {
            Username = "ValidUser", Password = "ValidPassword1!", ConfirmPassword = "ValidPassword1!",
            FirstName = "Test", LastName = "User", Email = email, Phone = "0612345678",
            Birthday = DateTimeOffset.UtcNow.AddYears(-20)
        };

        _mockUsersRepo.Setup(userRepo => userRepo.GetByUsername("ValidUser")).ReturnsAsync((UserModel?)null);

        // Act
        var result = await _userService.CreateUserAsync(dto);

        // Assert
        Assert.IsInstanceOfType(result, typeof(RegisterResult.InvalidData));
        StringAssert.Contains(((RegisterResult.InvalidData)result).Message, "Email");
        _mockUsersRepo.Verify(userRepo => userRepo.CreateWithId(It.IsAny<UserModel>()), Times.Never);
    }

    [TestMethod]
    [DataRow("0612345678", "+31612345678")]
    [DataRow("06 12345678", "+31612345678")]
    [DataRow("+31 6 12345678", "+31612345678")]
    [DataRow("+31612345678", "+31612345678")]
    [DataRow("00316 12345678", "+31612345678")]
    [DataRow("0031612345678", "+31612345678")]
    [DataRow("(06)12345678", "+31612345678")]
    [DataRow("06-12345678", "+31612345678")]
    [DataRow("06-12-34-56-78", "+31612345678")]
    [DataRow("612345678", "+31612345678")]
    [DataRow("+31 (0)6 12345678", "+31612345678")]
    [DataRow("0031 06 12345678", "+31612345678")]
    [DataRow("++31612345678", "+31612345678")]
    [DataRow(" 06  1234  5678 ", "+31612345678")]
    [DataRow("+310612345678", "+31612345678")]
    [DataRow("0101234567", "+31101234567")]
    [DataRow("010 1234567", "+31101234567")]
    [DataRow("+31 10 1234567", "+31101234567")]
    [DataRow("+31101234567", "+31101234567")]
    [DataRow("0031 10 1234567", "+31101234567")]
    [DataRow("0031515123456", "+31515123456")]
    [DataRow("(0515)123456", "+31515123456")]
    [DataRow("0515-123456", "+31515123456")]
    [DataRow("0515-12-34-56", "+31515123456")]
    [DataRow("515123456", "+31515123456")]
    [DataRow("+31 (0)76 1234567", "+31761234567")]
    [DataRow("0031 076 1234567", "+31761234567")]
    [DataRow("++310761234567", "+31761234567")]
    [DataRow(" 076  1234  567 ", "+31761234567")]
    [DataRow("+310761234567", "+31761234567")]
    [DataRow("06.1234.5678", "+31612345678")]    //        .
    [DataRow("06#1234#5678", "+31612345678")]    //        #
    [DataRow("06/1234/5678", "+31612345678")]    //        /
    [DataRow("06_1234_5678", "+31612345678")]    //        _
    [DataRow("06*1234*5678", "+31612345678")]    //        *
    [DataRow("06+1234+5678", "+31612345678")]    //        +
    [DataRow("06=1234=5678", "+31612345678")]    //        =
    [DataRow("06@1234@5678", "+31612345678")]    //        @
    [DataRow("06!1234!5678", "+31612345678")]    //        !
    [DataRow("06$1234$5678", "+31612345678")]    //        $
    [DataRow("06%1234%5678", "+31612345678")]    //        %
    [DataRow("06^1234^5678", "+31612345678")]    //        ^
    [DataRow("06&1234&5678", "+31612345678")]    //        &
    [DataRow("06{1234}5678", "+31612345678")]    //        {}
    [DataRow("06[1234]5678", "+31612345678")]    //        []
    [DataRow("06|1234|5678", "+31612345678")]    //        |
    [DataRow("06;1234;5678", "+31612345678")]    //        ;
    [DataRow("06:1234:5678", "+31612345678")]    //        :
    [DataRow("06'1234'5678", "+31612345678")]    //        '
    [DataRow("06\"1234\"5678", "+31612345678")]  //        "
    [DataRow("06<1234>5678", "+31612345678")]    //        <>
    [DataRow("06,1234,5678", "+31612345678")]    //        ,
    [DataRow("06?1234?5678", "+31612345678")]    //        ?
    [DataRow("06`1234`5678", "+31612345678")]    //        `
    [DataRow("06~1234~5678", "+31612345678")]    //        ~
    public async Task CreateUserAsync_ValidPhoneFormats_CreatesUserWithNormalizedPhone(string phone, string expected)
    {
        // Arrange
        var dto = new RegisterDto
        {
            Username = "ValidUser", Password = "ValidPassword1!", ConfirmPassword = "ValidPassword1!",
            FirstName = "Test", LastName = "User", Email = "test@test.com", Phone = phone,
            Birthday = DateTimeOffset.UtcNow.AddYears(-20)
        };

        _mockUsersRepo.Setup(userRepo => userRepo.GetByUsername("ValidUser")).ReturnsAsync((UserModel?)null);
        _mockUsersRepo.Setup(userRepo => userRepo.CreateWithId(It.IsAny<UserModel>())).ReturnsAsync((true, 1L));
        _mockHasher.Setup(hasher => hasher.HashPassword(It.IsAny<UserModel>(), "ValidPassword1!")).Returns("hashed_password");

        // Act
        var result = await _userService.CreateUserAsync(dto);

        // Assert
        Assert.IsInstanceOfType(result, typeof(RegisterResult.Success));
        _mockUsersRepo.Verify(userRepo => userRepo.CreateWithId(It.Is<UserModel>(user => user.Phone == expected)), Times.Once);
    }

    [TestMethod]
    [DataRow("061234567")]
    [DataRow("012345678901")]
    [DataRow("A061234567")]
    [DataRow("061234A567")]
    [DataRow("061234567A")]
    [DataRow("+32 612345678")]
    public async Task CreateUserAsync_InvalidPhoneFormats_ReturnsInvalidData(string phone)
    {
        // Arrange
        var dto = new RegisterDto
        {
            Username = "ValidUser", Password = "ValidPassword1!", ConfirmPassword = "ValidPassword1!",
            FirstName = "Test", LastName = "User", Email = "test@test.com", Phone = phone,
            Birthday = DateTimeOffset.UtcNow.AddYears(-20)
        };

        _mockUsersRepo.Setup(userRepo => userRepo.GetByUsername("ValidUser")).ReturnsAsync((UserModel?)null);

        // Act
        var result = await _userService.CreateUserAsync(dto);

        // Assert
        Assert.IsInstanceOfType(result, typeof(RegisterResult.InvalidData));
        StringAssert.Contains(((RegisterResult.InvalidData)result).Message, "Phone number");
        _mockUsersRepo.Verify(userRepo => userRepo.CreateWithId(It.IsAny<UserModel>()), Times.Never);
    }

    #endregion
}
