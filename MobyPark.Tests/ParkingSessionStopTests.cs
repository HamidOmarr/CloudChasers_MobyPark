using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MobyPark.Controllers;
using MobyPark.Models;
using MobyPark.Services;
using MobyPark.Models.Repositories.Interfaces;
using MobyPark.Models.Requests.Session;

namespace MobyPark.Tests.Controllers
{
    [TestClass]
    public class SessionStopTests
    {
        private Mock<ParkingSessionService>? _parkingSessionServiceMock;
        private Mock<PaymentService>? _paymentServiceMock;
        private Mock<ServiceStack>? _servicesMock;
        private ParkingSessionController? _controller;
        private UserModel? _user;

        #region Setup
        [TestInitialize]
        public void Setup()
        {
            _parkingSessionServiceMock = new Mock<ParkingSessionService>();
            _paymentServiceMock = new Mock<PaymentService>();

            _servicesMock = new Mock<ServiceStack>();
            _servicesMock.SetupGet(s => s.ParkingSessions).Returns(_parkingSessionServiceMock.Object);
            _servicesMock.SetupGet(s => s.Payments).Returns(_paymentServiceMock.Object);

            _controller = new ParkingSessionController(_servicesMock.Object);
            _user = new UserModel { Username = "testuser", Role = "USER" };
            _controller.SetCurrentUser(_user);
        }
        #endregion

        #region Tests

        [TestMethod]
        public async Task StopSession_SessionNotFound_ReturnsNotFound()
        {
            // Arrange
            var request = new StopSessionRequest { LicensePlate = "ABC123" };

            _parkingSessionServiceMock!
                .Setup(s => s.GetParkingLotSessionByLicensePlateAndParkingLotId(It.IsAny<int>(), It.IsAny<StopSessionRequest>()))
                .ReturnsAsync((ParkingSessionModel?)null);

            // Act
            var result = await _controller!.StopSession(1, request);

            // Assert
            Assert.IsInstanceOfType(result, typeof(NotFoundObjectResult));
        }

        [TestMethod]
        public async Task StopSession_SessionAlreadyStopped_ReturnsBadRequest()
        {
            // Arrange
            var session = new ParkingSessionModel
            {
                ParkingLotId = 1,
                User = "testuser",
                Stopped = DateTime.UtcNow
            };

            _parkingSessionServiceMock!
                .Setup(s => s.GetParkingLotSessionByLicensePlateAndParkingLotId(1, It.IsAny<StopSessionRequest>()))
                .ReturnsAsync(session);

            var request = new StopSessionRequest
            {
                LicensePlate = "ABC123",
                PaymentValidation = new PaymentValidation { Confirmed = true }
            };

            // Act
            var result = await _controller!.StopSession(1, request);

            // Assert
            Assert.IsInstanceOfType(result, typeof(BadRequestObjectResult));
        }

        [TestMethod]
        public async Task StopSession_NoPaymentsFound_ReturnsBadRequest()
        {
            // Arrange
            var session = new ParkingSessionModel { ParkingLotId = 1, User = "testuser" };

            _parkingSessionServiceMock!
                .Setup(s => s.GetParkingLotSessionByLicensePlateAndParkingLotId(1, It.IsAny<StopSessionRequest>()))
                .ReturnsAsync(session);

            _paymentServiceMock!
                .Setup(p => p.GetPaymentsByUser("testuser"))
                .ReturnsAsync(new List<PaymentModel>());

            var request = new StopSessionRequest
            {
                LicensePlate = "ABC123",
                PaymentValidation = new PaymentValidationDto { Confirmed = true }
            };

            // Act
            var result = await _controller!.StopSession(1, request);

            // Assert
            Assert.IsInstanceOfType(result, typeof(BadRequestObjectResult));
        }

        [TestMethod]
        public async Task StopSession_PaymentNotConfirmed_ReturnsBadRequest()
        {
            // Arrange
            var session = new ParkingSessionModel { ParkingLotId = 1, User = "testuser" };

            _parkingSessionServiceMock!
                .Setup(s => s.GetParkingLotSessionByLicensePlateAndParkingLotId(1, It.IsAny<StopSessionRequest>()))
                .ReturnsAsync(session);

            _paymentServiceMock!
                .Setup(p => p.GetPaymentsByUser("testuser"))
                .ReturnsAsync(new List<PaymentModel>
                {
                    new PaymentModel { Amount = 10, TransactionId = "tx1" }
                });

            var request = new StopSessionRequest
            {
                LicensePlate = "ABC123",
                PaymentValidation = new PaymentValidationDto { Confirmed = false }
            };

            // Act
            var result = await _controller!.StopSession(1, request);

            // Assert
            Assert.IsInstanceOfType(result, typeof(BadRequestObjectResult));
        }

        [TestMethod]
        public async Task StopSession_PaymentValidationFails_ReturnsBadRequest()
        {
            // Arrange
            var session = new ParkingSessionModel { ParkingLotId = 1, User = "testuser" };

            _parkingSessionServiceMock!
                .Setup(s => s.GetParkingLotSessionByLicensePlateAndParkingLotId(1, It.IsAny<StopSessionRequest>()))
                .ReturnsAsync(session);

            _paymentServiceMock!
                .Setup(p => p.GetPaymentsByUser("testuser"))
                .ReturnsAsync(new List<PaymentModel>
                {
                    new PaymentModel { Amount = 10, TransactionId = "tx1" }
                });

            _paymentServiceMock!
                .Setup(p => p.ValidatePayment("tx1", It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync((PaymentModel?)null);

            var request = new StopSessionRequest
            {
                LicensePlate = "ABC123",
                PaymentValidation = new PaymentValidationDto
                {
                    Confirmed = true,
                    Validation = "vcode",
                    TransactionData = "tdata"
                }
            };

            // Act
            var result = await _controller!.StopSession(1, request);

            // Assert
            Assert.IsInstanceOfType(result, typeof(BadRequestObjectResult));
        }

        [TestMethod]
        public async Task StopSession_ValidPayment_Success()
        {
            // Arrange
            var session = new ParkingSessionModel { ParkingLotId = 1, User = "testuser" };

            _parkingSessionServiceMock!
                .Setup(s => s.GetParkingLotSessionByLicensePlateAndParkingLotId(1, It.IsAny<StopSessionRequest>()))
                .ReturnsAsync(session);

            _paymentServiceMock!
                .Setup(p => p.GetPaymentsByUser("testuser"))
                .ReturnsAsync(new List<PaymentModel>
                {
                    new PaymentModel { Amount = 10, TransactionId = "tx1" }
                });

            _paymentServiceMock!
                .Setup(p => p.ValidatePayment("tx1", It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new PaymentModel { TransactionId = "tx1", Amount = 10 });

            _paymentServiceMock!
                .Setup(p => p.GetTotalAmountForTransaction("tx1"))
                .ReturnsAsync(10m);

            var request = new StopSessionRequest
            {
                LicensePlate = "ABC123",
                PaymentValidation = new PaymentValidationDto
                {
                    Confirmed = true,
                    Validation = "ok",
                    TransactionData = "data"
                }
            };

            // Act
            var result = await _controller!.StopSession(1, request);

            // Assert
            Assert.IsInstanceOfType(result, typeof(OkObjectResult));

            var okResult = result as OkObjectResult;
            Assert.IsNotNull(okResult?.Value);

            dynamic value = okResult.Value;
            Assert.AreEqual("Stopped", value.status);

            _parkingSessionServiceMock.Verify(s => s.UpdateParkingSession(It.Is<ParkingSessionModel>(
                sess => sess.Stopped.HasValue
            )), Times.Once);
        }

        #endregion
    }
}
