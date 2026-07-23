namespace PennySaver.API.Models;

public class Budget
{
    [Key]
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal TargetAmount { get; set; }
    public DateTime StartDate { get; set; } = DateTime.UtcNow;
    public DateTime? EndDate { get; set; } = null;
    public int Month { get; set; }
    public int Year { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DeletedAt { get; set; } = null;

    [Required]
    public int CategoryId { get; set; }
    [ForeignKey("CategoryId")]
    public Category Category { get; set; } = null!;
    
    [Required]
    public int UserId { get; set; }
    [ForeignKey("UserId")]
    public User User { get; set; } = null!;
}