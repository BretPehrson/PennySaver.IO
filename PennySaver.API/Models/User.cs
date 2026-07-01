namespace PennySaver.API.Models;

public class User
{
    public int UserId { get; set; }

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MinLength(6)]
    public string Password { get; set; } = string.Empty;
    
    public DateTime CreatedAt { get; set; }

    public List<RefreshToken> RefreshTokens { get; set; } = new();

    public UserInfo? Profile { get; set; }
}