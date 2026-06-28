namespace PennySaver.API.Models;

public class Accounts
{
    public enum AccountType
    {
        Inflow,
        Outflow
    }

    [Key]
    public int Id { get; set; }
    public AccountType Type { get; set; }
    public string AccountName { get; set; } = string.Empty;
    
    public int UserId { get; set; }
    public User User { get; set; } = null!;
}