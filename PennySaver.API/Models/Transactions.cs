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
    public double Amount { get; set; }
    public DateTime Date { get; set; }
    public string Description { get; set; } = string.Empty;

    public TransactionStatus Status { get; set; } = TransactionStatus.Pending;

    public int AccountId { get; set; }
    public Accounts Account { get; set; } = null!;
    
    public int CategoryId { get; set; }
    public Category Category { get; set; } = null!;
}