using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TB_NowyFolder.Models;

[Table("ReservationRooms")]
public class ReservationRoom
{
    [Required]
    public int ReservationID { get; set; }

    [Required]
    public int RoomID { get; set; }

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal PricePerNight { get; set; }

    // Navigation properties
    [ForeignKey(nameof(ReservationID))]
    public Reservation Reservation { get; set; } = null!;

    [ForeignKey(nameof(RoomID))]
    public Room Room { get; set; } = null!;
}
