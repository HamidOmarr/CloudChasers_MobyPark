using System.IdentityModel.Tokens.Jwt;
using System.Linq.Expressions;
using System.Security.Claims;
using System.Text;

using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

using MobyPark.DTOs.Token;
using MobyPark.Models;
using MobyPark.Models.Repositories.Interfaces;
using MobyPark.Services;
using MobyPark.Services.Results.Tokens;

using Moq;

namespace MobyPark.Tests;

[TestClass]
public sealed class TokenServiceTests
{
    #region Setup
    private Mock<IConfiguration> _mockConfig = null!;
    private Mock<IRepository<UserModel>> _mockUserRepo = null!;
    private TokenService _tokenService = null!;

    private const string TestSecretKey = "ThisIsMySuperSecretTestKeyForHmacSha256";
    private const string TestIssuer = "TestIssuer";
    private const string TestAudience = "TestAudience";

    [TestInitialize]
    public void TestInitialize()
    {
        _mockConfig = new Mock<IConfiguration>();
        _mockConfig.Setup(config => config["Jwt:Key"]).Returns(TestSecretKey);
        _mockConfig.Setup(config => config["Jwt:Issuer"]).Returns(TestIssuer);
        _mockConfig.Setup(config => config["Jwt:Audience"]).Returns(TestAudience);

        _mockUserRepo = new Mock<IRepository<UserModel>>();

        _tokenService = new TokenService(_mockConfig.Object, _mockUserRepo.Object);
    }

    #endregion

    #region CreateValid

    [TestMethod]
    [DataRow(1, "testuser", "test@email.com", "User")]
    [DataRow(99, "admin", "admin@email.com", "Admin")]
    public void CreateSession_ValidUser_ReturnsTokenWithCorrectClaims(
        long id, string username, string email, string role)
    {
        // Arrange
        var user = new UserModel
        {
            Id = id,
            Username = username,
            Email = email,
            Role = new RoleModel { Name = role }
        };

        // Act
        var tokenResult = _tokenService.CreateToken(user);


        // Assert
        Assert.IsInstanceOfType(tokenResult, typeof(CreateJwtResult.Success));

        string token = (tokenResult as CreateJwtResult.Success)!.JwtToken;

        Assert.IsFalse(string.IsNullOrWhiteSpace(token));

        var tokenHandler = new JwtSecurityTokenHandler();
        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestSecretKey)),
            ValidateIssuer = true,
            ValidIssuer = TestIssuer,
            ValidateAudience = true,
            ValidAudience = TestAudience,
            ValidateLifetime = false
        };

        var principal = tokenHandler.ValidateToken(token, validationParameters, out SecurityToken validatedToken);
        Assert.IsNotNull(principal);
        Assert.AreEqual(id.ToString(), principal.FindFirstValue(ClaimTypes.NameIdentifier));
        Assert.AreEqual(username, principal.FindFirstValue(ClaimTypes.Name));
        Assert.AreEqual(email, principal.FindFirstValue(ClaimTypes.Email));
        Assert.AreEqual(role, principal.FindFirstValue(ClaimTypes.Role));
    }

    #endregion

    #region CreateInvalid

    [TestMethod]
    public void CreateSession_MissingConfigKey_ThrowsInvalidOperationException()
    {
        // Arrange
        var badConfig = new Mock<IConfiguration>();
        badConfig.Setup(config => config["Jwt:Key"]).Returns((string?)null);  // Missing key
        badConfig.Setup(config => config["Jwt:Issuer"]).Returns(TestIssuer);
        badConfig.Setup(config => config["Jwt:Audience"]).Returns(TestAudience);

        var serviceWithBadConfig = new TokenService(badConfig.Object, _mockUserRepo.Object);
        var user = new UserModel { Id = 1, Username = "user", Email = "e", Role = new RoleModel { Name = "r" } };

        // Act
        var result = serviceWithBadConfig.CreateToken(user);

        // Assert
        Assert.IsInstanceOfType(result, typeof(CreateJwtResult.ConfigError));
        var configError = (result as CreateJwtResult.ConfigError)!.Message;
        StringAssert.Contains(configError, "JWT secret key is not configured");
    }

    #endregion

    #region ValidateToken

    [TestMethod]
    public async Task RefreshToken_ValidToken_ReturnsNewTokenAndUpdatesUser()
    {
        // Arrange
        var refreshToken = "old-refresh-token";
        var slidingExpiry = DateTimeOffset.UtcNow.AddMinutes(30);
        var absoluteExpiry = DateTimeOffset.UtcNow.AddDays(7);
        var user = new UserModel
        {
            Id = 1,
            Username = "test",
            Email = "test@test.com",
            Role = new RoleModel { Name = "User" },
            RefreshToken = refreshToken,
            SlidingTokenExpiryTime = slidingExpiry,
            AbsoluteTokenExpiryTime = absoluteExpiry
        };

        _mockUserRepo.Setup(repo => repo.GetSingleByAsync(It.IsAny<Expression<Func<UserModel, bool>>>()))
            .ReturnsAsync(user);

        // Act
        var result = await _tokenService.RefreshToken(refreshToken);

        // Assert
        Assert.IsInstanceOfType(result, typeof(TokenRefreshResult.Success));
        var success = (result as TokenRefreshResult.Success)!;

        Assert.IsFalse(string.IsNullOrWhiteSpace(success.RefreshToken));
        _mockUserRepo.Verify(r => r.Update(user, It.IsAny<TokenDto>()), Times.Once);
    }

    [TestMethod]
    public async Task RefreshToken_HitsAbsoluteLimit_ReturnsInvalidToken()
    {
        // Arrange
        var refreshToken = "active-but-old-session";
        var user = new UserModel
        {
            RefreshToken = refreshToken,
            SlidingTokenExpiryTime = DateTimeOffset.UtcNow.AddMinutes(20),
            AbsoluteTokenExpiryTime = DateTimeOffset.UtcNow.AddMinutes(-1)
        };

        _mockUserRepo.Setup(repo => repo.GetSingleByAsync(It.IsAny<Expression<Func<UserModel, bool>>>()))
            .ReturnsAsync(user);

        // Act
        var result = await _tokenService.RefreshToken(refreshToken);

        // Assert
        Assert.IsInstanceOfType(result, typeof(TokenRefreshResult.InvalidToken));
        Assert.AreEqual("Session has reached maximum duration (7 days).", (result as TokenRefreshResult.InvalidToken)!.Message);
    }

    [TestMethod]
    public async Task RefreshToken_InactivityTimeout_ReturnsInvalidToken()
    {
        // Arrange
        var refreshToken = "stale-token";
        var user = new UserModel
        {
            RefreshToken = refreshToken,
            SlidingTokenExpiryTime = DateTimeOffset.UtcNow.AddMinutes(-5),
            AbsoluteTokenExpiryTime = DateTimeOffset.UtcNow.AddDays(5)
        };

        _mockUserRepo.Setup(repo => repo.GetSingleByAsync(It.IsAny<Expression<Func<UserModel, bool>>>()))
            .ReturnsAsync(user);

        // Act
        var result = await _tokenService.RefreshToken(refreshToken);

        // Assert
        Assert.IsInstanceOfType(result, typeof(TokenRefreshResult.InvalidToken));
        Assert.AreEqual("Refresh token has expired due to inactivity.", (result as TokenRefreshResult.InvalidToken)!.Message);
    }

    [TestMethod]
    public async Task RefreshToken_NonExistentToken_ReturnsInvalidToken()
    {
        // Arrange
        _mockUserRepo.Setup(repo => repo.GetSingleByAsync(It.IsAny<Expression<Func<UserModel, bool>>>()))
            .ReturnsAsync((UserModel?)null);

        // Act
        var result = await _tokenService.RefreshToken("fake-token");

        // Assert
        Assert.IsInstanceOfType(result, typeof(TokenRefreshResult.InvalidToken));
        Assert.AreEqual("Invalid refresh token.", (result as TokenRefreshResult.InvalidToken)!.Message);
    }

    [TestMethod]
    public async Task RefreshToken_NearAbsoluteLimit_CapsSlidingExpiryToAbsoluteLimit()
    {
        // Arrange
        var refreshToken = "near-limit-token";
        var absoluteLimit = DateTimeOffset.UtcNow.AddMinutes(5);
        var user = new UserModel
        {
            Id = 1,
            Username = "u",
            Email = "e",
            Role = new RoleModel { Name = "r" },
            RefreshToken = refreshToken,
            SlidingTokenExpiryTime = DateTimeOffset.UtcNow.AddMinutes(2),
            AbsoluteTokenExpiryTime = absoluteLimit
        };

        _mockUserRepo.Setup(repo => repo.GetSingleByAsync(It.IsAny<Expression<Func<UserModel, bool>>>()))
            .ReturnsAsync(user);

        TokenDto? capturedDto = null;
        _mockUserRepo.Setup(r => r.Update(user, It.IsAny<TokenDto>()))
            .Callback<UserModel, TokenDto>((u, dto) => capturedDto = dto)
            .ReturnsAsync(true);

        // Act
        await _tokenService.RefreshToken(refreshToken);

        // Assert
        Assert.IsNotNull(capturedDto);
        Assert.AreEqual(absoluteLimit, capturedDto.SlidingTokenExpiryTime);
    }

    #endregion
}