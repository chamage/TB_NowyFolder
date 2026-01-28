using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace TB_NowyFolder.Models;

[Table("Reservations")]
public class Reservation
{
    [Key]
    public int ReservationID { get; set; }

    [Required]
    public int GuestID { get; set; }

    [Required]
    public DateTime ReservationDate { get; set; } = DateTime.Now;

    [Required]
    public DateOnly CheckInDate { get; set; }

    [Required]
    public DateOnly CheckOutDate { get; set; }

    [Required]
    public int NumberOfGuests { get; set; }

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalPrice { get; set; }

    [Required]
    [MaxLength(50)]
    public string ReservationStatus { get; set; } = "Confirmed";

    // Navigation properties
    [ForeignKey(nameof(GuestID))]
    public virtual Guest? Guest { get; set; }

    public virtual ICollection<ReservationRoom>? ReservationRooms { get; set; }

    public virtual ICollection<ReservationService>? ReservationServices { get; set; }
}