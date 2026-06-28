namespace PennySaver.API.Models;

public class Budgets
{
    [Key]
    public int Id { get; set; }
    [Required]
    public double TargetAmount { get; set; }
    public int Month { get; set; }
    public int Year { get; set; }

    public int CategoryId { get; set; }
    public Category Category { get; set; } = null!;
    
    [Required]
    public int UserId { get; set; }
    public User User { get; set; } = null!;
}