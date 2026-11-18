using HappyCity.Domain;
using HappyCity.Application;
namespace HappyCity.Infrastructure;
public sealed class DemoDiscoveryRepository : IDiscoveryRepository {
 private static readonly IReadOnlyList<DiscoveryItem> Items = Array.AsReadOnly(new DiscoveryItem[] {
 new("Een wandeling aan het water", "Inspiratie voor een gratis buitenactiviteit.", "activiteiten", "Almere Stad", "☀"),
 new("Een familiedag plannen", "Ontdek ideeën voor een dag met kinderen.", "activiteiten", "Almere Buiten", "♧"),
 new("Lokaal lunchen", "Voorbeeld van een categorie voor lunch en restaurants.", "eten", "Almere Haven", "♨"),
 new("Winkelaanbiedingen", "Hier komen actuele, gecontroleerde acties.", "besparen", "Almere Stad", "€"),
 new("Huis en tuin", "Vind straks lokale vakmensen voor onderhoud.", "diensten", "Almere Poort", "⌂"),
 new("Ontdek je buurt", "Inspiratie voor lokale winkels en voorzieningen.", "winkelen", "Muziekwijk", "◇") });
 public IReadOnlyList<DiscoveryItem> GetAll() => Items;
}
