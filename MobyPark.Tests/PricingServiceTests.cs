using MobyPark.Models;
using MobyPark.Services;
using MobyPark.Services.Results.Price;

namespace MobyPark.Tests;

[TestClass]
public sealed class PricingServiceTests
{
    #region Setup
    private PricingService _pricingService = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _pricingService = new PricingService();
    }

    #endregion

    #region Calculate

    [TestMethod]
    public void CalculateParkingCost_EndTimeBeforeStartTime_ReturnsError()
    {
        // Arrange
        var parkingLot = new ParkingLotModel { Tariff = 5 };
        var startTime = DateTime.UtcNow;
        var endTime = startTime.AddHours(-1);

        // Act
        var result = _pricingService.CalculateParkingCost(parkingLot, startTime, endTime);

        // Assert
        Assert.IsInstanceOfType(result, typeof(CalculatePriceResult.Error));
    }

    [TestMethod]
    [DataRow(5.0, 0, 1, 1, 0, 5.0)]
    [DataRow(5.0, 0, 2, 2, 0, 10.0)]
    [DataRow(5.0, 0, 0.5, 1, 0, 5.0)]
    [DataRow(7.5, 0, 3.2, 4, 0, 30.0)]
    public void CalculateParkingCost_NoDayTariff_CalculatesHourlyCorrectly(
        double tariff, double dayTariff, double durationHours,
        int expectedBillableHours, int expectedBillableDays, double expectedPrice)
    {
        // Arrange
        var parkingLot = new ParkingLotModel
        {
            Tariff = (decimal)tariff,
            DayTariff = dayTariff <= 0 ? null : (decimal)dayTariff
        };
        var startTime = DateTime.UtcNow;
        var endTime = startTime.AddHours(durationHours);
        var expectedDecimalPrice = (decimal)expectedPrice;

        // Act
        var result = _pricingService.CalculateParkingCost(parkingLot, startTime, endTime);

        // Assert
        Assert.IsInstanceOfType(result, typeof(CalculatePriceResult.Success));
        var successResult = (CalculatePriceResult.Success)result;
        Assert.AreEqual(expectedDecimalPrice, successResult.Price);
        Assert.AreEqual(expectedBillableHours, successResult.BillableHours);
        Assert.AreEqual(expectedBillableDays, successResult.BillableDays);
    }

    [TestMethod]
    [DataRow(5.0, 20.0, 3, 3, 0, 15.0)]
    [DataRow(5.0, 20.0, 4, 4, 1, 20.0)]  // 4 hours should trigger day tariff here, as it's the exact threshold for day tariff
    [DataRow(5.0, 20.0, 5, 5, 1, 20.0)]
    [DataRow(5.0, 20.0, 23.5, 24, 1, 20.0)]
    [DataRow(3.0, 15.0, 5.1, 6, 1, 15.0)]
    public void CalculateParkingCost_WithDayTariff_LessThan24Hours_CalculatesCorrectly(
        double tariff, double dayTariff, double durationHours,
        int expectedBillableHours, int expectedBillableDays, double expectedPrice)
    {
        // Arrange
        var parkingLot = new ParkingLotModel
        {
            Tariff = (decimal)tariff,
            DayTariff = (decimal)dayTariff
        };
        var startTime = DateTime.UtcNow;
        var endTime = startTime.AddHours(durationHours);
        var expectedDecimalPrice = (decimal)expectedPrice;

        // Act
        var result = _pricingService.CalculateParkingCost(parkingLot, startTime, endTime);

        // Assert
        Assert.IsInstanceOfType(result, typeof(CalculatePriceResult.Success));
        var successResult = (CalculatePriceResult.Success)result;
        Assert.AreEqual(expectedDecimalPrice, successResult.Price);
        Assert.AreEqual(expectedBillableHours, successResult.BillableHours);
        Assert.AreEqual(expectedBillableDays, successResult.BillableDays);
    }

    [TestMethod]
    [DataRow(5.0, 20.0, 25, 25, 2, 40.0)]
    [DataRow(5.0, 20.0, 48, 48, 2, 40.0)]
    [DataRow(5.0, 20.0, 48.1, 49, 3, 60.0)]
    [DataRow(2.0, 10.0, 72.5, 73, 4, 40.0)]
    public void CalculateParkingCost_WithDayTariff_MoreThan24Hours_CalculatesByDay(
        double tariff, double dayTariff, double durationHours,
        int expectedBillableHours, int expectedBillableDays, double expectedPrice)
    {
        // Arrange
        var parkingLot = new ParkingLotModel
        {
            Tariff = (decimal)tariff,
            DayTariff = (decimal)dayTariff
        };
        var startTime = DateTime.UtcNow;
        var endTime = startTime.AddHours(durationHours);
        var expectedDecimalPrice = (decimal)expectedPrice;

        // Act
        var result = _pricingService.CalculateParkingCost(parkingLot, startTime, endTime);

        // Assert
        Assert.IsInstanceOfType(result, typeof(CalculatePriceResult.Success));
        var successResult = (CalculatePriceResult.Success)result;
        Assert.AreEqual(expectedDecimalPrice, successResult.Price);
        Assert.AreEqual(expectedBillableHours, successResult.BillableHours);
        Assert.AreEqual(expectedBillableDays, successResult.BillableDays);
    }

    #endregion
}