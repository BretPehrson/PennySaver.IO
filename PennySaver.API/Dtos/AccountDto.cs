namespace PennySaver.API.Dtos;

public class AccountCreateDto
{
    [Required]
    [MaxLength(100)]
    public string AccountName { get; set; } = string.Empty;

    [MaxLength(100)]
    public string Institution { get; set; } = string.Empty;

    [Required]
    public Account.AccountType Type { get; set; } = Account.AccountType.Checking;

    [Required(ErrorMessage = "Balance is required.")]
    [Range(0, double.MaxValue, ErrorMessage = "Balance must be a non-negative value.")]
    [RegularExpression(@"^\d+(\.\d{1,2})?$", ErrorMessage = "Balance can only have 2 decimal places.")]
    public decimal Balance { get; set; } = 0.00m;

    public bool IsAutomated { get; set; } = false;
    public string? PlaidAccessToken { get; set; }
    public string? PlaidAccountId { get; set; }
}

public class AccountResponseDto
{
    public int Id { get; set; }
    public string AccountName { get; set; } = string.Empty;

    public string Institution { get; set; } = string.Empty;

    public Account.AccountType Type { get; set; } = Account.AccountType.Checking;

    public decimal Balance { get; set; } = 0.00m;

    public bool IsAutomated { get; set; } = false;
    public string? PlaidAccountId { get; set; }
    public AccountSyncStatus SyncStatus { get; set; } = AccountSyncStatus.Healthy;
}