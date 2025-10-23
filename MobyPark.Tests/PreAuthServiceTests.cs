using MobyPark.Services;

namespace MobyPark.Tests;

[TestClass]
public sealed class PreAuthServiceTests
{
    private PreAuthService _preAuthService = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _preAuthService = new PreAuthService();
    }

    [TestMethod]
    [DataRow("tok_visa", 10.00)]
    [DataRow("tok_mc", 0.01)]
    public async Task PreauthorizeAsync_ValidAmount_ReturnsApproved(string token, double amount)
    {
        // Arrange
        var decAmount = (decimal)amount;

        // Act
        var result = await _preAuthService.PreauthorizeAsync(token, decAmount, false);

        // Assert
        Assert.IsTrue(result.Approved);
        Assert.IsNull(result.Reason);
    }

    [TestMethod]
    [DataRow("tok_visa", 0.00)]
    [DataRow("tok_mc", -10.00)]
    public async Task PreauthorizeAsync_InvalidAmount_ReturnsNotApprovedWithReason(string token, double amount)
    {
        // Arrange
        var decAmount = (decimal)amount;

        // Act
        var result = await _preAuthService.PreauthorizeAsync(token, decAmount, false);

        // Assert
        Assert.IsFalse(result.Approved);
        Assert.AreEqual("Invalid amount", result.Reason);
    }

    [TestMethod]
    [DataRow("tok_visa", 10.00)]
    [DataRow("tok_mc", 50.00)]
    public async Task PreauthorizeAsync_SimulateInsufficientFunds_ReturnsNotApprovedWithReason(string token, double amount)
    {
        // Arrange
        var decAmount = (decimal)amount;

        // Act
        var result = await _preAuthService.PreauthorizeAsync(token, decAmount, true);

        // Assert
        Assert.IsFalse(result.Approved);
        Assert.AreEqual("Insufficient funds", result.Reason);
    }

    [TestMethod]
    [DataRow("tok_visa", -5.00)]
    public async Task PreauthorizeAsync_InvalidAmountAndSimulateFail_ReturnsInsufficientFunds(string token, double amount)
    {
        // Arrange
        var decAmount = (decimal)amount;

        // Act
        var result = await _preAuthService.PreauthorizeAsync(token, decAmount, true);

        // Assert
        Assert.IsFalse(result.Approved);
        Assert.AreEqual("Insufficient funds", result.Reason);
    }
}
