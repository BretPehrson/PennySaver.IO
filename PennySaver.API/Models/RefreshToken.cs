using System.ComponentModel.DataAnnotations;

namespace PennySaver.API.Models;

public class RefreshToken
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Token { get; set; } = string.Empty;

    public DateTime Expires { get; set; }

    public DateTime Created { get; set; } = DateTime.UtcNow;

    public bool IsExpired => DateTime.UtcNow >= Expires;
    public bool IsActive => !IsExpired;

    public int UserId { get; set; }
    public User User { get; set; } = null!;
}