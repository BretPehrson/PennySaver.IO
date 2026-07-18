namespace PennySaver.API.Models;

public class Account
{
    public enum AccountType
    {
        Checking,
        Savings,
        CreditCard,
        Investment,
        Loan
    }

    public enum AccountSyncStatus
    {
        Healthy = 0,
        RequiresAttention = 1
    }

    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string AccountName { get; set; } = string.Empty;

    [MaxLength(100)]
    public string Institution { get; set; } = string.Empty;

    [Required]
    public AccountType Type { get; set; } = AccountType.Checking;

    [Required(ErrorMessage = "Balance is required.")]
    [Column(TypeName = "decimal(18,2)")]
    [RegularExpression(@"^\d+(\.\d{1,2})?$", ErrorMessage = "Balance can only have 2 decimal places.")]
    public decimal Balance { get; set; } = 0.00m;

    public DateTime? DeletedAt { get; set; } = null;

    // Foreign Key Relationships
    [Required]
    public int UserId { get; set; }
    
    [ForeignKey("UserId")]
    public User User { get; set; } = null!;


    // Plaid properties
    public bool IsAutomated { get; set; } = false;
    [MaxLength(200)]
    public string? PlaidAccessToken { get; set; }
    [MaxLength(200)]
    public string? PlaidItemId { get; set; }
    [MaxLength(200)]
    public string? PlaidAccountId { get; set; }
    public AccountSyncStatus SyncStatus { get; set; } = AccountSyncStatus.Healthy;
    public DateTime LastSynced { get; set; } = DateTime.UtcNow;
}