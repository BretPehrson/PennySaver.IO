namespace PennySaver.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class AccountController(IDbContextFactory<PennySaverDbContext> dbContext) : ControllerBase
{
    private readonly IDbContextFactory<PennySaverDbContext> _context = dbContext;

    private int GetCurrentUserId() =>
        int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var userId) 
        ? userId 
        : throw new UnauthorizedAccessException();

    private static AccountResponseDto MapToDto(Account account) => new()
    {
        Id = account.Id,
        AccountName = account.AccountName,
        Institution = account.Institution,
        Type = account.Type,
        Balance = account.Balance,
        IsAutomated = account.IsAutomated,
        PlaidAccountId = account.PlaidAccountId,
        SyncStatus = account.SyncStatus
    };

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var userId = GetCurrentUserId();
        using var context = await _context.CreateDbContextAsync();
        var userAccounts = context.Account.Where(a => a.UserId == userId).Select(a => MapToDto(a)).ToList();
        return Ok(userAccounts);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        int userId = GetCurrentUserId();
        using var context = await _context.CreateDbContextAsync();
        var account = context.Account.FirstOrDefault(a => a.Id == id && a.UserId == userId);
        if (account == null) return NotFound();
        return Ok(MapToDto(account));
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> RefreshBalance([FromServices] IAccountSyncCoordinator SyncCoordinator)
    {
        int userId = GetCurrentUserId();
        try
        {
            await SyncCoordinator.RefreshUserBalancesAsync(userId);
            return Ok("Automated account balances refreshed successfully.");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error refreshing balances: {ex.Message}");
            return StatusCode(500, "An error occurred while refreshing account balances. Please try again later.");
        }
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] AccountCreateDto dto, [FromServices] IBankSyncService syncService, [FromServices] IWebHostEnvironment? env = null, [FromServices] IConfiguration? config = null)
    {
        if (dto == null) return BadRequest("Account data is required.");
        if (dto.AccountName == null || dto.AccountName.Trim() == "") return BadRequest("The AccountName field is required.");
        if (!ModelState.IsValid) return BadRequest(ModelState);
        if (dto.Balance < 0) return BadRequest("Balance cannot be negative.");
        if (dto.Institution != null && dto.Institution.Length > 100) return BadRequest("Institution name cannot exceed 100 characters.");

        int currentUserId = GetCurrentUserId();

        //Overwrite any provided UserId with the logged-in user's ID to enforce ownership
        var newAccount = new PennySaver.API.Models.Account
        {
            UserId = currentUserId,
            AccountName = dto.AccountName,
            Institution = dto.Institution!,
            Type = dto.Type,
            IsAutomated = dto.IsAutomated,
            Balance = dto.Balance
        };

        if (dto.IsAutomated)
        {
            // In development mode, allow the server to generate a unique mock token
            if (string.IsNullOrWhiteSpace(dto.PlaidAccessToken))
            {
                var allowServerMock = config?.GetValue<bool>("Dev:GenerateMockPlaidToken") ?? (env != null && env.IsDevelopment());
                if (allowServerMock)
                {
                    dto.PlaidAccessToken = $"mock_access_token_{Guid.NewGuid():N}"; // ensure unique per request
                }
                else
                {
                    return BadRequest("Plaid access token is required for automated accounts.");
                }
            }

            try
            {
                var (liveBalance, plaidId) = await syncService.FetchLiveBalanceAsync(dto.PlaidAccessToken, dto.PlaidAccountId!);

                newAccount.Balance = liveBalance;
                newAccount.PlaidAccessToken = dto.PlaidAccessToken;
                newAccount.PlaidAccountId = plaidId;
            }
            catch (Exception ex)
            {
                // Log the error and return a user-friendly message
                Console.Error.WriteLine($"Error creating Plaid link token: {ex.Message}");
                return StatusCode(500, "An error occurred while setting up bank synchronization. Please try again later.");
            }
        }
        else
        {
            newAccount.Balance = dto.Balance;
        }

        using var context = await _context.CreateDbContextAsync();
        context.Account.Add(newAccount);
        await context.SaveChangesAsync();
        
        return CreatedAtAction(nameof(GetById), new { id = newAccount.Id }, MapToDto(newAccount));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] AccountCreateDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        if (dto.AccountName == null || dto.AccountName.Trim() == "") return BadRequest("The AccountName field is required.");
        if (dto.Balance < 0) return BadRequest("Balance cannot be negative.");
        if (dto.Institution != null && dto.Institution.Length > 100) return BadRequest("Institution name cannot exceed 100 characters.");

        int userId = GetCurrentUserId();
        using var context = await _context.CreateDbContextAsync();
        
        var exists = await context.Account.AnyAsync(a => a.Id == id && a.UserId == userId);
        if (!exists) return NotFound();

        var accountToUpdate = await context.Account.FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);
        if (accountToUpdate == null) return NotFound();

        accountToUpdate.AccountName = dto.AccountName;
        accountToUpdate.Institution = dto.Institution!;
        accountToUpdate.Type = dto.Type;
        accountToUpdate.Balance = dto.Balance;

        await context.SaveChangesAsync();
        
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        int userId = GetCurrentUserId();
        using var context = await _context.CreateDbContextAsync();
        var account = await context.Account.FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);
        if (account == null) return NotFound();

        context.Account.Remove(account);
        await context.SaveChangesAsync();

        return NoContent();
    }
}