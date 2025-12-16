using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MobyPark.Models;

public class InvoiceModel
{
    [Key]
    public long Id { get; set; }
    public string LicensePlateId { get; set; } = string.Empty;
    [ForeignKey(nameof(LicensePlateId))]
    public LicensePlateModel LicensePlate { get; set; } = null!;
    public long ParkingSessionId { get; set; }
    [ForeignKey(nameof(ParkingSessionId))]
    public ParkingSessionModel ParkingSession { get; set; } = null!;

    [Required]
    public DateTimeOffset Started { get; set; }

    [Required]
    public DateTimeOffset Stopped { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? Cost { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public List<string> InvoiceSummary { get; set; } = new List<string>();
}