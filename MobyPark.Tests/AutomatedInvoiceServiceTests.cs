using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using MobyPark.DTOs.Invoice;
using MobyPark.Models;
using MobyPark.Models.Repositories.Interfaces;
using MobyPark.Services;
using MobyPark.Services.Results.Invoice;
using System.Runtime.CompilerServices;

namespace MobyPark.Tests;

[TestClass]
public sealed class AutomatedInvoiceServiceTests
{
    #region Setup

    private Mock<IInvoiceRepository> _mockInvoiceRepository = null!;
    private AutomatedInvoiceService _invoiceService = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _mockInvoiceRepository = new Mock<IInvoiceRepository>();
        _invoiceService = new AutomatedInvoiceService(_mockInvoiceRepository.Object);
    }

    #endregion

    #region CreateInvoice Tests

    [TestMethod]
    [DataRow("AB-12-CD", 1, 10.5)]
    [DataRow("WX-99-YZ", 5, 25.75)]
    public async Task CreateInvoice_NewInvoice_ReturnsSuccess(string licensePlate, long sessionId, double cost)
    {
        // Arrange
        var createDto = new CreateInvoiceDto
        {
            LicensePlateId = licensePlate,
            ParkingSessionId = sessionId,
            Started = DateTime.UtcNow.AddHours(-2),
            Stopped = DateTime.UtcNow,
            Cost = (decimal)cost,
        };

        _mockInvoiceRepository
            .Setup(r => r.GetInvoiceModelByLicensePlate(licensePlate))
            .ReturnsAsync((InvoiceModel?)null);

        _mockInvoiceRepository
            .Setup(r => r.Create(It.IsAny<InvoiceModel>()))
            .ReturnsAsync(true);

        _mockInvoiceRepository
            .Setup(r => r.SaveChangesAsync())
            .ReturnsAsync(1);

        // Act
        var result = await _invoiceService.CreateInvoice(createDto);

        // Assert
        Assert.IsInstanceOfType(result, typeof(CreateInvoiceResult.Success));
        var success = (CreateInvoiceResult.Success)result;
        Assert.AreEqual(licensePlate, success.Invoice.LicensePlateId);
        Assert.AreEqual(sessionId, success.Invoice.ParkingSessionId);
        Assert.AreEqual((decimal)cost, success.Invoice.Cost);

        _mockInvoiceRepository.Verify(
            r => r.GetInvoiceModelByLicensePlate(licensePlate),
            Times.Once);
        _mockInvoiceRepository.Verify(
            r => r.Create(It.IsAny<InvoiceModel>()),
            Times.Once);
        _mockInvoiceRepository.Verify(
            r => r.SaveChangesAsync(),
            Times.Once);
    }

    [TestMethod]
    [DataRow("AB-12-CD")]
    [DataRow("WX-99-YZ")]
    public async Task CreateInvoice_InvoiceAlreadyExists_ReturnsAlreadyExists(string licensePlate)
    {
        // Arrange
        var existingInvoice = new InvoiceModel
        {
            Id = 1,
            LicensePlateId = licensePlate,
            ParkingSessionId = 1,
            Started = DateTime.UtcNow.AddHours(-3),
            Stopped = DateTime.UtcNow,
            Cost = 10m
        };

        var createDto = new CreateInvoiceDto
        {
            LicensePlateId = licensePlate,
            ParkingSessionId = 2,
            Started = DateTime.UtcNow.AddHours(-2),
            Stopped = DateTime.UtcNow,
            Cost = 20m
        };

        _mockInvoiceRepository
            .Setup(r => r.GetInvoiceModelByLicensePlate(licensePlate))
            .ReturnsAsync(existingInvoice);

        // Act
        var result = await _invoiceService.CreateInvoice(createDto);

        // Assert
        Assert.IsInstanceOfType(result, typeof(CreateInvoiceResult.AlreadyExists));
        _mockInvoiceRepository.Verify(
            r => r.GetInvoiceModelByLicensePlate(licensePlate),
            Times.Once);
        _mockInvoiceRepository.Verify(
            r => r.Create(It.IsAny<InvoiceModel>()),
            Times.Never);
        _mockInvoiceRepository.Verify(
            r => r.SaveChangesAsync(),
            Times.Never);
    }

    [TestMethod]
    [DataRow("AB-12-CD")]
    [DataRow("WX-99-YZ")]
    public async Task CreateInvoice_RepositoryThrowsException_ReturnsError(string licensePlate)
    {
        // Arrange
        var createDto = new CreateInvoiceDto
        {
            LicensePlateId = licensePlate,
            ParkingSessionId = 1,
            Started = DateTime.UtcNow.AddHours(-2),
            Stopped = DateTime.UtcNow,
            Cost = 10m
        };

        _mockInvoiceRepository
            .Setup(r => r.GetInvoiceModelByLicensePlate(licensePlate))
            .ThrowsAsync(new InvalidOperationException("Database connection failed"));

        // Act
        var result = await _invoiceService.CreateInvoice(createDto);

        // Assert
        Assert.IsInstanceOfType(result, typeof(CreateInvoiceResult.Error));
        var error = (CreateInvoiceResult.Error)result;
        StringAssert.Contains(error.Message, "Database connection failed");
    }

    [TestMethod]
    [DataRow("AB-12-CD")]
    [DataRow("WX-99-YZ")]
    public async Task CreateInvoice_SaveChangesThrowsException_ReturnsError(string licensePlate)
    {
        // Arrange
        var createDto = new CreateInvoiceDto
        {
            LicensePlateId = licensePlate,
            ParkingSessionId = 1,
            Started = DateTime.UtcNow.AddHours(-2),
            Stopped = DateTime.UtcNow,
            Cost = 10m
        };

        _mockInvoiceRepository
            .Setup(r => r.GetInvoiceModelByLicensePlate(licensePlate))
            .ReturnsAsync((InvoiceModel?)null);

        _mockInvoiceRepository
            .Setup(r => r.Create(It.IsAny<InvoiceModel>()))
            .Verifiable();

        _mockInvoiceRepository
            .Setup(r => r.SaveChangesAsync())
            .ThrowsAsync(new TimeoutException("Save operation timed out"));

        // Act
        var result = await _invoiceService.CreateInvoice(createDto);

        // Assert
        Assert.IsInstanceOfType(result, typeof(CreateInvoiceResult.Error));
        var error = (CreateInvoiceResult.Error)result;
        StringAssert.Contains(error.Message, "Save operation timed out");
    }

    [TestMethod]
    public async Task CreateInvoice_WithNullCost_ReturnsSuccess()
    {
        // Arrange
        var licensePlate = "AB-12-CD";
        var createDto = new CreateInvoiceDto
        {
            LicensePlateId = licensePlate,
            ParkingSessionId = 1,
            Started = DateTime.UtcNow.AddHours(-2),
            Stopped = DateTime.UtcNow,
            Cost = 0m
        };

        _mockInvoiceRepository
            .Setup(r => r.GetInvoiceModelByLicensePlate(licensePlate))
            .ReturnsAsync((InvoiceModel?)null);

        _mockInvoiceRepository
            .Setup(r => r.Create(It.IsAny<InvoiceModel>()))
            .ReturnsAsync(true);

        _mockInvoiceRepository
            .Setup(r => r.SaveChangesAsync())
            .ReturnsAsync(1);

        // Act
        var result = await _invoiceService.CreateInvoice(createDto);

        // Assert
        Assert.IsInstanceOfType(result, typeof(CreateInvoiceResult.Success));
        var success = (CreateInvoiceResult.Success)result;
        Assert.AreEqual(0m, success.Invoice.Cost);
    }

    [TestMethod]
    public async Task CreateInvoice_WithEmptyInvoiceSummary_ReturnsSuccess()
    {
        // Arrange
        var licensePlate = "AB-12-CD";
        var createDto = new CreateInvoiceDto
        {
            LicensePlateId = licensePlate,
            ParkingSessionId = 1,
            Started = DateTime.UtcNow.AddHours(-2),
            Stopped = DateTime.UtcNow,
            Cost = 10m,

        };

        _mockInvoiceRepository
            .Setup(r => r.GetInvoiceModelByLicensePlate(licensePlate))
            .ReturnsAsync((InvoiceModel?)null);

        _mockInvoiceRepository
            .Setup(r => r.Create(It.IsAny<InvoiceModel>()))
            .ReturnsAsync(true);

        _mockInvoiceRepository
            .Setup(r => r.SaveChangesAsync())
            .ReturnsAsync(1);

        // Act
        var result = await _invoiceService.CreateInvoice(createDto);

        // Assert
        Assert.IsInstanceOfType(result, typeof(CreateInvoiceResult.Success));
        var success = (CreateInvoiceResult.Success)result;
        Assert.IsTrue(success.Invoice.InvoiceSummary.Count > 0);

    }

    #endregion

    #region GetInvoiceByLicensePlate Tests

    [TestMethod]
    [DataRow("AB-12-CD")]
    [DataRow("WX-99-YZ")]
    public async Task GetInvoiceByLicensePlate_InvoiceExists_ReturnsSuccess(string licensePlate)
    {
        // Arrange
        var invoice = new InvoiceModel
        {
            Id = 1,
            LicensePlateId = licensePlate,
            ParkingSessionId = 5,
            Started = DateTime.UtcNow.AddHours(-3),
            Stopped = DateTime.UtcNow,
            Cost = 15m,
            InvoiceSummary = new List<string> { "Parking invoice" }
        };

        _mockInvoiceRepository
            .Setup(r => r.GetInvoiceModelByLicensePlate(licensePlate))
            .ReturnsAsync(invoice);

        // Act
        var result = await _invoiceService.GetInvoiceByLicensePlate(licensePlate);

        // Assert
        Assert.IsInstanceOfType(result, typeof(GetInvoiceResult.Success));
        var success = (GetInvoiceResult.Success)result;
        Assert.AreEqual(invoice.Id, success.Invoice.Id);
        Assert.AreEqual(licensePlate, success.Invoice.LicensePlateId);
        Assert.AreEqual(15m, success.Invoice.Cost);

        _mockInvoiceRepository.Verify(
            r => r.GetInvoiceModelByLicensePlate(licensePlate),
            Times.Once);
    }

    [TestMethod]
    [DataRow("AB-12-CD")]
    [DataRow("WX-99-YZ")]
    public async Task GetInvoiceByLicensePlate_InvoiceNotFound_ReturnsNotFound(string licensePlate)
    {
        // Arrange
        _mockInvoiceRepository
            .Setup(r => r.GetInvoiceModelByLicensePlate(licensePlate))
            .ReturnsAsync((InvoiceModel?)null);

        // Act
        var result = await _invoiceService.GetInvoiceByLicensePlate(licensePlate);

        // Assert
        Assert.IsInstanceOfType(result, typeof(GetInvoiceResult.NotFound));

        _mockInvoiceRepository.Verify(
            r => r.GetInvoiceModelByLicensePlate(licensePlate),
            Times.Once);
    }

    [TestMethod]
    [DataRow("AB-12-CD")]
    [DataRow("WX-99-YZ")]
    public async Task GetInvoiceByLicensePlate_RepositoryThrowsException_ReturnsError(string licensePlate)
    {
        // Arrange
        _mockInvoiceRepository
            .Setup(r => r.GetInvoiceModelByLicensePlate(licensePlate))
            .ThrowsAsync(new TimeoutException("Query timeout"));

        // Act
        var result = await _invoiceService.GetInvoiceByLicensePlate(licensePlate);

        // Assert
        Assert.IsInstanceOfType(result, typeof(GetInvoiceResult.Error));
        var error = (GetInvoiceResult.Error)result;
        StringAssert.Contains(error.Message, "Query timeout");
    }

    #endregion

    #region UpdateInvoice Tests

    [TestMethod]
    [DataRow("AB-12-CD", 20.5)]
    [DataRow("WX-99-YZ", 30.0)]
    public async Task UpdateInvoice_InvoiceExists_ReturnsSuccess(string licensePlate, double newCost)
    {
        // Arrange
        var existingInvoice = new InvoiceModel
        {
            Id = 1,
            LicensePlateId = licensePlate,
            ParkingSessionId = 5,
            Started = DateTime.UtcNow.AddHours(-4),
            Stopped = DateTime.UtcNow.AddHours(-1),
            Cost = 10m,
            Status = InvoiceStatus.Pending,
            InvoiceSummary = new List<string> { "Old summary" }
        };

        var updateDto = new UpdateInvoiceDto
        {
            Started = DateTime.UtcNow.AddHours(-3),
            Stopped = DateTime.UtcNow,
            Cost = (decimal)newCost,
            Status = InvoiceStatus.Paid,
        };

        _mockInvoiceRepository
            .Setup(r => r.GetInvoiceModelByLicensePlate(licensePlate))
            .ReturnsAsync(existingInvoice);

        _mockInvoiceRepository
            .Setup(r => r.Update(It.IsAny<InvoiceModel>()))
            .Verifiable();

        _mockInvoiceRepository
            .Setup(r => r.SaveChangesAsync())
            .ReturnsAsync(1);

        // Act
        var result = await _invoiceService.UpdateInvoice(licensePlate, updateDto);

        // Assert
        Assert.IsInstanceOfType(result, typeof(UpdateInvoiceResult.Success));
        var success = (UpdateInvoiceResult.Success)result;
        Assert.AreEqual((decimal)newCost, success.Invoice.Cost);
        Assert.AreEqual(InvoiceStatus.Paid, success.Invoice.Status);
        StringAssert.Contains(success.Invoice.InvoiceSummary[0], "Parking session");

        _mockInvoiceRepository.Verify(
            r => r.GetInvoiceModelByLicensePlate(licensePlate),
            Times.Once);
        _mockInvoiceRepository.Verify(
            r => r.Update(It.IsAny<InvoiceModel>()),
            Times.Once);
        _mockInvoiceRepository.Verify(
            r => r.SaveChangesAsync(),
            Times.Once);
    }

    [TestMethod]
    [DataRow("AB-12-CD")]
    [DataRow("WX-99-YZ")]
    public async Task UpdateInvoice_InvoiceNotFound_ReturnsNotFound(string licensePlate)
    {
        // Arrange
        var updateDto = new UpdateInvoiceDto
        {
            Started = DateTime.UtcNow.AddHours(-3),
            Stopped = DateTime.UtcNow,
            Cost = 20m,
            Status = InvoiceStatus.Paid,
        };

        _mockInvoiceRepository
            .Setup(r => r.GetInvoiceModelByLicensePlate(licensePlate))
            .ReturnsAsync((InvoiceModel?)null);

        // Act
        var result = await _invoiceService.UpdateInvoice(licensePlate, updateDto);

        // Assert
        Assert.IsInstanceOfType(result, typeof(UpdateInvoiceResult.NotFound));

        _mockInvoiceRepository.Verify(
            r => r.GetInvoiceModelByLicensePlate(licensePlate),
            Times.Once);
        _mockInvoiceRepository.Verify(
            r => r.Update(It.IsAny<InvoiceModel>()),
            Times.Never);
        _mockInvoiceRepository.Verify(
            r => r.SaveChangesAsync(),
            Times.Never);
    }

    [TestMethod]
    [DataRow("AB-12-CD")]
    [DataRow("WX-99-YZ")]
    public async Task UpdateInvoice_RepositoryThrowsExceptionOnLookup_ReturnsError(string licensePlate)
    {
        // Arrange
        var updateDto = new UpdateInvoiceDto
        {
            Started = DateTime.UtcNow.AddHours(-3),
            Stopped = DateTime.UtcNow,
            Cost = 20m
        };

        _mockInvoiceRepository
            .Setup(r => r.GetInvoiceModelByLicensePlate(licensePlate))
            .ThrowsAsync(new InvalidOperationException("Database error"));

        // Act
        var result = await _invoiceService.UpdateInvoice(licensePlate, updateDto);

        // Assert
        Assert.IsInstanceOfType(result, typeof(UpdateInvoiceResult.Error));
        var error = (UpdateInvoiceResult.Error)result;
        StringAssert.Contains(error.Message, "Database error");
    }

    [TestMethod]
    [DataRow("AB-12-CD")]
    [DataRow("WX-99-YZ")]
    public async Task UpdateInvoice_RepositoryThrowsExceptionOnSave_ReturnsError(string licensePlate)
    {
        // Arrange
        var existingInvoice = new InvoiceModel
        {
            Id = 1,
            LicensePlateId = licensePlate,
            ParkingSessionId = 5,
            Started = DateTime.UtcNow.AddHours(-4),
            Stopped = DateTime.UtcNow.AddHours(-1),
            Cost = 10m,
            Status = InvoiceStatus.Pending
        };

        var updateDto = new UpdateInvoiceDto
        {
            Started = DateTime.UtcNow.AddHours(-3),
            Stopped = DateTime.UtcNow,
            Cost = 20m,
            Status = InvoiceStatus.Paid
        };

        _mockInvoiceRepository
            .Setup(r => r.GetInvoiceModelByLicensePlate(licensePlate))
            .ReturnsAsync(existingInvoice);

        _mockInvoiceRepository
            .Setup(r => r.Update(It.IsAny<InvoiceModel>()))
            .Verifiable();

        _mockInvoiceRepository
            .Setup(r => r.SaveChangesAsync())
            .ThrowsAsync(new TimeoutException("Save timeout"));

        // Act
        var result = await _invoiceService.UpdateInvoice(licensePlate, updateDto);

        // Assert
        Assert.IsInstanceOfType(result, typeof(UpdateInvoiceResult.Error));
        var error = (UpdateInvoiceResult.Error)result;
        StringAssert.Contains(error.Message, "Save timeout");
    }

    [TestMethod]
    public async Task UpdateInvoice_PartialUpdate_ReturnsSuccess()
    {
        // Arrange
        var licensePlate = "AB-12-CD";
        var existingInvoice = new InvoiceModel
        {
            Id = 1,
            LicensePlateId = licensePlate,
            ParkingSessionId = 5,
            Started = DateTime.UtcNow.AddHours(-4),
            Stopped = DateTime.UtcNow.AddHours(-1),
            Cost = 10m,
            Status = InvoiceStatus.Pending,
            InvoiceSummary = new List<string> { "Old summary" }
        };

        var updateDto = new UpdateInvoiceDto
        {
            Started = existingInvoice.Started,
            Stopped = existingInvoice.Stopped,
            Cost = existingInvoice.Cost.Value,
            Status = InvoiceStatus.Paid,

        };

        _mockInvoiceRepository
            .Setup(r => r.GetInvoiceModelByLicensePlate(licensePlate))
            .ReturnsAsync(existingInvoice);

        _mockInvoiceRepository
            .Setup(r => r.Update(It.IsAny<InvoiceModel>()))
            .Verifiable();

        _mockInvoiceRepository
            .Setup(r => r.SaveChangesAsync())
            .ReturnsAsync(1);

        // Act
        var result = await _invoiceService.UpdateInvoice(licensePlate, updateDto);

        // Assert
        Assert.IsInstanceOfType(result, typeof(UpdateInvoiceResult.Success));
        var success = (UpdateInvoiceResult.Success)result;
        Assert.AreEqual(InvoiceStatus.Paid, success.Invoice.Status);
    }

    #endregion
}
