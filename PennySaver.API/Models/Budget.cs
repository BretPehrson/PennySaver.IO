namespace PennySaver.API.Models;

public class Budget
{
    [Key]
    public int Id { get; set; }
    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal TargetAmount { get; set; }
    public int Month { get; set; }
    public int Year { get; set; }

    public int CategoryId { get; set; }
    public Category Category { get; set; } = null!;
    
    [Required]
    public int UserId { get; set; }
    public User User { get; set; } = null!;
}