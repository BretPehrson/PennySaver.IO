using System.ComponentModel.DataAnnotations.Schema;

namespace PennySaver.API.Models;

public class Transaction
{
    public int Id { get; set; }
    public int AccountId { get; set; }
    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }
    public DateTime Date { get; set; }
    public string Description { get; set; } = string.Empty;
    public int CategoryId { get; set; }
}