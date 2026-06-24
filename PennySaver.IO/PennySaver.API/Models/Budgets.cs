namespace PennySaver.API.Models;

public class Budgets
{
    public int Id { get; set; }
    public int CategoryId { get; set; }
    public double TargetAmount { get; set; }
    public int Month { get; set; }
    public int Year { get; set; }
}