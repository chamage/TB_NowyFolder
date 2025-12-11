using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TB_NowyFolder.Models;

[Table("ReservationServices")]
public class ReservationService
{
    [Required]
    public int ReservationID { get; set; }

    [Required]
    public int ServiceID { get; set; }

    [Required]
    public int Quantity { get; set; }

    [Required]
    public DateOnly ServiceDate { get; set; }

    // Navigation properties
    [ForeignKey(nameof(ReservationID))]
    public Reservation Reservation { get; set; } = null!;

    [ForeignKey(nameof(ServiceID))]
    public Service Service { get; set; } = null!;
}
