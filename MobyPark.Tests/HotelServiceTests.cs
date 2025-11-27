using MobyPark.DTOs.LicensePlate.Request;
using MobyPark.Models;
using MobyPark.Models.Repositories.Interfaces;
using MobyPark.Services;
using MobyPark.Services.Interfaces;
using MobyPark.Services.Results.LicensePlate;
using Moq;

namespace MobyPark.Tests;

[TestClass]
public class HotelServiceTests
{
    #region Setup

    private Mock<IRepository<HotelPassModel>> _mockHotelRepo = null;
    private Mock<IParkingLotService> _mockLotService = null;
    private HotelPassService _hotelService = null;

    [TestInitialize]
    public void TestInitialize()
    {
        _mockHotelRepo = new Mock<IRepository<HotelPassModel>>();
        _mockLotService = new Mock<IParkingLotService>();

        _hotelService = new HotelPassService(
            _mockHotelRepo.Object, 
            _mockLotService.Object);
    }
    #endregion
    
    
}
