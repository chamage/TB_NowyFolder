using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TB_NowyFolder.Models;

[Table("RoomTypes")]
public class RoomType
{
    [Key]
    public int RoomTypeID { get; set; }

    [Required]
    [MaxLength(100)]
    public string TypeName { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    [MaxLength(50)]
    public string? Standard { get; set; }

    // Navigation property
    public ICollection<Room> Rooms { get; set; } = new List<Room>();
}
