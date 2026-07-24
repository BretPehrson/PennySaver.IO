namespace PennySaver.API.Models;

public class Category
{
    [Key]
    public int Id { get; set; }
    
    [Required(ErrorMessage = "Name is required"), MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
    public string Description { get; set; } = string.Empty;

    [MaxLength(7, ErrorMessage = "Color code cannot exceed 7 characters")]
    public string ColorCode { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    [Required]
    public int UserId { get; set; }
    [ForeignKey("UserId")]
    public User User { get; set; } = null!;
}