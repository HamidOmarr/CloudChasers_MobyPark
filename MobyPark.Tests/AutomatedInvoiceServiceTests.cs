using Microsoft.VisualStudio.TestTools.UnitTesting;

using Moq;

using System.Collections.Generic;
using System.Threading.Tasks;

using MobyPark.Services;
using MobyPark.Services.Interfaces;
using MobyPark.Models;
using MobyPark.Models.Repositories.Interfaces;
using MobyPark.DTOs.Invoice;
using MobyPark.Services.Results.Invoice;

namespace MobyPark.Tests.Services
{
    [TestClass]
    public class InvoiceServiceTests
    {
        private Mock<IInvoiceRepository> _mockInvoiceRepository = null!;
        private IAutomatedInvoiceService _invoiceService = null!;

        [TestInitialize]
        public void Setup()
        {
            _mockInvoiceRepository = new Mock<IInvoiceRepository>();
            _invoiceService = new AutomatedInvoiceService(_mockInvoiceRepository.Object);
        }

        #region CreateInvoice

        [TestMethod]
        [DataRow("AB-12-CD", 1, 120, 10.5)]
        [DataRow("WX-99-YZ", 5, 180, 25.75)]
        public async Task CreateInvoice_NewInvoice_ReturnsSuccess(
            string licensePlate,
            long sessionId,
            int duration,
            double cost)
        {
            // Arrange
            var createDto = new CreateInvoiceDto
            {
                LicensePlateId = licensePlate,
                ParkingSessionId = sessionId,
                SessionDuration = duration,
                Cost = (decimal)cost
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
            Assert.AreEqual(duration, success.Invoice.SessionDuration);
            Assert.AreEqual((decimal)cost, success.Invoice.Cost);

            _mockInvoiceRepository.Verify(
                r => r.GetInvoiceModelByLicensePlate(licensePlate), Times.Once);
            _mockInvoiceRepository.Verify(
                r => r.Create(It.IsAny<InvoiceModel>()), Times.Once);
            _mockInvoiceRepository.Verify(
                r => r.SaveChangesAsync(), Times.Once);
        }

        [TestMethod]
        public async Task CreateInvoice_InvoiceAlreadyExists_ReturnsAlreadyExists()
        {
            // Arrange
            var licensePlate = "AB-12-CD";

            var existingInvoice = new InvoiceModel
            {
                Id = 1,
                LicensePlateId = licensePlate,
                ParkingSessionId = 1,
                SessionDuration = 120,
                Cost = 10m
            };

            var createDto = new CreateInvoiceDto
            {
                LicensePlateId = licensePlate,
                ParkingSessionId = 2,
                SessionDuration = 180,
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
                r => r.Create(It.IsAny<InvoiceModel>()), Times.Never);
        }

        #endregion

        #region GetInvoiceByLicensePlate

        [TestMethod]
        public async Task GetInvoiceByLicensePlate_InvoiceExists_ReturnsSuccess()
        {
            // Arrange
            var licensePlate = "AB-12-CD";

            var invoice = new InvoiceModel
            {
                Id = 1,
                LicensePlateId = licensePlate,
                ParkingSessionId = 5,
                SessionDuration = 180,
                Cost = 15m,
                InvoiceSummary = new List<string> { "Parkingkosten" }
            };

            _mockInvoiceRepository
                .Setup(r => r.GetInvoiceModelByLicensePlate(licensePlate))
                .ReturnsAsync(invoice);

            // Act
            var result = await _invoiceService.GetInvoiceByLicensePlate(licensePlate);

            // Assert
            Assert.IsInstanceOfType(result, typeof(GetInvoiceResult.Success));
            var success = (GetInvoiceResult.Success)result;

            Assert.AreEqual(licensePlate, success.Invoice.LicensePlateId);
            Assert.AreEqual(180, success.Invoice.SessionDuration);
            Assert.AreEqual(15m, success.Invoice.Cost);
        }

        [TestMethod]
        public async Task GetInvoiceByLicensePlate_NotFound_ReturnsNotFound()
        {
            // Arrange
            _mockInvoiceRepository
                .Setup(r => r.GetInvoiceModelByLicensePlate(It.IsAny<string>()))
                .ReturnsAsync((InvoiceModel?)null);

            // Act
            var result = await _invoiceService.GetInvoiceByLicensePlate("ZZ-99-ZZ");

            // Assert
            Assert.IsInstanceOfType(result, typeof(GetInvoiceResult.NotFound));
        }

        #endregion

        #region UpdateInvoice

        [TestMethod]
        public async Task UpdateInvoice_ValidUpdate_ReturnsSuccess()
        {
            // Arrange
            var licensePlate = "AB-12-CD";

            var existingInvoice = new InvoiceModel
            {
                Id = 1,
                LicensePlateId = licensePlate,
                ParkingSessionId = 3,
                SessionDuration = 120,
                Cost = 10m,
                Status = InvoiceStatus.Pending,
                InvoiceSummary = new List<string> { "Oud" }
            };

            var updateDto = new UpdateInvoiceDto
            {
                SessionDuration = 180,
                Cost = 20m,
                Status = InvoiceStatus.Paid
            };

            _mockInvoiceRepository
                .Setup(r => r.GetInvoiceModelByLicensePlate(licensePlate))
                .ReturnsAsync(existingInvoice);

            _mockInvoiceRepository
                .Setup(r => r.SaveChangesAsync())
                .ReturnsAsync(1);

            // Act
            var result = await _invoiceService.UpdateInvoice(licensePlate, updateDto);

            // Assert
            Assert.IsInstanceOfType(result, typeof(UpdateInvoiceResult.Success));
            var success = (UpdateInvoiceResult.Success)result;

            Assert.AreEqual(180, success.Invoice.SessionDuration);
            Assert.AreEqual(20m, success.Invoice.Cost);
            Assert.AreEqual(InvoiceStatus.Paid, success.Invoice.Status);
        }

        [TestMethod]
        public async Task UpdateInvoice_NotFound_ReturnsNotFound()
        {
            // Arrange
            _mockInvoiceRepository
                .Setup(r => r.GetInvoiceModelByLicensePlate(It.IsAny<string>()))
                .ReturnsAsync((InvoiceModel?)null);

            var updateDto = new UpdateInvoiceDto
            {
                SessionDuration = 100,
                Cost = 10m,
                Status = InvoiceStatus.Paid
            };

            // Act
            var result = await _invoiceService.UpdateInvoice("XX-00-XX", updateDto);

            // Assert
            Assert.IsInstanceOfType(result, typeof(UpdateInvoiceResult.NotFound));
        }

        #endregion
    }
}
