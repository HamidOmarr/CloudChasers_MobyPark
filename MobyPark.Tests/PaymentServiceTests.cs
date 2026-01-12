using MobyPark.DTOs.Payment.Request;
using MobyPark.DTOs.Transaction.Request;
using MobyPark.Models;
using MobyPark.Models.Repositories.Interfaces;
using MobyPark.Services;
using MobyPark.Services.Interfaces;
using MobyPark.Services.Results.Payment;
using MobyPark.Services.Results.Transaction;

using Moq;

namespace MobyPark.Tests;

[TestClass]
public sealed class PaymentServiceTests
{
    private Mock<IPaymentRepository> _mockPaymentsRepo = null!;
    private Mock<ITransactionService> _mockTransactionService = null!;
    private PaymentService _paymentService = null!;

    private const long RequestingUserId = 1L;
    private const string AdminUsername = "admin@mobypark.com";

    [TestInitialize]
    public void TestInitialize()
    {
        _mockPaymentsRepo = new Mock<IPaymentRepository>();
        _mockTransactionService = new Mock<ITransactionService>();
        _paymentService = new PaymentService(
            _mockPaymentsRepo.Object,
            _mockTransactionService.Object
        );
    }

    #region Create

    [TestMethod]
    [DataRow("ABC-123", 10.50)]
    public async Task CreatePaymentAndTransaction_Success_ReturnsSuccess(string plate, double amount)
    {
        // Arrange
        var decAmount = (decimal)amount;
        var dto = new CreatePaymentDto { LicensePlateNumber = plate, Amount = decAmount };
        var transaction = new TransactionModel { Id = Guid.NewGuid(), Amount = decAmount };
        var newPaymentId = Guid.NewGuid();

        _mockTransactionService.Setup(transactionService => transactionService.CreateTransaction(It.Is<TransactionModel>(
                transactionModel => transactionModel.Amount == decAmount)))
            .ReturnsAsync(new CreateTransactionResult.Success(transaction.Id, transaction));

        _mockPaymentsRepo.Setup(paymentRepo => paymentRepo.CreateWithId(It.Is<PaymentModel>(
                paymentModel => paymentModel.Amount == decAmount && paymentModel.LicensePlateNumber == plate)))
            .ReturnsAsync((true, newPaymentId));

        // Act
        var result = await _paymentService.CreatePaymentAndTransaction(dto);

        // Assert
        Assert.IsInstanceOfType(result, typeof(CreatePaymentResult.Success));
        var successResult = (CreatePaymentResult.Success)result;
        Assert.AreEqual(newPaymentId, successResult.Payment.PaymentId);
        Assert.AreEqual(transaction.Id, successResult.Payment.TransactionId);
        _mockTransactionService.Verify(transactionService => transactionService.DeleteTransaction(It.IsAny<Guid>()), Times.Never);
    }

    [TestMethod]
    [DataRow("ABC-123", 10.50)]
    public async Task CreatePaymentAndTransaction_TransactionFails_ReturnsError(string plate, double amount)
    {
        // Arrange
        var decAmount = (decimal)amount;
        var dto = new CreatePaymentDto { LicensePlateNumber = plate, Amount = decAmount };

        _mockTransactionService.Setup(transactionService => transactionService.CreateTransaction(It.IsAny<TransactionModel>()))
            .ReturnsAsync(new CreateTransactionResult.Error("DB Error"));

        // Act
        var result = await _paymentService.CreatePaymentAndTransaction(dto);

        // Assert
        Assert.IsInstanceOfType(result, typeof(CreatePaymentResult.Error));
        _mockPaymentsRepo.Verify(paymentRepo => paymentRepo.CreateWithId(It.IsAny<PaymentModel>()), Times.Never);
    }

    [TestMethod]
    [DataRow("ABC-123", 10.50)]
    public async Task CreatePaymentAndTransaction_PaymentFails_RollsBackAndReturnsError(string plate, double amount)
    {
        // Arrange
        var decAmount = (decimal)amount;
        var dto = new CreatePaymentDto { LicensePlateNumber = plate, Amount = decAmount };
        var transactionId = Guid.NewGuid();
        var transaction = new TransactionModel { Id = transactionId, Amount = decAmount };

        _mockTransactionService.Setup(transactionService => transactionService.CreateTransaction(It.IsAny<TransactionModel>()))
            .ReturnsAsync(new CreateTransactionResult.Success(transactionId, transaction));

        // Simulate payment creation failing
        _mockPaymentsRepo.Setup(paymentRepo => paymentRepo.CreateWithId(It.IsAny<PaymentModel>()))
            .ReturnsAsync((false, Guid.Empty));

        // Act
        var result = await _paymentService.CreatePaymentAndTransaction(dto);

        // Assert
        Assert.IsInstanceOfType(result, typeof(CreatePaymentResult.Error));
        StringAssert.Contains(((CreatePaymentResult.Error)result).Message, "Failed to create payment record");

        // Verify rollback was called
        _mockTransactionService.Verify(transactionService => transactionService.DeleteTransaction(transactionId), Times.Once);
    }

    [TestMethod]
    [DataRow("ABC-123", 10.50)]
    public async Task CreatePaymentAndTransaction_PaymentThrows_RollsBackAndReturnsError(string plate, double amount)
    {
        // Arrange
        var decAmount = (decimal)amount;
        var dto = new CreatePaymentDto { LicensePlateNumber = plate, Amount = decAmount };
        var transactionId = Guid.NewGuid();
        var transaction = new TransactionModel { Id = transactionId, Amount = decAmount };

        _mockTransactionService.Setup(transactionService => transactionService.CreateTransaction(It.IsAny<TransactionModel>()))
            .ReturnsAsync(new CreateTransactionResult.Success(transactionId, transaction));

        _mockPaymentsRepo.Setup(paymentRepo => paymentRepo.CreateWithId(It.IsAny<PaymentModel>()))
            .ThrowsAsync(new InvalidOperationException("DB Boom"));

        // Act
        var result = await _paymentService.CreatePaymentAndTransaction(dto);

        // Assert
        Assert.IsInstanceOfType(result, typeof(CreatePaymentResult.Error));
        StringAssert.Contains(((CreatePaymentResult.Error)result).Message, "saving the payment");
        _mockTransactionService.Verify(transactionService => transactionService.DeleteTransaction(transactionId), Times.Once);
    }

    #endregion

    #region GetById

    [TestMethod]
    [DataRow("3F2504E0-4F89-11D3-9A0C-0305E82C3301")]
    public async Task GetPaymentById_ValidId_ReturnsSuccess(string pId)
    {
        // Arrange
        var paymentId = Guid.Parse(pId);
        var payment = new PaymentModel { PaymentId = paymentId };
        _mockPaymentsRepo.Setup(paymentRepo => paymentRepo.GetByPaymentId(paymentId, RequestingUserId)).ReturnsAsync(payment);

        // Act
        var result = await _paymentService.GetPaymentById(pId, RequestingUserId);

        // Assert
        Assert.IsInstanceOfType(result, typeof(GetPaymentResult.Success));
        Assert.AreEqual(paymentId, ((GetPaymentResult.Success)result).Payment.PaymentId);
    }

    [TestMethod]
    [DataRow("3F2504E0-4F89-11D3-9A0C-0305E82C3301")]
    public async Task GetPaymentById_NotFound_ReturnsNotFound(string pId)
    {
        // Arrange
        var paymentId = Guid.Parse(pId);
        _mockPaymentsRepo.Setup(paymentRepo => paymentRepo.GetByPaymentId(paymentId, RequestingUserId)).ReturnsAsync((PaymentModel?)null);

        // Act
        var result = await _paymentService.GetPaymentById(pId, RequestingUserId);

        // Assert
        Assert.IsInstanceOfType(result, typeof(GetPaymentResult.NotFound));
    }

    [TestMethod]
    [DataRow("not-a-guid")]
    [DataRow(null)]
    [DataRow(" ")]
    public async Task GetPaymentById_InvalidGuid_ReturnsInvalidInput(string pId)
    {
        // Act
        var result = await _paymentService.GetPaymentById(pId, RequestingUserId);

        // Assert
        Assert.IsInstanceOfType(result, typeof(GetPaymentResult.InvalidInput));
    }

    #endregion

    #region DeletePayment

    [TestMethod]
    [DataRow("3F2504E0-4F89-11D3-9A0C-0305E82C3301")]
    public async Task DeletePayment_ValidId_ReturnsSuccess(string pId)
    {
        // Arrange
        var paymentId = Guid.Parse(pId);
        _mockPaymentsRepo.Setup(paymentRepo => paymentRepo.DeletePayment(paymentId, RequestingUserId)).ReturnsAsync(true);

        // Act
        var result = await _paymentService.DeletePayment(pId, RequestingUserId);

        // Assert
        Assert.IsInstanceOfType(result, typeof(DeletePaymentResult.Success));
    }

    [TestMethod]
    [DataRow("3F2504E0-4F89-11D3-9A0C-0305E82C3301")]
    public async Task DeletePayment_NotFound_ReturnsNotFound(string pId)
    {
        // Arrange
        var paymentId = Guid.Parse(pId);
        _mockPaymentsRepo.Setup(paymentRepo => paymentRepo.DeletePayment(paymentId, RequestingUserId)).ReturnsAsync(false);

        // Act
        var result = await _paymentService.DeletePayment(pId, RequestingUserId);

        // Assert
        Assert.IsInstanceOfType(result, typeof(DeletePaymentResult.NotFound));
    }

    [TestMethod]
    [DataRow("not-a-guid")]
    public async Task DeletePayment_InvalidGuid_ReturnsError(string pId)
    {
        // Act
        var result = await _paymentService.DeletePayment(pId, RequestingUserId);

        // Assert
        Assert.IsInstanceOfType(result, typeof(DeletePaymentResult.Error));
    }

    [TestMethod]
    [DataRow("3F2504E0-4F89-11D3-9A0C-0305E82C3301")]
    public async Task DeletePayment_Throws_ReturnsError(string pId)
    {
        // Arrange
        var paymentId = Guid.Parse(pId);
        _mockPaymentsRepo.Setup(paymentRepo => paymentRepo.DeletePayment(paymentId, RequestingUserId))
            .ThrowsAsync(new InvalidOperationException("DB Boom"));

        // Act
        var result = await _paymentService.DeletePayment(pId, RequestingUserId);

        // Assert
        Assert.IsInstanceOfType(result, typeof(DeletePaymentResult.Error));
        StringAssert.Contains(((DeletePaymentResult.Error)result).Message, "DB Boom");
    }

    #endregion

    #region GetByVariousCriteria

    #region GetByTransactionId

    [TestMethod]
    [DataRow("3F2504E0-4F89-11D3-9A0C-0305E82C3301")]
    public async Task GetPaymentByTransactionId_ValidId_ReturnsSuccess(string tId)
    {
        // Arrange
        var transactionId = Guid.Parse(tId);
        var payment = new PaymentModel { TransactionId = transactionId };
        _mockPaymentsRepo.Setup(paymentRepo => paymentRepo.GetByTransactionId(transactionId, RequestingUserId)).ReturnsAsync(payment);

        // Act
        var result = await _paymentService.GetPaymentByTransactionId(tId, RequestingUserId);

        // Assert
        Assert.IsInstanceOfType(result, typeof(GetPaymentResult.Success));
        Assert.AreEqual(transactionId, ((GetPaymentResult.Success)result).Payment.TransactionId);
    }

    [TestMethod]
    [DataRow("3F2504E0-4F89-11D3-9A0C-0305E82C3301")]
    public async Task GetPaymentByTransactionId_NotFound_ReturnsNotFound(string tId)
    {
        // Arrange
        var transactionId = Guid.Parse(tId);
        _mockPaymentsRepo.Setup(paymentRepo => paymentRepo.GetByTransactionId(transactionId, RequestingUserId)).ReturnsAsync((PaymentModel?)null);

        // Act
        var result = await _paymentService.GetPaymentByTransactionId(tId, RequestingUserId);

        // Assert
        Assert.IsInstanceOfType(result, typeof(GetPaymentResult.NotFound));
    }

    [TestMethod]
    [DataRow("not-a-guid")]
    [DataRow(null)]
    public async Task GetPaymentByTransactionId_InvalidGuid_ReturnsInvalidInput(string tId)
    {
        // Act
        var result = await _paymentService.GetPaymentByTransactionId(tId, RequestingUserId);

        // Assert
        Assert.IsInstanceOfType(result, typeof(GetPaymentResult.InvalidInput));
    }

    #endregion

    #region GetByLicensePlate

    [TestMethod]
    [DataRow("AB-12-CD", 1)]
    [DataRow("WX-99-YZ", 5)]
    public async Task GetPaymentsByLicensePlate_Found_ReturnsSuccessList(string plate, int count)
    {
        // Arrange
        var payments = Enumerable.Range(1, count)
            .Select(_ => new PaymentModel { LicensePlateNumber = plate }).ToList();
        _mockPaymentsRepo.Setup(paymentRepo => paymentRepo.GetByLicensePlate(plate, RequestingUserId)).ReturnsAsync(payments);

        // Act
        var result = await _paymentService.GetPaymentsByLicensePlate(plate, RequestingUserId);

        // Assert
        Assert.IsInstanceOfType(result, typeof(GetPaymentListResult.Success));
        Assert.AreEqual(count, ((GetPaymentListResult.Success)result).Payments.Count);
    }

    [TestMethod]
    [DataRow("NOT-FOUND")]
    public async Task GetPaymentsByLicensePlate_NotFound_ReturnsNotFound(string plate)
    {
        // Arrange
        _mockPaymentsRepo.Setup(paymentRepo => paymentRepo.GetByLicensePlate(plate, RequestingUserId)).ReturnsAsync(new List<PaymentModel>());

        // Act
        var result = await _paymentService.GetPaymentsByLicensePlate(plate, RequestingUserId);

        // Assert
        Assert.IsInstanceOfType(result, typeof(GetPaymentListResult.NotFound));
    }

    #endregion

    #region GetByUserId

    [TestMethod]
    [DataRow(1L, 3)]
    [DataRow(2L, 10)]
    public async Task GetPaymentsByUser_Found_ReturnsSuccessList(long userId, int count)
    {
        // Arrange
        var payments = Enumerable.Range(1, count)
            .Select(_ => new PaymentModel { LicensePlateNumber = "AB-12-CD" }).ToList();
        _mockPaymentsRepo.Setup(paymentRepo => paymentRepo.GetByUserId(userId)).ReturnsAsync(payments);

        // Act
        var result = await _paymentService.GetPaymentsByUser(userId);

        // Assert
        Assert.IsInstanceOfType(result, typeof(GetPaymentListResult.Success));
        Assert.AreEqual(count, ((GetPaymentListResult.Success)result).Payments.Count);
    }

    [TestMethod]
    [DataRow(99L)]
    public async Task GetPaymentsByUser_NotFound_ReturnsNotFound(long userId)
    {
        // Arrange
        _mockPaymentsRepo.Setup(paymentRepo => paymentRepo.GetByUserId(userId)).ReturnsAsync(new List<PaymentModel>());

        // Act
        var result = await _paymentService.GetPaymentsByUser(userId);

        // Assert
        Assert.IsInstanceOfType(result, typeof(GetPaymentListResult.NotFound));
    }

    #endregion

    #region GetAll

    [TestMethod]
    [DataRow(5)]
    public async Task GetAllPayments_Found_ReturnsSuccessList(int count)
    {
        // Arrange
        var payments = Enumerable.Range(1, count)
            .Select(_ => new PaymentModel()).ToList();
        _mockPaymentsRepo.Setup(paymentRepo => paymentRepo.GetAll()).ReturnsAsync(payments);

        // Act
        var result = await _paymentService.GetAllPayments();

        // Assert
        Assert.IsInstanceOfType(result, typeof(GetPaymentListResult.Success));
        Assert.AreEqual(count, ((GetPaymentListResult.Success)result).Payments.Count);
    }

    [TestMethod]
    public async Task GetAllPayments_NotFound_ReturnsNotFound()
    {
        // Arrange
        _mockPaymentsRepo.Setup(paymentRepo => paymentRepo.GetAll()).ReturnsAsync(new List<PaymentModel>());

        // Act
        var result = await _paymentService.GetAllPayments();

        // Assert
        Assert.IsInstanceOfType(result, typeof(GetPaymentListResult.NotFound));
    }

    #endregion

    #endregion

    #region CountPayments

    [TestMethod]
    [DataRow(0)]
    [DataRow(123)]
    public async Task CountPayments_ReturnsCount(int count)
    {
        // Arrange
        _mockPaymentsRepo.Setup(paymentRepo => paymentRepo.Count()).ReturnsAsync(count);

        // Act
        var result = await _paymentService.CountPayments();

        // Assert
        Assert.AreEqual(count, result);
    }

    #endregion

    #region ValidatePayment

    [TestMethod]
    [DataRow("3F2504E0-4F89-11D3-9A0C-0305E82C3301", "2F2504E0-4F89-11D3-9A0C-0305E82C3301")]
    public async Task ValidatePayment_Success_ReturnsSuccess(string pId, string tId)
    {
        // Arrange
        var paymentId = Guid.Parse(pId);
        var transactionId = Guid.Parse(tId);
        var dto = new TransactionDataDto { Method = "iDEAL", Issuer = "ING", Bank = "ING BANK" };

        var transaction = new TransactionModel { Id = transactionId };

        _mockPaymentsRepo.Setup(paymentRepo => paymentRepo.GetByPaymentId(paymentId, RequestingUserId))
            .ReturnsAsync(() => new PaymentModel
            {
                PaymentId = paymentId,
                TransactionId = transactionId,
                CompletedAt = null
            });

        _mockTransactionService.Setup(transactionService => transactionService.GetTransactionById(transactionId))
            .ReturnsAsync(new GetTransactionResult.Success(transaction));

        _mockPaymentsRepo.Setup(paymentRepo => paymentRepo.Update(
            It.Is<PaymentModel>(paymentModel => paymentModel.PaymentId == paymentId),
            It.IsAny<CompletePaymentDto>())).Callback<PaymentModel, object>((paymentToUpdate, dtoObj) =>
        {
            if (dtoObj is CompletePaymentDto completeDto)
                paymentToUpdate.CompletedAt = completeDto.CompletedAt;
        }).ReturnsAsync(true);

        _mockTransactionService.Setup(transactionService => transactionService.UpdateTransaction(transactionId, dto))
            .ReturnsAsync(new UpdateTransactionResult.Success(transaction));

        // Act
        var result = await _paymentService.ValidatePayment(paymentId, dto, RequestingUserId);

        // Assert
        Assert.IsInstanceOfType(result, typeof(ValidatePaymentResult.Success));
        var successResult = (ValidatePaymentResult.Success)result;
        Assert.IsNotNull(successResult.Payment.CompletedAt);
        Assert.AreEqual("iDEAL", successResult.Payment.Transaction.Method);
    }

    [TestMethod]
    [DataRow("3F2504E0-4F89-11D3-9A0C-0305E82C3301")]
    public async Task ValidatePayment_PaymentNotFound_ReturnsNotFound(string pId)
    {
        // Arrange
        var paymentId = Guid.Parse(pId);
        var dto = new TransactionDataDto();
        _mockPaymentsRepo.Setup(paymentRepo => paymentRepo.GetByPaymentId(paymentId, RequestingUserId)).ReturnsAsync((PaymentModel?)null);

        // Act
        var result = await _paymentService.ValidatePayment(paymentId, dto, RequestingUserId);

        // Assert
        Assert.IsInstanceOfType(result, typeof(ValidatePaymentResult.NotFound));
    }

    [TestMethod]
    [DataRow("3F2504E0-4F89-11D3-9A0C-0305E82C3301")]
    public async Task ValidatePayment_AlreadyCompleted_ReturnsInvalidData(string pId)
    {
        // Arrange
        var paymentId = Guid.Parse(pId);
        var dto = new TransactionDataDto();
        var payment = new PaymentModel { PaymentId = paymentId, CompletedAt = DateTime.UtcNow }; // Already completed

        _mockPaymentsRepo.Setup(paymentRepo => paymentRepo.GetByPaymentId(paymentId, RequestingUserId)).ReturnsAsync(payment);

        // Act
        var result = await _paymentService.ValidatePayment(paymentId, dto, RequestingUserId);

        // Assert
        Assert.IsInstanceOfType(result, typeof(ValidatePaymentResult.InvalidData));
        StringAssert.Contains(((ValidatePaymentResult.InvalidData)result).Message, "already been completed");
    }

    [TestMethod]
    [DataRow("3F2504E0-4F89-11D3-9A0C-0305E82C3301", "2F2504E0-4F89-11D3-9A0C-0305E82C3301")]
    public async Task ValidatePayment_TransactionNotFound_ReturnsInvalidData(string pId, string tId)
    {
        // Arrange
        var paymentId = Guid.Parse(pId);
        var transactionId = Guid.Parse(tId);
        var dto = new TransactionDataDto();
        var payment = new PaymentModel { PaymentId = paymentId, TransactionId = transactionId, CompletedAt = null };

        _mockPaymentsRepo.Setup(paymentRepo => paymentRepo.GetByPaymentId(paymentId, RequestingUserId)).ReturnsAsync(payment);
        _mockTransactionService.Setup(transactionService => transactionService.GetTransactionById(transactionId))
            .ReturnsAsync(new GetTransactionResult.NotFound()); // Transaction not found

        // Act
        var result = await _paymentService.ValidatePayment(paymentId, dto, RequestingUserId);

        // Assert
        Assert.IsInstanceOfType(result, typeof(ValidatePaymentResult.InvalidData));
        StringAssert.Contains(((ValidatePaymentResult.InvalidData)result).Message, "Transaction not found");
    }

    [TestMethod]
    [DataRow("3F2504E0-4F89-11D3-9A0C-0305E82C3301", "2F2504E0-4F89-11D3-9A0C-0305E82C3301")]
    public async Task ValidatePayment_TransactionUpdateFails_ReturnsError(string pId, string tId)
    {
        // Arrange
        var paymentId = Guid.Parse(pId);
        var transactionId = Guid.Parse(tId);
        var dto = new TransactionDataDto { Method = "iDEAL" };

        var transaction = new TransactionModel { Id = transactionId };

        _mockPaymentsRepo.Setup(paymentRepo => paymentRepo.GetByPaymentId(paymentId, RequestingUserId))
            .ReturnsAsync(() => new PaymentModel
            {
                PaymentId = paymentId,
                TransactionId = transactionId,
                CompletedAt = null
            });

        _mockTransactionService.Setup(transactionService => transactionService.GetTransactionById(transactionId))
            .ReturnsAsync(new GetTransactionResult.Success(transaction));

        _mockPaymentsRepo.Setup(paymentRepo => paymentRepo.Update(It.IsAny<PaymentModel>(), It.IsAny<CompletePaymentDto>()))
            .ReturnsAsync(true);

        _mockTransactionService.Setup(transactionService => transactionService.UpdateTransaction(transactionId, dto))
            .ReturnsAsync(new UpdateTransactionResult.Error("DB Error"));

        // Act
        var result = await _paymentService.ValidatePayment(paymentId, dto, RequestingUserId);

        // Assert
        Assert.IsInstanceOfType(result, typeof(ValidatePaymentResult.Error));
        StringAssert.Contains(((ValidatePaymentResult.Error)result).Message, "DB Error");
    }

    #endregion

    #region RefundPayment

    [TestMethod]
    [DataRow("3F2504E0-4F89-11D3-9A0C-0305E82C3301", 10.0, 5.0)]
    public async Task RefundPayment_Success_ReturnsSuccess(string pId, double originalAmount, double refundAmount)
    {
        // Arrange
        var paymentId = Guid.Parse(pId);
        var decOriginal = (decimal)originalAmount;
        var decRefund = (decimal)refundAmount;

        var originalPayment = new PaymentModel { PaymentId = paymentId, Amount = decOriginal, LicensePlateNumber = "AB-12-CD" };
        var refundTransaction = new TransactionModel { Id = Guid.NewGuid(), Amount = -decRefund };
        var newRefundPaymentId = Guid.NewGuid();

        _mockPaymentsRepo.Setup(paymentRepo => paymentRepo.GetByPaymentIdAdmin(paymentId)).ReturnsAsync(originalPayment);
        _mockTransactionService.Setup(transactionService => transactionService.CreateTransaction(It.Is<TransactionModel>(tx => tx.Amount == -decRefund)))
            .ReturnsAsync(new CreateTransactionResult.Success(refundTransaction.Id, refundTransaction));
        _mockPaymentsRepo.Setup(paymentRepo => paymentRepo.CreateWithId(It.Is<PaymentModel>(paymentModel => paymentModel.Amount == -decRefund)))
            .ReturnsAsync((true, newRefundPaymentId));

        // Act
        var result = await _paymentService.RefundPayment(pId, decRefund, AdminUsername);

        // Assert
        Assert.IsInstanceOfType(result, typeof(RefundPaymentResult.Success));
        var successResult = (RefundPaymentResult.Success)result;
        Assert.AreEqual(newRefundPaymentId, successResult.RefundPayment.PaymentId);
        Assert.AreEqual(-decRefund, successResult.RefundPayment.Amount);
        Assert.AreEqual("AB-12-CD", successResult.RefundPayment.LicensePlateNumber);
    }

    [TestMethod]
    [DataRow("not-a-guid", 5.0)]
    public async Task RefundPayment_InvalidGuid_ReturnsInvalidInput(string pId, double refundAmount)
    {
        // Act
        var result = await _paymentService.RefundPayment(pId, (decimal)refundAmount, AdminUsername);

        // Assert
        Assert.IsInstanceOfType(result, typeof(RefundPaymentResult.InvalidInput));
    }

    [TestMethod]
    [DataRow("3F2504E0-4F89-11D3-9A0C-0305E82C3301", 5.0)]
    public async Task RefundPayment_PaymentNotFound_ReturnsNotFound(string pId, double refundAmount)
    {
        // Arrange
        var paymentId = Guid.Parse(pId);
        _mockPaymentsRepo.Setup(paymentRepo => paymentRepo.GetByPaymentIdAdmin(paymentId)).ReturnsAsync((PaymentModel?)null);

        // Act
        var result = await _paymentService.RefundPayment(pId, (decimal)refundAmount, AdminUsername);

        // Assert
        Assert.IsInstanceOfType(result, typeof(RefundPaymentResult.NotFound));
    }

    [TestMethod]
    [DataRow("3F2504E0-4F89-11D3-9A0C-0305E82C3301", 10.0, 0.0)]  // Zero amount
    [DataRow("3F2504E0-4F89-11D3-9A0C-0305E82C3301", 10.0, -5.0)] // Negative amount
    [DataRow("3F2504E0-4F89-11D3-9A0C-0305E82C3301", 10.0, 10.01)]// More than original
    public async Task RefundPayment_InvalidAmount_ReturnsInvalidInput(string pId, double originalAmount, double refundAmount)
    {
        // Arrange
        var paymentId = Guid.Parse(pId);
        var originalPayment = new PaymentModel { PaymentId = paymentId, Amount = (decimal)originalAmount };
        _mockPaymentsRepo.Setup(paymentRepo => paymentRepo.GetByPaymentIdAdmin(paymentId)).ReturnsAsync(originalPayment);

        // Act
        var result = await _paymentService.RefundPayment(pId, (decimal)refundAmount, AdminUsername);

        // Assert
        Assert.IsInstanceOfType(result, typeof(RefundPaymentResult.InvalidInput));
    }

    [TestMethod]
    [DataRow("3F2504E0-4F89-11D3-9A0C-0305E82C3301", 10.0, 5.0)]
    public async Task RefundPayment_TransactionFails_ReturnsError(string pId, double originalAmount, double refundAmount)
    {
        // Arrange
        var paymentId = Guid.Parse(pId);
        var originalPayment = new PaymentModel { PaymentId = paymentId, Amount = (decimal)originalAmount };

        _mockPaymentsRepo.Setup(paymentRepo => paymentRepo.GetByPaymentIdAdmin(paymentId)).ReturnsAsync(originalPayment);
        _mockTransactionService.Setup(transactionService => transactionService.CreateTransaction(It.IsAny<TransactionModel>()))
            .ReturnsAsync(new CreateTransactionResult.Error("DB Error"));

        // Act
        var result = await _paymentService.RefundPayment(pId, (decimal)refundAmount, AdminUsername);

        // Assert
        Assert.IsInstanceOfType(result, typeof(RefundPaymentResult.Error));
        _mockPaymentsRepo.Verify(paymentRepo => paymentRepo.CreateWithId(It.IsAny<PaymentModel>()), Times.Never);
    }

    #endregion

    #region GetTotalAmountForPayment

    [TestMethod]
    [DataRow("3F2504E0-4F89-11D3-9A0C-0305E82C3301", 12.34)]
    public async Task GetTotalAmountForPayment_Success_ReturnsAmount(string pId, double amount)
    {
        // Arrange
        var paymentId = Guid.Parse(pId);
        var decAmount = (decimal)amount;
        var payment = new PaymentModel { PaymentId = paymentId, Amount = decAmount };
        _mockPaymentsRepo.Setup(paymentRepo => paymentRepo.GetByPaymentId(paymentId, RequestingUserId)).ReturnsAsync(payment);

        // Act
        var result = await _paymentService.GetTotalAmountForPayment(pId, RequestingUserId);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(decAmount, result.Value);
    }

    [TestMethod]
    [DataRow("3F2504E0-4F89-11D3-9A0C-0305E82C3301")]
    public async Task GetTotalAmountForPayment_NotFound_ReturnsNull(string pId)
    {
        // Arrange
        var paymentId = Guid.Parse(pId);
        _mockPaymentsRepo.Setup(paymentRepo => paymentRepo.GetByPaymentId(paymentId, RequestingUserId)).ReturnsAsync((PaymentModel?)null);

        // Act
        var result = await _paymentService.GetTotalAmountForPayment(pId, RequestingUserId);

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    [DataRow("not-a-guid")]
    public async Task GetTotalAmountForPayment_InvalidId_ReturnsNull(string pId)
    {
        // Act
        var result = await _paymentService.GetTotalAmountForPayment(pId, RequestingUserId);

        // Assert
        Assert.IsNull(result);
    }

    #endregion
}