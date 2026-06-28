namespace PennySaver.API.Models;

public enum TransactionStatus
{
    Pending,
    Completed,
    Voided
}

public class Transaction
{
    public int Id { get; set; }
    [Required]
    public double Amount { get; set; }
    [Required]
    public DateTime Date { get; set; }
    public string Description { get; set; } = string.Empty;

    public TransactionStatus Status { get; set; } = TransactionStatus.Pending;

    [Required]
    public int AccountId { get; set; }
    public Accounts Account { get; set; } = null!;
    
    [Required]
    public int CategoryId { get; set; }
    public Category Category { get; set; } = null!;
}