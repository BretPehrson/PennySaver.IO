namespace PennySaver.API.Dtos;

public class BudgetCreateDto
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Range(0, double.MaxValue, ErrorMessage = "Target amount must be a non-negative value.")]
    public decimal TargetAmount { get; set; }
    public int Month { get; set; }
    public int Year { get; set; }

    [Required]
    public DateTime StartDate { get; set; }

    [Required]
    public DateTime? EndDate { get; set; } = null;
    [Required]
    public int CategoryId { get; set; }
    public bool IsActive { get; set; } = true;
}

public class BudgetResponseDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal TargetAmount { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; } = null;
    public int CategoryId { get; set; }
    public int Month { get; set; }
    public int Year { get; set; }
}