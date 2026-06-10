using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace TB_NowyFolder.Models;

[Table("ReservationRooms")]
public class ReservationRoom
{
    [Key, Column(Order = 0)]
    [Required]
    public int ReservationID { get; set; }

    [Key, Column(Order = 1)]
    [Required]
    public int RoomID { get; set; }

    // Cena za noc jest kopiowana z pokoju w momencie tworzenia rezerwacji.
    // Dzięki temu późniejsza zmiana cennika nie wpływa na istniejące rezerwacje.
    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal PricePerNight { get; set; }

    // JsonIgnore zapobiega cyklicznej serializacji (Reservation -> ReservationRoom -> Reservation...).
    [JsonIgnore]
    [ForeignKey(nameof(ReservationID))]
    public virtual Reservation? Reservation { get; set; }

    [ForeignKey(nameof(RoomID))]
    public virtual Room? Room { get; set; }
}