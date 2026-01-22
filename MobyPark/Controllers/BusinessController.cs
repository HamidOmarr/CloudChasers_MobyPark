using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using MobyPark.DTOs.Business;
using MobyPark.Services.Interfaces;

using Swashbuckle.AspNetCore.Annotations;

namespace MobyPark.Controllers;


[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Authorize("CanManageBusinesses")]
public class BusinessController : BaseController
{
    private readonly IBusinessService _businessService;

    public BusinessController(IUserService users, IBusinessService businessService) : base(users)
    {
        _businessService = businessService;
    }

    [HttpPost]
    [SwaggerOperation(Summary = "Creates a new business.")]
    [SwaggerResponse(200, "Business created successfully", typeof(ReadBusinessDto))]
    [SwaggerResponse(400, "Invalid IBAN provided")]
    [SwaggerResponse(409, "Address already in use")]
    public async Task<IActionResult> CreateBusiness([FromBody] CreateBusinessDto business)
    {
        var result = await _businessService.CreateBusinessAsync(business);
        return FromServiceResult(result);
    }

    [HttpPatch("{id:long}")]
    [SwaggerOperation(Summary = "Updates an existing business.")]
    [SwaggerResponse(200, "Business updated", typeof(ReadBusinessDto))]
    [SwaggerResponse(400, "Invalid IBAN provided")]
    [SwaggerResponse(404, "Business not found")]
    [SwaggerResponse(409, "New address already taken")]
    public async Task<IActionResult> PatchBusiness([FromRoute] long id, [FromBody] PatchBusinessDto businessPatch)
    {
        var result = await _businessService.PatchBusinessAsync(id, businessPatch);
        return FromServiceResult(result);
    }

    [HttpDelete("{id:long}")]
    [SwaggerOperation(Summary = "Deletes a business by its ID.")]
    [SwaggerResponse(200, "Business deleted", typeof(bool))]
    [SwaggerResponse(404, "Business not found")]
    public async Task<IActionResult> DeleteBusinessById(long id)
    {
        var result = await _businessService.DeleteBusinessByIdAsync(id);
        return FromServiceResult(result);
    }

    [HttpGet("all")]
    [SwaggerOperation(Summary = "Retrieves a list of all registered businesses.")]
    [AllowAnonymous]  // Ignore authorization for this endpoint
    [SwaggerResponse(200, "List retrieved", typeof(List<ReadBusinessDto>))]
    public async Task<IActionResult> GetAllBusinesses()
    {
        var result = await _businessService.GetAllAsync();
        return FromServiceResult(result);
    }

    [HttpGet("{id:long}")]
    [SwaggerOperation(Summary = "Retrieves a business by its ID.")]
    [SwaggerResponse(200, "Found business", typeof(ReadBusinessDto))]
    [SwaggerResponse(404, "Not found")]
    public async Task<IActionResult> GetBusinessById(long id)
    {
        var result = await _businessService.GetBusinessByIdAsync(id);
        return FromServiceResult(result);
    }

    [HttpGet("{address}")]
    [SwaggerOperation(Summary = "Retrieves a business by its address.")]
    [SwaggerResponse(200, "Found business", typeof(ReadBusinessDto))]
    [SwaggerResponse(404, "Not found")]
    public async Task<IActionResult> GetBusinessByAddress(string address)
    {
        var result = await _businessService.GetBusinessByAddressAsync(address);
        return FromServiceResult(result);
    }
}