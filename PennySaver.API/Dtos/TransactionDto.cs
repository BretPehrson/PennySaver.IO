namespace PennySaver.API.Dtos;

public class TransactionResponseDto
{
    public int Id { get; set; }
    public decimal Amount { get; set; }
    public string Description { get; set; } = string.Empty;
    public TransactionStatus Status { get; set; }
    public int AccountId { get; set; }
    public int CategoryId { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class TransactionCreateDto
{
    [Required]
    public decimal Amount { get; set; }

    [Required]
    [MaxLength(250)]
    public string Description { get; set; } = string.Empty;

    [Required]
    public TransactionStatus Status { get; set; } = TransactionStatus.Pending;

    [Required]
    public int CategoryId { get; set; }
    public DateTime CreatedAt { get; set; }

    [Required]
    public int AccountId { get; set; }
    public Account? Account { get; set; } = null!;
}