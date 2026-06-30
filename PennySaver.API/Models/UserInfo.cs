namespace PennySaver.API.Models;

public class UserInfo
{
    [Key]
    public int Id { get; set; }
    [Required]
    public string FirstName { get; set; } = string.Empty;
    [Required]
    public string LastName { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? ZipCode { get; set; }

    [Required]
    public int UserId { get; set; }
    [ForeignKey("UserId")]
    public User User { get; set; } = null!;
}