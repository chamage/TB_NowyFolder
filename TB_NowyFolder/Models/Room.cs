using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace TB_NowyFolder.Models;

[Table("Rooms")]
public class Room
{
    [Key]
    public int RoomID { get; set; }

    [Required]
    public int RoomTypeID { get; set; }

    [Required]
    [MaxLength(20)]
    public string RoomNumber { get; set; } = string.Empty;

    [Required]
    public int Capacity { get; set; }

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal PricePerNight { get; set; }

    [Required]
    [MaxLength(50)]
    public string Status { get; set; } = "Available";

    // Navigation properties
    [ForeignKey(nameof(RoomTypeID))]
    public virtual RoomType? RoomType { get; set; }

    [JsonIgnore]
    public virtual ICollection<ReservationRoom>? ReservationRooms { get; set; }
}