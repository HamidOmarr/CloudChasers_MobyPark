using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MobyPark.Services;
using MobyPark.Services.Interfaces;

namespace MobyPark.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BusinessController : BaseController
{
    private readonly IBusinessService _businessService;

    public BusinessController(UserService users, IBusinessService businessService) :base(users)
    {
        _businessService = businessService;
    }

    [HttpPost]
    [Authorize("CanManageBusinesses")]
    public async Task<IActionResult> CreateBusiness([FromBody]CreateBusinessDto business)
    {
        var result = await _businessService.CreateBusinessAsync(business);
        return FromServiceResult(result);
    }

    [HttpPatch("{id:long}")]
    [Authorize("CanManageBusinesses")]
    public async Task<IActionResult> PatchBusiness([FromRoute]long id, [FromBody]PatchBusinessDto businessPatch)
    {
        var result = await _businessService.PatchBusinessAsync(id, businessPatch);
        return FromServiceResult(result);
    }
    
    [HttpDelete("{id:long}")]
    [Authorize("CanManageBusinesses")]
    public async Task<IActionResult> DeleteBusinessById(long id)
    {
        var result = await _businessService.DeleteBusinessByIdAsync(id);
        return FromServiceResult(result);
    }
    
    [HttpGet("all")]
    public async Task<IActionResult> GetAllBusinesses()
    {
        var result = await _businessService.GetAllAsync();
        return FromServiceResult(result);
    }
    
    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetBusinessById(long id)
    {
        var result = await _businessService.GetBusinessByIdAsync(id);
        return FromServiceResult(result);
    }
    
    [HttpGet("{address}")]
    public async Task<IActionResult> GetBusinessByAddress(string address)
    {
        var result = await _businessService.GetBusinessByAddressAsync(address);
        return FromServiceResult(result);
    }
}