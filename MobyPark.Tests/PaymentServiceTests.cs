using MobyPark.Models;
using MobyPark.Models.Access;
using MobyPark.Models.DataService;
using MobyPark.Services;
using Moq;

namespace MobyPark.Tests;

[TestClass]
public sealed class PaymentServiceTests
{
    private Mock<IDataService>? _mockDataService;
    private Mock<IPaymentAccess>? _mockPaymentAccess;

    private IDataService? _dataService;
    private PaymentService? _paymentService;

    [TestInitialize]
    public void TestInitialize()
    {
        _mockDataService = new Mock<IDataService>();
        _mockPaymentAccess = new Mock<IPaymentAccess>();

        _dataService = _mockDataService.Object;
        _paymentService = new(_dataService);

        _mockDataService.Setup(service => service.Payments).Returns(_mockPaymentAccess.Object);

        _mockPaymentAccess.Setup(access => access.Create(It.IsAny<PaymentModel>())).ReturnsAsync(true);
    }

    [TestMethod]
    [DataRow("abc123", 50.0)]
    [DataRow("def456", 0.0)]
    [DataRow("ghi789", -25.5)]
    [DataRow("jkl012", 1000000.99)]
    [DataRow("mno345", 0.0001)]
    public async Task CreatePayment_ValidInput_ReturnsPaymentModel(string transaction, double amountDouble)
    {
        // Use doubles in the DataRows, as decimals are not runtime constants, but better for financial situations like this.

        // Arrange
        decimal amount = (decimal)amountDouble;

        string initiator = "testUser";
        var transactionData = new TransactionDataModel
        {
            Amount = amount,
            Date = DateTime.Now,
            Method = "CreditCard",
            Issuer = "TestBank",
            Bank = "TestBank"
        };

        // Act
        var result = await _paymentService!.CreatePayment(transaction, amount, initiator, transactionData);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(transaction, result.TransactionId);
        Assert.AreEqual(amount, result.Amount);
        Assert.AreEqual(initiator, result.Initiator);
        Assert.AreEqual(transactionData, result.TransactionData);

        _mockPaymentAccess!.Verify(access => access.Create(It.Is<PaymentModel>(p =>
            p.TransactionId == transaction &&
            p.Amount == amount &&
            p.Initiator == initiator
        )), Times.Once);
    }

    [TestMethod]
    [DataRow(null, 50.0, "testUser")]
    [DataRow("", 50.0, "testUser")]
    [DataRow("abc123", 50.0, null)]
    [DataRow("abc123", 50.0, "")]
    public async Task CreatePayment_InvalidInputs_ThrowsException(string transaction, double amountDouble, string initiator)
    {
        decimal amount = (decimal)amountDouble;

        var transactionData = new TransactionDataModel
        {
            Amount = amount,
            Date = DateTime.Now,
            Method = "CreditCard",
            Issuer = "TestBank",
            Bank = "TestBank"
        };

        // Act & Assert
        await Assert.ThrowsExceptionAsync<ArgumentException>(async () =>
        {
            await _paymentService!.CreatePayment(transaction, amount, initiator, transactionData);
        });

        _mockPaymentAccess!.Verify(access => access.Create(It.IsAny<PaymentModel>()), Times.Never);
    }

    [TestMethod]
    [DataRow("orig123", 50.0, "adminUser")]
    [DataRow("orig456", 0.0, "adminUser")]
    [DataRow("orig789", 1000000.5, "adminUser")]
    public async Task RefundPayment_Successful_ReturnsRefundModel(string originalTransaction, double amountDouble, string adminUser)
    {
        decimal amount = (decimal)amountDouble;

        var result = await _paymentService!.RefundPayment(originalTransaction, amount, adminUser);

        Assert.IsNotNull(result);
        Assert.AreEqual(-Math.Abs(amount), result.Amount);
        Assert.AreEqual(adminUser, result.Initiator);
        Assert.AreEqual(originalTransaction, result.CoupledTo);

        _mockPaymentAccess!.Verify(access => access.Create(It.Is<PaymentModel>(p =>
            p.Amount == -Math.Abs(amount) &&
            p.Initiator == adminUser &&
            p.CoupledTo == originalTransaction
        )), Times.Once);
    }

    [TestMethod]
    [DataRow(null, 50.0, "adminUser")]
    [DataRow("orig123", 50.0, null)]
    [DataRow("orig123", -50.0, "adminUser")]
    public async Task RefundPayment_InvalidInputs_ThrowsException(string originalTransaction, double amountDouble, string adminUser)
    {
        decimal amount = (decimal)amountDouble;
        await Assert.ThrowsExceptionAsync<ArgumentException>(async () =>
            await _paymentService!.RefundPayment(originalTransaction, amount, adminUser)
        );

        _mockPaymentAccess!.Verify(access => access.Create(It.IsAny<PaymentModel>()), Times.Never);
    }

    [TestMethod]
    [DataRow("txn_001", "hash_abc")]
    [DataRow("txn_002", "hash_xyz")]
    public async Task ValidatePayment_PaymentExists_HashMatches_UpdatesPayment(string transactionId, string validationHash)
    {
        var transactionData = new TransactionDataModel
        {
            Amount = 100,
            Date = DateTime.UtcNow,
            Method = "CARD",
            Issuer = "BANK",
            Bank = "BANK01"
        };

        var existingPayment = new PaymentModel
        {
            TransactionId = transactionId,
            Hash = SystemService.GenerateGuid(validationHash).ToString("D"),
            Completed = null,
            TransactionData = new TransactionDataModel()
        };

        _mockPaymentAccess!.Setup(p => p.GetByTransactionId(transactionId)).ReturnsAsync(existingPayment);
        _mockPaymentAccess.Setup(p => p.Update(It.IsAny<PaymentModel>())).ReturnsAsync(true).Verifiable();

        var result = await _paymentService!.ValidatePayment(transactionId, validationHash, transactionData);

        Assert.IsNotNull(result);
        Assert.AreEqual(transactionId, result.TransactionId);
        Assert.IsNotNull(result.Completed);
        Assert.AreEqual(transactionData.Amount, result.TransactionData.Amount);
        _mockPaymentAccess.Verify(p => p.Update(It.IsAny<PaymentModel>()), Times.Once);
    }

    [TestMethod]
    [DataRow("txn_003", "correct_hash")]
    [DataRow("txn_004", "some_hash")]
    public async Task ValidatePayment_PaymentExists_HashDoesNotMatch_ThrowsUnauthorizedAccessException(string transactionId, string validationHash)
    {
        var transactionData = new TransactionDataModel
        {
            Amount = 50,
            Date = DateTime.UtcNow,
            Method = "CARD",
            Issuer = "BANK",
            Bank = "BANK02"
        };

        var existingPayment = new PaymentModel
        {
            TransactionId = transactionId,
            Hash = "wrong_hash",
            Completed = null,
            TransactionData = new TransactionDataModel()
        };

        _mockPaymentAccess!.Setup(p => p.GetByTransactionId(transactionId)).ReturnsAsync(existingPayment);

        await Assert.ThrowsExceptionAsync<UnauthorizedAccessException>(async () =>
            await _paymentService!.ValidatePayment(transactionId, validationHash, transactionData)
        );

        _mockPaymentAccess.Verify(p => p.Update(It.IsAny<PaymentModel>()), Times.Never);
    }

    [TestMethod]
    [DataRow("txn_005", "any_hash")]
    [DataRow("txn_006", "another_hash")]
    public async Task ValidatePayment_PaymentDoesNotExist_ThrowsKeyNotFoundException(string transactionId, string validationHash)
    {
        var transactionData = new TransactionDataModel
        {
            Amount = 75,
            Date = DateTime.UtcNow,
            Method = "CARD",
            Issuer = "BANK",
            Bank = "BANK03"
        };

        _mockPaymentAccess!.Setup(p => p.GetByTransactionId(transactionId)).ReturnsAsync((PaymentModel?)null);

        await Assert.ThrowsExceptionAsync<KeyNotFoundException>(async () =>
            await _paymentService!.ValidatePayment(transactionId, validationHash, transactionData)
        );

        _mockPaymentAccess.Verify(p => p.Update(It.IsAny<PaymentModel>()), Times.Never);
    }

    [TestMethod]
    [DataRow("txn_007", "hash_123")]
    [DataRow("txn_008", "hash_456")]
    public async Task ValidatePayment_PaymentAlreadyCompleted_DoesNotUpdateTimestamp(string transactionId, string validationHash)
    {
        var transactionData = new TransactionDataModel
        {
            Amount = 200,
            Date = DateTime.UtcNow,
            Method = "CARD",
            Issuer = "BANK",
            Bank = "BANK04"
        };

        var existingPayment = new PaymentModel
        {
            TransactionId = transactionId,
            Hash = SystemService.GenerateGuid(validationHash).ToString("D"),
            Completed = DateTime.UtcNow.AddHours(-1), // already completed
            TransactionData = new TransactionDataModel()
        };

        _mockPaymentAccess!.Setup(p => p.GetByTransactionId(transactionId)).ReturnsAsync(existingPayment);

        var result = await _paymentService!.ValidatePayment(transactionId, validationHash, transactionData);

        Assert.IsNotNull(result);
        Assert.AreEqual(transactionId, result.TransactionId);
        Assert.AreEqual(existingPayment.Completed, result.Completed); // timestamp unchanged
        Assert.AreEqual(transactionData.Amount, result.TransactionData.Amount); // still updates t_data
        _mockPaymentAccess.Verify(p => p.Update(It.IsAny<PaymentModel>()), Times.Never);
    }

    [TestMethod]
    [DataRow("user1", 2)]
    [DataRow("user2", 0)]
    [DataRow("user3", 1)]
    public async Task GetPaymentsForUser_ReturnsExpectedCount(string username, int expectedCount)
    {
        // Arrange
        var payments = new List<PaymentModel>();
        for (int i = 0; i < expectedCount; i++)
        {
            payments.Add(new PaymentModel
            {
                TransactionId = $"txn_{i + 1}",
                Amount = 100 + i * 10,
                Initiator = username
            });
        }

        _mockPaymentAccess!.Setup(p => p.GetByUser(username)).ReturnsAsync(payments);

        // Act
        var result = await _paymentService!.GetPaymentsForUser(username);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(expectedCount, result.Count);
        Assert.IsTrue(result.All(p => p.Initiator == username));
    }

    [TestMethod]
    [DataRow("txn_001", 150)]
    [DataRow("txn_002", 50)]
    [DataRow("txn_003", 0)]
    public async Task GetTotalAmountForTransaction_PaymentExists_ReturnsAmount(string transactionId, int expectedAmount)
    {
        // Arrange
        decimal amount = expectedAmount;

        var payment = new PaymentModel
        {
            TransactionId = transactionId,
            Amount = amount
        };

        _mockPaymentAccess!.Setup(p => p.GetByTransactionId(transactionId)).ReturnsAsync(payment);

        // Act
        var result = await _paymentService!.GetTotalAmountForTransaction(transactionId);

        // Assert
        Assert.AreEqual(expectedAmount, result);
    }

    [TestMethod]
    [DataRow("txn_missing1")]
    [DataRow("txn_missing2")]
    public async Task GetTotalAmountForTransaction_PaymentDoesNotExist_ReturnsDecimalMinValue(string transactionId)
    {
        // Arrange
        _mockPaymentAccess!.Setup(p => p.GetByTransactionId(transactionId)).ReturnsAsync((PaymentModel?)null);

        // Act
        var result = await _paymentService!.GetTotalAmountForTransaction(transactionId);

        // Assert
        Assert.AreEqual(decimal.MinValue, result);
    }
}
