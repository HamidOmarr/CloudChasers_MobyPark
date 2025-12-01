using System.ComponentModel.DataAnnotations.Schema;
using MobyPark.Models.Repositories.Interfaces;

namespace MobyPark.Models;

public class BusinessParkingRegistrationModel : IHasLongId
{
    public long Id { get; set; }
    // even vragen of dit erbij moet
    // ja
    public string BusinessName { get; set; }
    public long ParkingLotId { get; set; }
    [ForeignKey(nameof(ParkingLotId))]
    public ParkingLotModel ParkingLot { get; set; }
    
    // voor businesses:
    // Bij hoeveel parkeerplaatsen moeten de auto's kunnen parkeren?
    // kan bij meerdere
    // Is de pass voor een enkele parkeersplaats of moeten auto's met een pass op alle parkeerplaatsen kunnen parkeren?
    // Wordt er 1 vast bedrag betaald per auto of moeten de parkeertijden ook opgeslagen worden?
    // tarieven worden gewoon berekend per sessie.
    
    /*
     * Voor hotels: hoe moeten de hoteleigenaren worden geverifieerd wanneer zij een account maken?
     * user wordt gemaakt voor mobypark
     * Maakt de admin een account voor hen?
     * Kan een hoteleigenaar alleen voor hun eigen parkeerplaats een hotelpass aanmaken?
     * ja
     */
}