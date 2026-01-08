using System.Linq.Expressions;

using MobyPark.DTOs.Transaction.Request;
using MobyPark.Models;
using MobyPark.Models.Repositories.Interfaces;
using MobyPark.Services;
using MobyPark.Services.Results.Transaction;

using Moq;

namespace MobyPark.Tests;

[TestClass]
public sealed class TransactionServiceTests
{
    private Mock<ITransactionRepository> _mockTransactionsRepo = null!;
    private TransactionService _transactionService = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _mockTransactionsRepo = new Mock<ITransactionRepository>();
        _transactionService = new TransactionService(_mockTransactionsRepo.Object);
    }

    #region Create

    [TestMethod]
    [DataRow(10.50)]
    [DataRow(0.01)]
    [DataRow(-5.00)]
    public async Task CreateTransaction_Success_ReturnsSuccess(double amount)
    {
        // Arrange
        var decAmount = (decimal)amount;
        var transactionToCreate = new TransactionModel { Amount = decAmount };
        var newTransactionId = Guid.NewGuid();

        _mockTransactionsRepo.Setup(transactionRepo => transactionRepo.CreateWithId(transactionToCreate)).ReturnsAsync((true, newTransactionId));

        // Act
        var result = await _transactionService.CreateTransaction(transactionToCreate);

        // Assert
        Assert.IsInstanceOfType(result, typeof(CreateTransactionResult.Success));
        var successResult = (CreateTransactionResult.Success)result;
        Assert.AreEqual(newTransactionId, successResult.Id);
        Assert.AreEqual(newTransactionId, successResult.Transaction.Id);
    }

    [TestMethod]
    [DataRow(10.50)]
    public async Task CreateTransaction_DbInsertionFails_ReturnsError(double amount)
    {
        // Arrange
        var decAmount = (decimal)amount;
        var transactionToCreate = new TransactionModel { Amount = decAmount };

        _mockTransactionsRepo.Setup(transactionRepo => transactionRepo.CreateWithId(transactionToCreate)).ReturnsAsync((false, Guid.Empty));

        // Act
        var result = await _transactionService.CreateTransaction(transactionToCreate);

        // Assert
        Assert.IsInstanceOfType(result, typeof(CreateTransactionResult.Error));
        StringAssert.Contains(((CreateTransactionResult.Error)result).Message, "Database insertion failed");
    }

    [TestMethod]
    [DataRow(10.50)]
    public async Task CreateTransaction_RepositoryThrows_ReturnsError(double amount)
    {
        // Arrange
        var decAmount = (decimal)amount;
        var transactionToCreate = new TransactionModel { Amount = decAmount };

        _mockTransactionsRepo.Setup(transactionRepo => transactionRepo.CreateWithId(transactionToCreate)).ThrowsAsync(new InvalidOperationException("DB Boom"));

        // Act
        var result = await _transactionService.CreateTransaction(transactionToCreate);

        // Assert
        Assert.IsInstanceOfType(result, typeof(CreateTransactionResult.Error));
        StringAssert.Contains(((CreateTransactionResult.Error)result).Message, "An error occurred");
    }

    #endregion

    #region GetById

    [TestMethod]
    [DataRow("3F2504E0-4F89-11D3-9A0C-0305E82C3301")]
    [DataRow("E6F7C8D9-B3A2-4E1D-8F9C-0A1B2C3D4E5F")]
    public async Task GetTransactionById_Found_ReturnsSuccess(string idString)
    {
        // Arrange
        var transactionId = Guid.Parse(idString);
        var transaction = new TransactionModel { Id = transactionId, Amount = 10 };
        _mockTransactionsRepo.Setup(transactionRepo => transactionRepo.GetByTransactionId(transactionId)).ReturnsAsync(transaction);

        // Act
        var result = await _transactionService.GetTransactionById(transactionId);

        // Assert
        Assert.IsInstanceOfType(result, typeof(GetTransactionResult.Success));
        Assert.AreEqual(transactionId, ((GetTransactionResult.Success)result).Transaction.Id);
    }

    [TestMethod]
    [DataRow("11111111-1111-1111-1111-111111111111")]
    public async Task GetTransactionById_NotFound_ReturnsNotFound(string idString)
    {
        // Arrange
        var transactionId = Guid.Parse(idString);
        _mockTransactionsRepo.Setup(transactionRepo => transactionRepo.GetByTransactionId(transactionId)).ReturnsAsync((TransactionModel?)null);

        // Act
        var result = await _transactionService.GetTransactionById(transactionId);

        // Assert
        Assert.IsInstanceOfType(result, typeof(GetTransactionResult.NotFound));
    }

    #endregion

    #region GetTransactionByPaymentId

    [TestMethod]
    [DataRow("3F2504E0-4F89-11D3-9A0C-0305E82C3301", "2F2504E0-4F89-11D3-9A0C-0305E82C3301")]
    public async Task GetTransactionByPaymentId_Found_ReturnsSuccess(string pIdString, string tIdString)
    {
        // Arrange
        var paymentId = Guid.Parse(pIdString);
        var transactionId = Guid.Parse(tIdString);
        var transaction = new TransactionModel { Id = transactionId, Amount = 10 };
        _mockTransactionsRepo.Setup(transactionRepo => transactionRepo.GetByPaymentId(paymentId)).ReturnsAsync(transaction);

        // Act
        var result = await _transactionService.GetTransactionByPaymentId(paymentId);

        // Assert
        Assert.IsInstanceOfType(result, typeof(GetTransactionResult.Success));
        Assert.AreEqual(transactionId, ((GetTransactionResult.Success)result).Transaction.Id);
    }

    [TestMethod]
    [DataRow("11111111-1111-1111-1111-111111111111")]
    public async Task GetTransactionByPaymentId_NotFound_ReturnsNotFound(string pIdString)
    {
        // Arrange
        var paymentId = Guid.Parse(pIdString);
        _mockTransactionsRepo.Setup(transactionRepo => transactionRepo.GetByPaymentId(paymentId)).ReturnsAsync((TransactionModel?)null);

        // Act
        var result = await _transactionService.GetTransactionByPaymentId(paymentId);

        // Assert
        Assert.IsInstanceOfType(result, typeof(GetTransactionResult.NotFound));
    }

    #endregion

    #region GetAll

    [TestMethod]
    public async Task GetAllTransactions_Found_ReturnsSuccessList()
    {
        // Arrange
        var transactions = new List<TransactionModel>
        {
            new() { Id = Guid.NewGuid(), Amount = 10 },
            new() { Id = Guid.NewGuid(), Amount = 20 }
        };
        _mockTransactionsRepo.Setup(transactionRepo => transactionRepo.GetAll()).ReturnsAsync(transactions);

        // Act
        var result = await _transactionService.GetAllTransactions();

        // Assert
        Assert.IsInstanceOfType(result, typeof(GetTransactionListResult.Success));
        Assert.AreEqual(2, ((GetTransactionListResult.Success)result).Transactions.Count);
    }

    [TestMethod]
    public async Task GetAllTransactions_NotFound_ReturnsNotFound()
    {
        // Arrange
        _mockTransactionsRepo.Setup(transactionRepo => transactionRepo.GetAll()).ReturnsAsync(new List<TransactionModel>());

        // Act
        var result = await _transactionService.GetAllTransactions();

        // Assert
        Assert.IsInstanceOfType(result, typeof(GetTransactionListResult.NotFound));
    }

    #endregion

    #region Exists

    [TestMethod]
    [DataRow("3F2504E0-4F89-11D3-9A0C-0305E82C3301")]
    public async Task TransactionExists_WhenExists_ReturnsExistsResult(string idString)
    {
        // Arrange
        _mockTransactionsRepo.Setup(transactionRepo => transactionRepo.Exists
            (It.IsAny<Expression<Func<TransactionModel, bool>>>())).ReturnsAsync(true);

        // Act
        var result = await _transactionService.TransactionExists("id", idString);

        // Assert
        Assert.IsInstanceOfType(result, typeof(TransactionExistsResult.Exists));
        _mockTransactionsRepo.Verify(transactionRepo => transactionRepo.Exists(
            It.IsAny<Expression<Func<TransactionModel, bool>>>()), Times.Once);
    }

    [TestMethod]
    [DataRow("11111111-1111-1111-1111-111111111111")]
    public async Task TransactionExists_WhenNotExists_ReturnsNotExistsResult(string idString)
    {
        // Arrange
        _mockTransactionsRepo.Setup(transactionRepo => transactionRepo.Exists(
            It.IsAny<Expression<Func<TransactionModel, bool>>>())).ReturnsAsync(false);

        // Act
        var result = await _transactionService.TransactionExists("id", idString);

        // Assert
        Assert.IsInstanceOfType(result, typeof(TransactionExistsResult.NotExists));
        _mockTransactionsRepo.Verify(transactionRepo => transactionRepo.Exists(
            It.IsAny<Expression<Func<TransactionModel, bool>>>()), Times.Once);
    }

    [TestMethod]
    [DataRow("id", "not-a-guid")]
    [DataRow("id", " ")]
    [DataRow("id", null)]
    [DataRow("invalidCheck", "3F2504E0-4F89-11D3-9A0C-0305E82C3301")]
    public async Task TransactionExists_InvalidInput_ReturnsInvalidInput(string checkBy, string value)
    {
        // Act
        var result = await _transactionService.TransactionExists(checkBy, value);

        // Assert
        Assert.IsInstanceOfType(result, typeof(TransactionExistsResult.InvalidInput));
        _mockTransactionsRepo.Verify(transactionRepo => transactionRepo.Exists(
            It.IsAny<Expression<Func<TransactionModel, bool>>>()), Times.Never);
    }

    #endregion

    #region Count

    [TestMethod]
    [DataRow(0)]
    [DataRow(15)]
    public async Task CountTransactions_ReturnsCount(int count)
    {
        // Arrange
        _mockTransactionsRepo.Setup(transactionRepo => transactionRepo.Count()).ReturnsAsync(count);

        // Act
        var result = await _transactionService.CountTransactions();

        // Assert
        Assert.AreEqual(count, result);
    }

    #endregion

    #region Update

    [TestMethod]
    [DataRow("3F2504E0-4F89-11D3-9A0C-0305E82C3301", "iDEAL", "ING", "ING Bank", "CreditCard", "Visa", "ABN")]
    public async Task UpdateTransaction_ValidChange_ReturnsSuccess(string idString, string newMethod, string newIssuer, string newBank, string oldMethod, string oldIssuer, string oldBank)
    {
        // Arrange
        var transactionId = Guid.Parse(idString);
        var dto = new TransactionDataDto { Method = newMethod, Issuer = newIssuer, Bank = newBank };

        _mockTransactionsRepo.Setup(transactionRepo => transactionRepo.GetByTransactionId(transactionId))
            .ReturnsAsync(() => new TransactionModel { Id = transactionId, Method = oldMethod, Issuer = oldIssuer, Bank = oldBank });

        _mockTransactionsRepo.Setup(transactionRepo => transactionRepo.Update(
            It.Is<TransactionModel>(transaction => transaction.Id == transactionId), dto))
            .Callback<TransactionModel, object>((transaction, resultDto) =>
            {
                if (resultDto is not TransactionDataDto updateDto) return;
                transaction.Method = updateDto.Method;
                transaction.Issuer = updateDto.Issuer;
                transaction.Bank = updateDto.Bank;
            }).ReturnsAsync(true);

        // Act
        var result = await _transactionService.UpdateTransaction(transactionId, dto);

        // Assert
        Assert.IsInstanceOfType(result, typeof(UpdateTransactionResult.Success));
        var successResult = (UpdateTransactionResult.Success)result;
        Assert.AreEqual(newMethod, successResult.Transaction.Method);
        Assert.AreEqual(newIssuer, successResult.Transaction.Issuer);
        Assert.AreEqual(newBank, successResult.Transaction.Bank);
    }

    [TestMethod]
    [DataRow("3F2504E0-4F89-11D3-9A0C-0305E82C3301", "iDEAL", "ING", "ING Bank")]
    public async Task UpdateTransaction_NoChanges_ReturnsNoChangesMade(string idString, string method, string issuer, string bank)
    {
        // Arrange
        var transactionId = Guid.Parse(idString);
        var dto = new TransactionDataDto { Method = method, Issuer = issuer, Bank = bank };
        var existingTransaction = new TransactionModel { Id = transactionId, Method = method, Issuer = issuer, Bank = bank };

        _mockTransactionsRepo.Setup(transactionRepo => transactionRepo.GetByTransactionId(transactionId)).ReturnsAsync(existingTransaction);

        // Act
        var result = await _transactionService.UpdateTransaction(transactionId, dto);

        // Assert
        Assert.IsInstanceOfType(result, typeof(UpdateTransactionResult.NoChangesMade));
        _mockTransactionsRepo.Verify(transactionRepo => transactionRepo.Update(It.IsAny<TransactionModel>(), It.IsAny<TransactionDataDto>()), Times.Never);
    }

    [TestMethod]
    [DataRow("11111111-1111-1111-1111-111111111111")]
    public async Task UpdateTransaction_NotFound_ReturnsNotFound(string idString)
    {
        // Arrange
        var transactionId = Guid.Parse(idString);
        var dto = new TransactionDataDto();
        _mockTransactionsRepo.Setup(transactionRepo => transactionRepo.GetByTransactionId(transactionId)).ReturnsAsync((TransactionModel?)null);

        // Act
        var result = await _transactionService.UpdateTransaction(transactionId, dto);

        // Assert
        Assert.IsInstanceOfType(result, typeof(UpdateTransactionResult.NotFound));
    }

    [TestMethod]
    [DataRow("3F2504E0-4F89-11D3-9A0C-0305E82C3301", "NewMethod")]
    public async Task UpdateTransaction_DbUpdateFails_ReturnsError(string idString, string newMethod)
    {
        // Arrange
        var transactionId = Guid.Parse(idString);
        var dto = new TransactionDataDto { Method = newMethod };
        var existingTransaction = new TransactionModel { Id = transactionId, Method = "OldMethod" };

        _mockTransactionsRepo.Setup(transactionRepo => transactionRepo.GetByTransactionId(transactionId)).ReturnsAsync(existingTransaction);
        _mockTransactionsRepo.Setup(transactionRepo => transactionRepo.Update(It.IsAny<TransactionModel>(), dto)).ReturnsAsync(false);

        // Act
        var result = await _transactionService.UpdateTransaction(transactionId, dto);

        // Assert
        Assert.IsInstanceOfType(result, typeof(UpdateTransactionResult.Error));
        StringAssert.Contains(((UpdateTransactionResult.Error)result).Message, "Database update failed");
    }

    [TestMethod]
    [DataRow("3F2504E0-4F89-11D3-9A0C-0305E82C3301", "NewMethod")]
    public async Task UpdateTransaction_RepositoryThrows_ReturnsError(string idString, string newMethod)
    {
        // Arrange
        var transactionId = Guid.Parse(idString);
        var dto = new TransactionDataDto { Method = newMethod };
        var existingTransaction = new TransactionModel { Id = transactionId, Method = "OldMethod" };

        _mockTransactionsRepo.Setup(transactionRepo => transactionRepo.GetByTransactionId(transactionId)).ReturnsAsync(existingTransaction);
        _mockTransactionsRepo.Setup(transactionRepo => transactionRepo.Update(It.IsAny<TransactionModel>(), dto)).ThrowsAsync(new InvalidOperationException("DB Boom"));

        // Act
        var result = await _transactionService.UpdateTransaction(transactionId, dto);

        // Assert
        Assert.IsInstanceOfType(result, typeof(UpdateTransactionResult.Error));
        StringAssert.Contains(((UpdateTransactionResult.Error)result).Message, "DB Boom");
    }

    #endregion

    #region Delete

    [TestMethod]
    [DataRow("3F2504E0-4F89-11D3-9A0C-0305E82C3301")]
    public async Task DeleteTransaction_Success_ReturnsSuccess(string idString)
    {
        // Arrange
        var transactionId = Guid.Parse(idString);
        var transaction = new TransactionModel { Id = transactionId };
        _mockTransactionsRepo.Setup(transactionRepo => transactionRepo.GetByTransactionId(transactionId)).ReturnsAsync(transaction);
        _mockTransactionsRepo.Setup(transactionRepo => transactionRepo.DeleteTransaction(transaction)).ReturnsAsync(true);

        // Act
        var result = await _transactionService.DeleteTransaction(transactionId);

        // Assert
        Assert.IsInstanceOfType(result, typeof(DeleteTransactionResult.Success));
        _mockTransactionsRepo.Verify(transactionRepo => transactionRepo.DeleteTransaction(transaction), Times.Once);
    }

    [TestMethod]
    [DataRow("11111111-1111-1111-1111-111111111111")]
    public async Task DeleteTransaction_NotFound_ReturnsNotFound(string idString)
    {
        // Arrange
        var transactionId = Guid.Parse(idString);
        _mockTransactionsRepo.Setup(transactionRepo => transactionRepo.GetByTransactionId(transactionId)).ReturnsAsync((TransactionModel?)null);

        // Act
        var result = await _transactionService.DeleteTransaction(transactionId);

        // Assert
        Assert.IsInstanceOfType(result, typeof(DeleteTransactionResult.NotFound));
        _mockTransactionsRepo.Verify(transactionRepo => transactionRepo.DeleteTransaction(It.IsAny<TransactionModel>()), Times.Never);
    }

    [TestMethod]
    [DataRow("3F2504E0-4F89-11D3-9A0C-0305E82C3301")]
    public async Task DeleteTransaction_DbDeleteFails_ReturnsError(string idString)
    {
        // Arrange
        var transactionId = Guid.Parse(idString);
        var transaction = new TransactionModel { Id = transactionId };
        _mockTransactionsRepo.Setup(transactionRepo => transactionRepo.GetByTransactionId(transactionId)).ReturnsAsync(transaction);
        _mockTransactionsRepo.Setup(transactionRepo => transactionRepo.DeleteTransaction(transaction)).ReturnsAsync(false);

        // Act
        var result = await _transactionService.DeleteTransaction(transactionId);

        // Assert
        Assert.IsInstanceOfType(result, typeof(DeleteTransactionResult.Error));
        StringAssert.Contains(((DeleteTransactionResult.Error)result).Message, "Failed to delete transaction");
    }

    [TestMethod]
    [DataRow("3F2504E0-4F89-11D3-9A0C-0305E82C3301")]
    public async Task DeleteTransaction_RepositoryThrows_ReturnsError(string idString)
    {
        // Arrange
        var transactionId = Guid.Parse(idString);
        var transaction = new TransactionModel { Id = transactionId };
        _mockTransactionsRepo.Setup(transactionRepo => transactionRepo.GetByTransactionId(transactionId)).ReturnsAsync(transaction);
        _mockTransactionsRepo.Setup(transactionRepo => transactionRepo.DeleteTransaction(transaction)).ThrowsAsync(new InvalidOperationException("DB Boom"));

        // Act
        var result = await _transactionService.DeleteTransaction(transactionId);

        // Assert
        Assert.IsInstanceOfType(result, typeof(DeleteTransactionResult.Error));
        StringAssert.Contains(((DeleteTransactionResult.Error)result).Message, "DB Boom");
    }

    #endregion
}