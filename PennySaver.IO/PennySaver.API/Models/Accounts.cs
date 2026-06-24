namespace PennySaver.API.Models;

public class Accounts
{
    public enum AccountType
    {
        Inflow,
        Outflow
    }

    public AccountType Type { get; set; }
    public int Id { get; set; }
    public string AccountId { get; set; } = string.Empty;
    public double Amount { get; set; }
    public DateTime Date { get; set; }
    public string Description { get; set; } = string.Empty;
    public int CategoryId { get; set; }
}