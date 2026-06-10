using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TB_NowyFolder.Models;

[Table("Users")]
public class User
{
    [Key]
    public int UserID { get; set; }

    [Required]
    [MaxLength(100)]
    public string Username { get; set; } = string.Empty;

    // Hasło jako hash PBKDF2 - oryginalne hasło nigdy nie jest zapisywane.
    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    // Rola decyduje o dostępie do endpointów - wartości zdefiniowane w ApplicationRoles.
    [Required]
    [MaxLength(50)]
    public string Role { get; set; } = string.Empty;

    // GuestID tylko dla klientów - trafia do tokenu JWT i służy do weryfikacji właściciela rezerwacji.
    public int? GuestID { get; set; }

    [ForeignKey("GuestID")]
    public virtual Guest? Guest { get; set; }
}
