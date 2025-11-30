using System.ComponentModel.DataAnnotations.Schema;
using MobyPark.Models.Repositories.Interfaces;

namespace MobyPark.Models;

public class BusinessPassModel : IHasLongId
{
    public long Id { get; set; }
    // even vragen of dit erbij moet
    public string BusinessName { get; set; }
    public long ParkingLotId { get; set; }
    [ForeignKey(nameof(ParkingLotId))]
    public ParkingLotModel ParkingLot { get; set; }
    
    // Bij hoeveel parkeerplaatsen moeten de auto's kunnen parkeren?
    // Is de pass voor een enkele parkeersplaats of moeten auto's met een pass op alle parkeerplaatsen kunnen parkeren?
    // Wordt er 1 vast bedrag betaald per auto of moeten de parkeertijden ook opgeslagen worden?
    
}