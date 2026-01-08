using System.ComponentModel.DataAnnotations.Schema;

using MobyPark.Models.Repositories.Interfaces;

namespace MobyPark.Models;

public class BusinessParkingRegistrationModel : IHasLongId
{
    public long Id { get; set; }
    public long BusinessId { get; set; }

    [ForeignKey(nameof(BusinessId))]
    public BusinessModel Business { get; set; } = null!;

    public string LicensePlateNumber { get; set; } = string.Empty;
    public bool Active { get; set; } = true;
    public DateTimeOffset LastSinceActive { get; set; } = DateTimeOffset.Now;

    // mijn idee: sessie haalt op of de licenseplate een businessparkingregistrationmodel heeft
    /* sessie haalt op of de licenseplate een businessparkingregistrationmodel heeft
     * De sessie slagboom gaat open, sessie start tijd wordt opgeslagen, opgeslagen als business sessie
     * De business sessie kan een nieuwe table zijn, óf het kan de payment enum businesspayment meegeven
     * De auto rijdt uit, kenteken wordt weer gescand. Sessie haalt weer op dat het kenteken een businessparkingregistrationmodel heeft
     * slagboom gaat open, auto kan uitrijden
     *
     * method toevoegen die de sessies ophaald op basis van de license plates en de business id waar de enum businesspayment is
     */

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
     *
     * Stappenplan voor hotels:
     * HotelModel
     * User koppelen die wijzigen mag doen voor het hotel
     * Admin moet account aan kunnen maken voor hotels
     * Ergens moet opgeslagen worden welke parkinglotId bij welk hotel hoort
     * en dan kan een ingelogd hotel alleen voor hun eigen parkinglotId een hotelpass toevoegen.
     * admin kan voor elke parkinglot een pass toevoegen
     *
     */

    /*
     * aan de usermodel moet een id meegegeven worden die alleen de admin kan zetten.
     * die id BusinessRepresentativeFor is een foreign key naar een businessmodel id
     * Die user kan dan passes toevoegen voor dat bedrijf/registrations doen
     */

}