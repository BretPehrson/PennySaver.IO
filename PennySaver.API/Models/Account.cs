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

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal Balance { get; set; } = 0.00m;

    // Foreign Key Relationships
    [Required]
    public int UserId { get; set; }
    
    [ForeignKey("UserId")]
    public User User { get; set; } = null!;


    // Plaid properties
    public bool IsAutomated { get; set; } = false;
    public string? PlaidAccessToken { get; set; }
    public string? PlaidAccountId { get; set; }
    public AccountSyncStatus SyncStatus { get; set; } = AccountSyncStatus.Healthy;

    public AccountResponseDto ToDto()
    {
        return new AccountResponseDto
        {
            AccountName = this.AccountName,
            Institution = this.Institution,
            Type = this.Type,
            Balance = this.Balance,
            IsAutomated = this.IsAutomated,
            SyncStatus = this.SyncStatus
        };
    }
}

public class AccountCreateDto
{
    [Required]
    [MaxLength(100)]
    public string AccountName { get; set; } = string.Empty;

    [MaxLength(100)]
    public string Institution { get; set; } = string.Empty;

    [Required]
    public Account.AccountType Type { get; set; } = Account.AccountType.Checking;

    [Range(0, double.MaxValue, ErrorMessage = "Balance must be a non-negative value.")]
    public decimal Balance { get; set; } = 0.00m;

    public bool IsAutomated { get; set; } = false;
    public string? PlaidAccessToken { get; set; }
}

public class AccountResponseDto
{
    [Required]
    [MaxLength(100)]
    public string AccountName { get; set; } = string.Empty;

    [MaxLength(100)]
    public string Institution { get; set; } = string.Empty;

    [Required]
    public Account.AccountType Type { get; set; } = Account.AccountType.Checking;

    [Range(0, double.MaxValue, ErrorMessage = "Balance must be a non-negative value.")]
    public decimal Balance { get; set; } = 0.00m;

    public bool IsAutomated { get; set; } = false;
    public string? PlaidAccountId { get; set; }
    public AccountSyncStatus SyncStatus { get; set; } = AccountSyncStatus.Healthy;
}