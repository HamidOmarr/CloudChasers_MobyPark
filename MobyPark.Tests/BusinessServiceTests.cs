using MobyPark.DTOs.Business;
using MobyPark.Models;
using MobyPark.Models.Repositories.Interfaces;
using MobyPark.Services;
using MobyPark.Services.Results;

using MockQueryable.Moq;

using Moq;

namespace MobyPark.Tests;

[TestClass]
public sealed class BusinessServiceTests
{
    #region Setup
    private Mock<IRepository<BusinessModel>> _mockBusinessRepo = null!;

    private BusinessService _businessService = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _mockBusinessRepo = new Mock<IRepository<BusinessModel>>();

        _businessService = new BusinessService(_mockBusinessRepo.Object);
    }
    #endregion

    #region CreateBusinessAsync

    [TestMethod]
    public async Task CreateBusinessAsync_AddressTaken_ReturnsConflict()
    {
        var dto = new CreateBusinessDto
        {
            Name = "Test",
            Address = "Teststreet 1",
            IBAN = "NL91ABNA0417164300"
        };

        var normalized = dto.Address.Trim().ToLower();
        var data = new List<BusinessModel>
        {
            new BusinessModel { Id = 1, Name = "Existing", Address = normalized, IBAN = "NL91ABNA0417164300" }
        };

        var mockSet = data.BuildMockDbSet();
        _mockBusinessRepo.Setup(r => r.Query()).Returns(mockSet.Object);

        var result = await _businessService.CreateBusinessAsync(dto);

        Assert.AreEqual(ServiceStatus.Conflict, result.Status);
    }

    [TestMethod]
    public async Task CreateBusinessAsync_InvalidIban_ReturnsBadRequest()
    {
        var dto = new CreateBusinessDto
        {
            Name = "Test",
            Address = "Teststreet 1",
            IBAN = "INVALIDIBAN"
        };

        var data = new List<BusinessModel>();
        var mockSet = data.BuildMockDbSet();
        _mockBusinessRepo.Setup(r => r.Query()).Returns(mockSet.Object);

        var result = await _businessService.CreateBusinessAsync(dto);

        Assert.AreEqual(ServiceStatus.BadRequest, result.Status);
    }

    [TestMethod]
    public async Task CreateBusinessAsync_Success_ReturnsOk()
    {
        var dto = new CreateBusinessDto
        {
            Name = "  Test  ",
            Address = "  Teststreet 1  ",
            IBAN = "NL91ABNA0417164300"
        };

        var data = new List<BusinessModel>();
        var mockSet = data.BuildMockDbSet();
        _mockBusinessRepo.Setup(r => r.Query()).Returns(mockSet.Object);

        _mockBusinessRepo.Setup(r => r.Add(It.IsAny<BusinessModel>()))
            .Callback<BusinessModel>(m => m.Id = 123);

        _mockBusinessRepo.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);

        var result = await _businessService.CreateBusinessAsync(dto);

        Assert.AreEqual(ServiceStatus.Success, result.Status);
        Assert.IsNotNull(result.Data);
        Assert.AreEqual(123, result.Data.Id);
        Assert.AreEqual("Test", result.Data.Name);
        Assert.AreEqual("Teststreet 1", result.Data.Address);
        Assert.AreEqual("NL91ABNA0417164300", result.Data.IBAN);

        _mockBusinessRepo.Verify(r => r.Add(It.IsAny<BusinessModel>()), Times.Once);
        _mockBusinessRepo.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    #endregion

    #region PatchBusinessAsync

    [TestMethod]
    public async Task PatchBusinessAsync_NotFound_ReturnsNotFound()
    {
        long id = 1;
        var patch = new PatchBusinessDto { Name = "New" };

        _mockBusinessRepo.Setup(r => r.FindByIdAsync(id)).ReturnsAsync((BusinessModel?)null);

        var result = await _businessService.PatchBusinessAsync(id, patch);

        Assert.AreEqual(ServiceStatus.NotFound, result.Status);
    }

    [TestMethod]
    public async Task PatchBusinessAsync_AddressTaken_ReturnsConflict()
    {
        long id = 1;
        var existing = new BusinessModel { Id = id, Name = "A", Address = "old", IBAN = "NL91ABNA0417164300" };
        var patch = new PatchBusinessDto { Address = "New Street 1" };

        _mockBusinessRepo.Setup(r => r.FindByIdAsync(id)).ReturnsAsync(existing);

        var normalized = patch.Address.Trim().ToLower();
        var data = new List<BusinessModel>
        {
            new BusinessModel { Id = 999, Name = "Other", Address = normalized, IBAN = "NL91ABNA0417164300" }
        };
        var mockSet = data.BuildMockDbSet();
        _mockBusinessRepo.Setup(r => r.Query()).Returns(mockSet.Object);

        var result = await _businessService.PatchBusinessAsync(id, patch);

        Assert.AreEqual(ServiceStatus.Conflict, result.Status);
    }

    [TestMethod]
    public async Task PatchBusinessAsync_InvalidIban_ReturnsBadRequest()
    {
        long id = 1;
        var existing = new BusinessModel { Id = id, Name = "A", Address = "old", IBAN = "NL91ABNA0417164300" };
        var patch = new PatchBusinessDto { IBAN = "INVALID" };

        _mockBusinessRepo.Setup(r => r.FindByIdAsync(id)).ReturnsAsync(existing);

        var result = await _businessService.PatchBusinessAsync(id, patch);

        Assert.AreEqual(ServiceStatus.BadRequest, result.Status);
    }

    [TestMethod]
    public async Task PatchBusinessAsync_Success_ReturnsOk()
    {
        long id = 1;
        var existing = new BusinessModel { Id = id, Name = "A", Address = "Old Street 1", IBAN = "NL91ABNA0417164300" };
        var patch = new PatchBusinessDto
        {
            Name = "  NewName  ",
            Address = "  New Street 1  ",
            IBAN = "NL91ABNA0417164300"
        };

        _mockBusinessRepo.Setup(r => r.FindByIdAsync(id)).ReturnsAsync(existing);

        var data = new List<BusinessModel>();
        var mockSet = data.BuildMockDbSet();
        _mockBusinessRepo.Setup(r => r.Query()).Returns(mockSet.Object);

        _mockBusinessRepo.Setup(r => r.Update(It.IsAny<BusinessModel>()));
        _mockBusinessRepo.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);

        var result = await _businessService.PatchBusinessAsync(id, patch);

        Assert.AreEqual(ServiceStatus.Success, result.Status);
        Assert.IsNotNull(result.Data);
        Assert.AreEqual(id, result.Data.Id);
        Assert.AreEqual("NewName", result.Data.Name);
        Assert.AreEqual("New Street 1", result.Data.Address);
        Assert.AreEqual("NL91ABNA0417164300", result.Data.IBAN);

        _mockBusinessRepo.Verify(r => r.Update(It.IsAny<BusinessModel>()), Times.Once);
        _mockBusinessRepo.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    #endregion

    #region DeleteBusinessByIdAsync

    [TestMethod]
    public async Task DeleteBusinessByIdAsync_NotFound_ReturnsNotFound()
    {
        long id = 1;

        _mockBusinessRepo.Setup(r => r.FindByIdAsync(id)).ReturnsAsync((BusinessModel?)null);

        var result = await _businessService.DeleteBusinessByIdAsync(id);

        Assert.AreEqual(ServiceStatus.NotFound, result.Status);
    }

    [TestMethod]
    public async Task DeleteBusinessByIdAsync_Success_ReturnsOk()
    {
        long id = 1;
        var existing = new BusinessModel { Id = id, Name = "A", Address = "Old", IBAN = "NL91ABNA0417164300" };

        _mockBusinessRepo.Setup(r => r.FindByIdAsync(id)).ReturnsAsync(existing);
        _mockBusinessRepo.Setup(r => r.Deletee(existing));
        _mockBusinessRepo.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);

        var result = await _businessService.DeleteBusinessByIdAsync(id);

        Assert.AreEqual(ServiceStatus.Success, result.Status);
        Assert.AreEqual(true, result.Data);

        _mockBusinessRepo.Verify(r => r.Deletee(existing), Times.Once);
        _mockBusinessRepo.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    #endregion

    #region GetAllAsync

    [TestMethod]
    public async Task GetAllAsync_Success_ReturnsOkWithList()
    {
        var list = new List<BusinessModel>
        {
            new BusinessModel { Id = 1, Name = "A", Address = "Addr1", IBAN = "NL91ABNA0417164300" },
            new BusinessModel { Id = 2, Name = "B", Address = "Addr2", IBAN = "NL91ABNA0417164300" }
        };

        _mockBusinessRepo.Setup(r => r.ReadAllAsync()).ReturnsAsync(list);

        var result = await _businessService.GetAllAsync();

        Assert.AreEqual(ServiceStatus.Success, result.Status);
        Assert.IsNotNull(result.Data);
        Assert.AreEqual(2, result.Data.Count);
        Assert.AreEqual(1, result.Data[0].Id);
        Assert.AreEqual(2, result.Data[1].Id);
    }

    #endregion

    #region GetBusinessByIdAsync

    [TestMethod]
    public async Task GetBusinessByIdAsync_NotFound_ReturnsNotFound()
    {
        long id = 1;

        _mockBusinessRepo.Setup(r => r.FindByIdAsync(id)).ReturnsAsync((BusinessModel?)null);

        var result = await _businessService.GetBusinessByIdAsync(id);

        Assert.AreEqual(ServiceStatus.NotFound, result.Status);
    }

    [TestMethod]
    public async Task GetBusinessByIdAsync_Success_ReturnsOk()
    {
        long id = 1;
        var existing = new BusinessModel { Id = id, Name = "A", Address = "Addr1", IBAN = "NL91ABNA0417164300" };

        _mockBusinessRepo.Setup(r => r.FindByIdAsync(id)).ReturnsAsync(existing);

        var result = await _businessService.GetBusinessByIdAsync(id);

        Assert.AreEqual(ServiceStatus.Success, result.Status);
        Assert.IsNotNull(result.Data);
        Assert.AreEqual(id, result.Data.Id);
    }

    #endregion

    #region GetBusinessByAddressAsync

    [TestMethod]
    public async Task GetBusinessByAddressAsync_NotFound_ReturnsNotFound()
    {
        var address = "Addr1";
        var data = new List<BusinessModel>();
        var mockSet = data.BuildMockDbSet();
        _mockBusinessRepo.Setup(r => r.Query()).Returns(mockSet.Object);

        var result = await _businessService.GetBusinessByAddressAsync(address);

        Assert.AreEqual(ServiceStatus.NotFound, result.Status);
    }

    [TestMethod]
    public async Task GetBusinessByAddressAsync_Success_ReturnsOk()
    {
        var address = "  Addr1  ";
        var normalized = address.Trim().ToLower();

        var existing = new BusinessModel { Id = 1, Name = "A", Address = normalized, IBAN = "NL91ABNA0417164300" };
        var data = new List<BusinessModel> { existing };
        var mockSet = data.BuildMockDbSet();
        _mockBusinessRepo.Setup(r => r.Query()).Returns(mockSet.Object);

        var result = await _businessService.GetBusinessByAddressAsync(address);

        Assert.AreEqual(ServiceStatus.Success, result.Status);
        Assert.IsNotNull(result.Data);
        Assert.AreEqual(1, result.Data.Id);
    }

    #endregion
}