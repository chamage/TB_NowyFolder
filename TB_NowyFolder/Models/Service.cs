using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TB_NowyFolder.Models;

[Table("Services")]
public class Service
{
    [Key]
    public int ServiceID { get; set; }

    [Required]
    [MaxLength(100)]
    public string ServiceName { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal UnitPrice { get; set; }

    [Required]
    [MaxLength(50)]
    public string Availability { get; set; } = "Available";

    // Navigation property
    public ICollection<ReservationService> ReservationServices { get; set; } = new List<ReservationService>();
}
