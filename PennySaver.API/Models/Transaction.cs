namespace PennySaver.API.Models;

public enum TransactionStatus
{
    Pending,
    Completed,
    Voided
}

public class Transaction
{
    [Key]
    public int Id { get; set; }
    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }
    [Required]
    public DateTime Date { get; set; }
    [MaxLength(1000)]
    public string Description { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    public TransactionStatus Status { get; set; } = TransactionStatus.Pending;

    [Required]
    public int AccountId { get; set; }
    [ForeignKey("AccountId")]
    public Account? Account { get; set; } = null!;
    
    [Required]
    public int CategoryId { get; set; }
    [ForeignKey("CategoryId")]
    public Category Category { get; set; } = null!;
}