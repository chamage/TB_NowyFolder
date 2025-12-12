using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

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

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal PricePerNight { get; set; }

    // Navigation properties
    [ForeignKey(nameof(ReservationID))]
    public virtual Reservation? Reservation { get; set; }

    [ForeignKey(nameof(RoomID))]
    public virtual Room? Room { get; set; }
}