namespace PennySaver.API.Models;

public sealed class PlaidSettings
{
    public const string SectionName = "Plaid";
    
    [Required]
    public string ClientId { get; set; } = string.Empty;
    [Required]
    public string Secret { get; set; } = string.Empty;
    [Required]
    public string Environment { get; set; } = "sandbox";

    public string[] Products {get;set;} = ["transactions"];
    public string[] CountryCodes {get;set;} = ["US"];

    public bool Enabled { get; set; } = false;
}