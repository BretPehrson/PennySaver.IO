using Going.Plaid;
using Going.Plaid.Entity;
using Going.Plaid.Item;
using Going.Plaid.Link;

namespace PennySaver.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class PlaidController(IDbContextFactory<PennySaverDbContext> contextFactory, IAccountSyncCoordinator SyncCoordinator, PlaidClient plaidClient) : ControllerBase
{
    private readonly IAccountSyncCoordinator _syncCoordinator = SyncCoordinator;
    private readonly IDbContextFactory<PennySaverDbContext> _contextFactory = contextFactory;

    private readonly PlaidClient _plaidClient = plaidClient;

    private int GetCurrentUserId() =>
        int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var userId) 
        ? userId 
        : throw new UnauthorizedAccessException();

    [HttpPost("create-link-token")]
    public async Task<IActionResult> CreateLinkToken()
    {
        try
        {
            var userId = GetCurrentUserId();

            var request = new LinkTokenCreateRequest
            {
                ClientName = "PennySaver.IO",
                Language = Language.English,
                CountryCodes = new[] { CountryCode.Us },
                User = new LinkTokenCreateRequestUser
                {
                    ClientUserId = userId.ToString()
                },
                Products = new[] { Products.Balance }
            };

            var response = await _plaidClient.LinkTokenCreateAsync(request);
            
            if (!response.IsSuccessStatusCode)
            {
                Console.Error.WriteLine($"Plaid API error: {response.Error?.ErrorMessage}");
                return StatusCode(500, "An error occurred while communicating with the Plaid API. Please try again later.");
            }

            return Ok(new { link_token = response.LinkToken });
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error creating Plaid link token: {ex.Message}");
            return StatusCode(500, "An error occurred while creating the Plaid link token. Please try again later.");
        }
    }

    [HttpPost("exchange-public-token")]
    public async Task<IActionResult> ExchangePublicToken([FromBody] PlaidExchangeRequestDto dto)
    {
        if (string.IsNullOrEmpty(dto.PublicToken)) return BadRequest("Public token required.");

        int userId = GetCurrentUserId();

        // 1. Ask Plaid to trade the short-lived user token for a permanent system key card
        var exchangeResponse = await _plaidClient.ItemPublicTokenExchangeAsync(
            new ItemPublicTokenExchangeRequest { PublicToken = dto.PublicToken }
        );

        if (!exchangeResponse.IsSuccessStatusCode)
        {
            return BadRequest($"Token translation mapping failed: {exchangeResponse.Error?.ErrorMessage}");
        }

        string permanentAccessToken = exchangeResponse.AccessToken;
        string plaidItemId = exchangeResponse.ItemId;

        // 2. Query Plaid immediately using that new token to see what sub-accounts exist 
        // (e.g., separating a checking account and a savings account under one login)
        var accountsResponse = await _plaidClient.AccountsBalanceGetAsync(
            new Going.Plaid.Accounts.AccountsBalanceGetRequest { AccessToken = permanentAccessToken }
        );

        if (!accountsResponse.IsSuccessStatusCode)
        {
            return BadRequest("Could not fetch information for linked accounts.");
        }

        using var context = await _contextFactory.CreateDbContextAsync();

        // 3. Map each discovered sub-account directly into your PennySaver account database
        foreach (var plaidAcc in accountsResponse.Accounts)
        {
            var newAccount = new PennySaver.API.Models.Account
            {
                UserId = userId,
                // Use the user's institution name + the type (e.g., "Chase Checking")
                AccountName = $"{dto.InstitutionName} {plaidAcc.Name}",
                Institution = dto.InstitutionName,
                Balance = plaidAcc.Balances.Current ?? 0.00m,
                IsAutomated = true,
                SyncStatus = AccountSyncStatus.Healthy,
                PlaidAccessToken = permanentAccessToken,
                PlaidItemId = plaidItemId,
                PlaidAccountId = plaidAcc.AccountId,
                
                // Direct conversion mapping helper for your internal AccountType Enum
                Type = MapPlaidTypeToInternal(plaidAcc.Type) 
            };

            context.Account.Add(newAccount);
        }

        await context.SaveChangesAsync();
        return Ok(new { success = true, accountsCount = accountsResponse.Accounts.Count });
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> RefreshBalances()
    {
        int userId = GetCurrentUserId();
        await _syncCoordinator.RefreshUserBalancesAsync(userId);
        return Ok(new { message = "All balances refreshed via Plaid." });
    }

    [HttpPost("webhook")]
    public async Task<IActionResult> HandlePlaidWebhook()
    {
        using var reader = new StreamReader(Request.Body);
        var body = await reader.ReadToEndAsync();

        try
        {
            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;

            string? webhookType = root.TryGetProperty("webhook_type", out var typeProp) ? typeProp.GetString() : null;
            string? webhookCode = root.TryGetProperty("webhook_code", out var codeProp) ? codeProp.GetString() : null;
            string? itemId = root.TryGetProperty("item_id", out var itemProp) ? itemProp.GetString() : null;

            if (string.IsNullOrEmpty(webhookType) || string.IsNullOrEmpty(webhookCode))
            {
                return BadRequest("Invalid webhook envelope structure.");
            }

            switch (webhookType)
            {
                case "ITEM":
                    if (webhookCode == "ERROR" && !string.IsNullOrEmpty(itemId))
                    {
                        await HandleWebhookItemErrorAsync(itemId);
                    }
                    break;

                case "TRANSACTIONS":
                    if ((webhookCode == "DEFAULT_UPDATE" || webhookCode == "SYNC_UPDATES_AVAILABLE") && !string.IsNullOrEmpty(itemId))
                    {
                        // await _syncCoordinator.RefreshBalancesByItemIdAsync(itemId);
                    }
                    break;
        }

        return Ok();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error processing Plaid webhook: {ex.Message}");
            return StatusCode(500, "An error occurred while processing the Plaid webhook. Please try again later.");
        }
    }

    private static PennySaver.API.Models.Account.AccountType MapPlaidTypeToInternal(Going.Plaid.Entity.AccountType plaidType)
    {
        return plaidType switch
        {
            Going.Plaid.Entity.AccountType.Depository => PennySaver.API.Models.Account.AccountType.Checking, // map savings/checking accordingly
            Going.Plaid.Entity.AccountType.Credit => PennySaver.API.Models.Account.AccountType.CreditCard,
            Going.Plaid.Entity.AccountType.Investment => PennySaver.API.Models.Account.AccountType.Investment,
            Going.Plaid.Entity.AccountType.Loan => PennySaver.API.Models.Account.AccountType.Loan,
            _ => PennySaver.API.Models.Account.AccountType.Checking
        };
    }

    private async Task HandleWebhookItemErrorAsync(string itemId)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var accountsToUpdate = context.Account.Where(a => a.PlaidItemId == itemId).ToList();

        foreach (var account in accountsToUpdate)
        {
            account.SyncStatus = AccountSyncStatus.RequiresAttention;
        }

        await context.SaveChangesAsync();
    }

    public class PlaidExchangeRequestDto
    {
        [Required]
        public string PublicToken { get; set; } = string.Empty;
        [Required]
        public string PlaidAccountId { get; set; } = string.Empty;
        [Required]
        public string InstitutionName { get; set; } = string.Empty;
        [Required]
        public string? InstitutionId { get; set; }
    }
}