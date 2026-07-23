namespace PennySaver.API.Models;

public class UserInfo
{
    [Key]
    public int Id { get; set; }
    [Required]
    [MaxLength(50)]
    public string FirstName { get; set; } = string.Empty;
    [Required]
    [MaxLength(50)]
    public string LastName { get; set; } = string.Empty;
    [MaxLength(20)]
    public string? PhoneNumber { get; set; }
    [MaxLength(100)]
    public string? Address { get; set; }
    [MaxLength(50)]
    public string? City { get; set; }
    [MaxLength(50)]
    public string? State { get; set; }
    [MaxLength(20)]
    public string? ZipCode { get; set; }

    [Required]
    public int UserId { get; set; }
    [ForeignKey("UserId")]
    public User User { get; set; } = null!;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}