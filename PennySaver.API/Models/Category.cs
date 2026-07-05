namespace PennySaver.API.Models;

public class Category
{
    [Key]
    public int Id { get; set; }
    [Required, MaxLength(100)]
    public string CategoryName { get; set; } = string.Empty;
    [MaxLength(7)]
    public string ColorCode { get; set; } = string.Empty;
    
    public int UserId { get; set; }
    public User User { get; set; } = null!;
}