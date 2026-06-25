namespace PennySaver.API.Models;

public class Category
{
    public int Id { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string ColorCode { get; set; } = string.Empty;
    
    public int UserId { get; set; }
    public User User { get; set; } = null!;
}