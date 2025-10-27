using MobyPark.Services;

namespace MobyPark.Tests;

[TestClass]
public sealed class SystemServiceTests
{
    #region GenerateGuid
    [TestMethod]
    [DataRow("test")]
    [DataRow("hello world")]
    [DataRow("123456")]
    [DataRow("special-characters-!@#$%^&*()")]
    [DataRow("")]
    public void GenerateGuid_SameInput_ReturnsConsistentGuid(string input)
    {
        // Act
        var guid1 = SystemService.GenerateGuid(input);
        var guid2 = SystemService.GenerateGuid(input);

        // Assert
        Assert.AreEqual(guid1, guid2);
    }

    [TestMethod]
    [DataRow("input1", "input2")]
    [DataRow("foo", "bar")]
    [DataRow("guid_test_A", "guid_test_B")]
    [DataRow("same-prefix-1", "same-prefix-2")]
    public void GenerateGuid_DifferentInputs_ReturnDifferentGuids(string input1, string input2)
    {
        // Act
        var guid1 = SystemService.GenerateGuid(input1);
        var guid2 = SystemService.GenerateGuid(input2);

        // Assert
        Assert.AreNotEqual(guid1, guid2);
    }

    [TestMethod]
    [DataRow("empty")]
    [DataRow("some random string")]
    [DataRow("another one")]
    public void GenerateGuid_ReturnsValidGuid(string input)
    {
        // Act
        var guid = SystemService.GenerateGuid(input);

        // Assert
        Assert.AreNotEqual(Guid.Empty, guid);
        Assert.IsTrue(Guid.TryParse(guid.ToString(), out _));
    }

    #endregion
}
