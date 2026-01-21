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
    [DataRow("tok_visa")]
    [DataRow("tok_mc")]
    public async Task PreauthorizeAsync_SufficientFunds_ReturnsApproved(string token)
    {
        var result = await _preAuthService.PreauthorizeAsync(token, true);

        Assert.IsTrue(result.Approved);
        Assert.AreEqual("Sufficient funds", result.Reason);
    }

    [TestMethod]
    [DataRow("tok_visa")]
    [DataRow("tok_mc")]
    public async Task PreauthorizeAsync_InsufficientFunds_ReturnsNotApprovedWithReason(string token)
    {
        var result = await _preAuthService.PreauthorizeAsync(token, false);

        Assert.IsFalse(result.Approved);
        Assert.AreEqual("Insufficient funds", result.Reason);
    }
}