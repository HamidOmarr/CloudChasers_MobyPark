using MobyPark.Models;
using MobyPark.Services;

namespace MobyPark.Tests;

[TestClass]
public sealed class SessionServiceTests
{
    private SessionService? _sessionService;

    [TestInitialize]
    public void TestInitialize()
    {
        _sessionService = new SessionService();
    }

    [TestMethod]
    [DataRow("token1", "user1")]
    [DataRow("token2", "user2")]
    [DataRow("token3", "admin")]
    [DataRow("abc123", "guest")]
    [DataRow("xyz789", "tester")]
    public void AddSession_StoresUserSession(string token, string username)
    {
        // Arrange
        var user = new UserModel { Username = username };

        // Act
        _sessionService!.AddSession(token, user);
        var result = _sessionService.GetSession(token);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(username, result.Username);
    }

    [TestMethod]
    [DataRow("dupToken1", "firstUser", "secondUser")]
    [DataRow("dupToken2", "alpha", "beta")]
    [DataRow("dupToken3", "initial", "override")]
    public void AddSession_SameToken_OverridesUser(string token, string firstUser, string secondUser)
    {
        // Arrange
        var user1 = new UserModel { Username = firstUser };
        var user2 = new UserModel { Username = secondUser };

        _sessionService!.AddSession(token, user1);

        // Act
        _sessionService.AddSession(token, user2);
        var result = _sessionService.GetSession(token);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(secondUser, result.Username);
    }

    [TestMethod]
    [DataRow("removeToken1", "toRemove1")]
    [DataRow("removeToken2", "toRemove2")]
    [DataRow("removeToken3", "toRemove3")]
    public void RemoveSession_ExistingToken_RemovesAndReturnsUser(string token, string username)
    {
        // Arrange
        var user = new UserModel { Username = username };
        _sessionService!.AddSession(token, user);

        // Act
        var removed = _sessionService.RemoveSession(token);
        var afterRemove = _sessionService.GetSession(token);

        // Assert
        Assert.IsNotNull(removed);
        Assert.AreEqual(username, removed.Username);
        Assert.IsNull(afterRemove);
    }

    [TestMethod]
    [DataRow("missing1")]
    [DataRow("missing2")]
    [DataRow("missing3")]
    public void RemoveSession_NonExistentToken_ReturnsNull(string token)
    {
        // Act
        var result = _sessionService!.RemoveSession(token);

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    [DataRow("ghost1")]
    [DataRow("ghost2")]
    [DataRow("ghost3")]
    public void GetSession_NonExistentToken_ReturnsNull(string token)
    {
        // Act
        var result = _sessionService!.GetSession(token);

        // Assert
        Assert.IsNull(result);
    }
}
