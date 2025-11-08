using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using MobyPark.Models;
using MobyPark.Services;
using MobyPark.Models.Repositories.Interfaces;
using MobyPark.Services.Results.ParkingLot;

namespace MobyPark.Tests
{
    [TestClass]
    public sealed class ParkingLotServiceTests
    {
        private Mock<IParkingLotRepository> _repo = null!;
        private ParkingLotService _service = null!;

        [TestInitialize]
        public void Init()
        {
            _repo = new Mock<IParkingLotRepository>(MockBehavior.Strict);
            // SessionService isn’t used in your current service methods, pass a dummy if needed
            _service = new ParkingLotService(_repo.Object, sessions: null!);
        }

        #region Insert

        [TestMethod]
        public async Task Insert_Valid_ReturnsSuccess()
        {
            // Arrange
            var lot = new ParkingLotModel
            {
                Name = "Lot A",
                Location = "Location A",
                Address = "Address A",
                Capacity = 100,
                Reserved = 0,
                Tariff = 5,
                DayTariff = 20
            };

            _repo.Setup(r => r.GetParkingLotByAddress(lot.Address)).ReturnsAsync((ParkingLotModel?)null);
            _repo.Setup(r => r.AddParkingLotAsync(lot)).ReturnsAsync(123);

            // Act
            var result = await _service.InsertParkingLotAsync(lot);

            // Assert
            Assert.IsInstanceOfType(result, typeof(RegisterResult.Success));
            var success = (RegisterResult.Success)result;
            Assert.AreEqual(123, success.ParkingLot.Id);

            _repo.Verify(r => r.GetParkingLotByAddress(lot.Address), Times.Once);
            _repo.Verify(r => r.AddParkingLotAsync(lot), Times.Once);
        }

        [TestMethod]
        public async Task Insert_AddressTaken_ReturnsAddressTaken()
        {
            // Arrange
            var lot = new ParkingLotModel
            {
                Name = "Lot A",
                Location = "Location A",
                Address = "Address A",
                Capacity = 100,
                Reserved = 0,
                Tariff = 5
            };

            _repo.Setup(r => r.GetParkingLotByAddress(lot.Address)).ReturnsAsync(new ParkingLotModel { Id = 1, Address = lot.Address });

            // Act
            var result = await _service.InsertParkingLotAsync(lot);

            // Assert
            Assert.IsInstanceOfType(result, typeof(RegisterResult.AddressTaken));
            _repo.Verify(r => r.GetParkingLotByAddress(lot.Address), Times.Once);
            _repo.Verify(r => r.AddParkingLotAsync(It.IsAny<ParkingLotModel>()), Times.Never);
        }

        [TestMethod]
        public async Task Insert_InvalidData_MissingFields_ReturnsInvalidData()
        {
            // Arrange: missing Name
            var lot = new ParkingLotModel
            {
                Name = " ",
                Location = "Location",
                Address = "Address",
                Capacity = 10,
                Tariff = 1
            };

            // Act
            var result = await _service.InsertParkingLotAsync(lot);

            // Assert
            Assert.IsInstanceOfType(result, typeof(RegisterResult.InvalidData));
            _repo.Verify(r => r.AddParkingLotAsync(It.IsAny<ParkingLotModel>()), Times.Never);
        }

        [TestMethod]
        public async Task Insert_InvalidData_NegativeValues_ReturnsInvalidData()
        {
            // Arrange
            var lot = new ParkingLotModel
            {
                Name = "Lot",
                Location = "Loc",
                Address = "Addr",
                Capacity = -1,
                Tariff = 1
            };

            // Act
            var result = await _service.InsertParkingLotAsync(lot);

            // Assert
            Assert.IsInstanceOfType(result, typeof(RegisterResult.InvalidData));
            _repo.Verify(r => r.AddParkingLotAsync(It.IsAny<ParkingLotModel>()), Times.Never);
        }

        [TestMethod]
        public async Task Insert_InvalidData_ReservedOutOfRange_ReturnsInvalidData()
        {
            var lot = new ParkingLotModel
            {
                Name = "Lot",
                Location = "Loc",
                Address = "Addr",
                Capacity = 10,
                Reserved = 11,
                Tariff = 1
            };

            var result = await _service.InsertParkingLotAsync(lot);

            Assert.IsInstanceOfType(result, typeof(RegisterResult.InvalidData));
            _repo.Verify(r => r.AddParkingLotAsync(It.IsAny<ParkingLotModel>()), Times.Never);
        }

        #endregion

        #region Get

        [TestMethod]
        public async Task GetById_Found_ReturnsModel()
        {
            _repo.Setup(r => r.GetParkingLotByID(7)).ReturnsAsync(new ParkingLotModel { Id = 7 });

            var lot = await _service.GetParkingLotById(7);

            Assert.IsNotNull(lot);
            Assert.AreEqual(7, lot!.Id);
            _repo.Verify(r => r.GetParkingLotByID(7), Times.Once);
        }

        [TestMethod]
        public async Task GetById_NotFound_ReturnsNull()
        {
            _repo.Setup(r => r.GetParkingLotByID(99)).ReturnsAsync((ParkingLotModel?)null);

            var lot = await _service.GetParkingLotById(99);

            Assert.IsNull(lot);
            _repo.Verify(r => r.GetParkingLotByID(99), Times.Once);
        }

        #endregion

        #region Update

        [TestMethod]
        public async Task UpdateById_Found_ReturnsSuccess()
        {
            var input = new ParkingLotModel
            {
                Name = "New",
                Location = "Loc",
                Address = "Addr",
                Capacity = 50,
                Reserved = 0,
                Tariff = 2,
                DayTariff = 10
            };

            var updated = new ParkingLotModel { Id = 5, Name = "New" };

            _repo.Setup(r => r.UpdateParkingLotByID(input, 5)).ReturnsAsync(updated);

            var result = await _service.UpdateParkingLotByIDAsync(input, 5);

            Assert.IsInstanceOfType(result, typeof(RegisterResult.Success));
            _repo.Verify(r => r.UpdateParkingLotByID(input, 5), Times.Once);
        }

        [TestMethod]
        public async Task UpdateById_NotFound_ReturnsNotFound()
        {
            var input = new ParkingLotModel { Name = "New" };
            _repo.Setup(r => r.UpdateParkingLotByID(input, 5)).ReturnsAsync((ParkingLotModel?)null);

            var result = await _service.UpdateParkingLotByIDAsync(input, 5);

            Assert.IsInstanceOfType(result, typeof(RegisterResult.NotFound));
        }

        [TestMethod]
        public async Task UpdateByAddress_Found_ReturnsSuccess()
        {
            var input = new ParkingLotModel { Name = "New", Address = "A" };
            var updated = new ParkingLotModel { Id = 2, Address = "A", Name = "New" };

            _repo.Setup(r => r.UpdateParkingLotByAddress(input, "A")).ReturnsAsync(updated);

            var result = await _service.UpdateParkingLotByAddressAsync(input, "A");

            Assert.IsInstanceOfType(result, typeof(RegisterResult.Success));
            _repo.Verify(r => r.UpdateParkingLotByAddress(input, "A"), Times.Once);
        }

        [TestMethod]
        public async Task UpdateByAddress_NotFound_ReturnsNotFound()
        {
            var input = new ParkingLotModel { Name = "New" };
            _repo.Setup(r => r.UpdateParkingLotByAddress(input, "X")).ReturnsAsync((ParkingLotModel?)null);

            var result = await _service.UpdateParkingLotByAddressAsync(input, "X");

            Assert.IsInstanceOfType(result, typeof(RegisterResult.NotFound));
        }

        #endregion

        #region Delete

        [TestMethod]
        public async Task DeleteById_Found_ReturnsSuccessfullyDeleted()
        {
            _repo.Setup(r => r.DeleteParkingLotByID(3)).ReturnsAsync(true);

            var result = await _service.DeleteParkingLotByIDAsync(3);

            Assert.IsInstanceOfType(result, typeof(RegisterResult.SuccessfullyDeleted));
            _repo.Verify(r => r.DeleteParkingLotByID(3), Times.Once);
        }

        [TestMethod]
        public async Task DeleteById_NotFound_ReturnsNotFound()
        {
            _repo.Setup(r => r.DeleteParkingLotByID(3)).ReturnsAsync(false);

            var result = await _service.DeleteParkingLotByIDAsync(3);

            Assert.IsInstanceOfType(result, typeof(RegisterResult.NotFound));
        }

        [TestMethod]
        public async Task DeleteByAddress_Found_ReturnsSuccessfullyDeleted()
        {
            _repo.Setup(r => r.DeleteParkingLotByAddress("Addr")).ReturnsAsync(true);

            var result = await _service.DeleteParkingLotByAddressAsync("Addr");

            Assert.IsInstanceOfType(result, typeof(RegisterResult.SuccessfullyDeleted));
            _repo.Verify(r => r.DeleteParkingLotByAddress("Addr"), Times.Once);
        }

        [TestMethod]
        public async Task DeleteByAddress_NotFound_ReturnsNotFound()
        {
            _repo.Setup(r => r.DeleteParkingLotByAddress("Addr")).ReturnsAsync(false);

            var result = await _service.DeleteParkingLotByAddressAsync("Addr");

            Assert.IsInstanceOfType(result, typeof(RegisterResult.NotFound));
        }

        #endregion
    }
}