using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace TB_NowyFolder.Models;

[Table("Guests")]
public class Guest
{
    [Key]
    public int GuestID { get; set; }

    [Required]
    [MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string LastName { get; set; } = string.Empty;

    [Required]
    [MaxLength(255)]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? Phone { get; set; }

    [MaxLength(20)]
    public string? TaxID { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }

    // Navigation property
    [JsonIgnore]
    public virtual ICollection<Reservation>? Reservations { get; set; }
}