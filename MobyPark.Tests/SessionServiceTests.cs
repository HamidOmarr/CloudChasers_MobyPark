using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

using MobyPark.Models;
using MobyPark.Services;
using MobyPark.Services.Results.Session;

using Moq;

namespace MobyPark.Tests;

[TestClass]
public sealed class SessionServiceTests
{
    #region Setup
    private Mock<IConfiguration> _mockConfig = null!;
    private SessionService _sessionService = null!;

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

        _sessionService = new SessionService(_mockConfig.Object);
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
        var tokenResult = _sessionService.CreateSession(user);


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

        var serviceWithBadConfig = new SessionService(badConfig.Object);
        var user = new UserModel { Id = 1, Username = "user", Email = "e", Role = new RoleModel { Name = "r" } };

        // Act
        var result = serviceWithBadConfig.CreateSession(user);

        // Assert
        Assert.IsInstanceOfType(result, typeof(CreateJwtResult.ConfigError));
        var configError = (result as CreateJwtResult.ConfigError)!.Message;
        StringAssert.Contains(configError, "JWT secret key is not configured");
    }

    #endregion
}