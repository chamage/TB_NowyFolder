using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace TB_NowyFolder.Models;

[Table("ReservationServices")]
public class ReservationService
{
    [Key, Column(Order = 0)]
    [Required]
    public int ReservationID { get; set; }

    [Key, Column(Order = 1)]
    [Required]
    public int ServiceID { get; set; }

    [Required]
    public int Quantity { get; set; }

    // ServiceDate jest częścią klucza złożonego - ta sama usługa może być zamawiana wielokrotnie w różnych dniach.
    [Required]
    public DateOnly ServiceDate { get; set; }

    // JsonIgnore zapobiega cyklicznej serializacji (Reservation -> ReservationService -> Reservation...).
    [JsonIgnore]
    [ForeignKey(nameof(ReservationID))]
    public virtual Reservation? Reservation { get; set; }

    [ForeignKey(nameof(ServiceID))]
    public virtual Service? Service { get; set; }
}