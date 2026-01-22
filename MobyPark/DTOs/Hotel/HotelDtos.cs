using System.ComponentModel.DataAnnotations;

using Swashbuckle.AspNetCore.Annotations;

namespace MobyPark.DTOs.Hotel;

[SwaggerSchema(Description = "Data for creating a new Hotel.")]
public class CreateHotelDto
{
    [Required]
    [SwaggerSchema("The name of the hotel")]
    public string Name { get; set; } = string.Empty;

    [Required]
    [SwaggerSchema("Physical address")]
    public string Address { get; set; } = string.Empty;

    [Required]
    [SwaggerSchema("Bank account number (IBAN)")]
    public string IBAN { get; set; } = string.Empty;

    [Required]
    [SwaggerSchema("The ID of the parking lot owned by this hotel")]
    public long HotelParkingLotId { get; set; }
}

[SwaggerSchema(Description = "Data for updating a Hotel.")]
public class PatchHotelDto
{
    [Required]
    [SwaggerSchema("The unique identifier of the hotel")]
    public long Id { get; set; }

    [SwaggerSchema("The new name (optional)")]
    public string? Name { get; set; }

    [SwaggerSchema("The new address (optional)")]
    public string? Address { get; set; }

    [SwaggerSchema("The new IBAN (optional)")]
    public string? IBAN { get; set; }

    [SwaggerSchema("The new parking lot ID (optional)")]
    public long? HotelParkingLotId { get; set; }
}

[SwaggerSchema(Description = "Details of a Hotel.")]
public class ReadHotelDto
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public long HotelParkingLotId { get; set; }
}