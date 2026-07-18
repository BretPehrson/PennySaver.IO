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

    [HttpGet]
    public async Task<ActionResult<IEnumerable<AccountResponseDto>>> GetAll()
    {
        if (!int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var userId))
            return Unauthorized();

        using var context = await _context.CreateDbContextAsync();

        var accountsInDb = await context.Account
            .Where(a => 
                a.UserId == userId 
                && a.DeletedAt == null)
            .ToListAsync();

        var accounts = accountsInDb
            .Select(a => a.ToDto())
            .ToList();

        return accounts;
    }

    [HttpGet("{id}", Name = "GetById")]
    public async Task<ActionResult<AccountResponseDto>> GetById(int id)
    {
        if (!int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var userId))
            return Unauthorized();

        using var context = await _context.CreateDbContextAsync();

        var account = await context.Account
            .FirstOrDefaultAsync(a => 
                a.Id == id 
                && a.UserId == userId 
                && a.DeletedAt == null);

        if (account == null) return NotFound();
        
        return account.ToDto();
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> RefreshBalance([FromServices] IAccountSyncCoordinator SyncCoordinator)
    {
        if (!int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var userId))
            return Unauthorized();
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
    public async Task<ActionResult<AccountResponseDto>> Create([FromBody] AccountCreateDto dto, [FromServices] IBankSyncService syncService, [FromServices] IWebHostEnvironment? env = null, [FromServices] IConfiguration? config = null)
    {
        if (dto == null) return BadRequest("Account data is required.");
        if (dto.AccountName == null || dto.AccountName.Trim() == "") return BadRequest("The AccountName field is required.");
        if (!ModelState.IsValid) return BadRequest(ModelState);
        if (dto.Balance < 0) return BadRequest("Balance cannot be negative.");
        if (dto.Institution != null && dto.Institution.Length > 100) return BadRequest("Institution name cannot exceed 100 characters.");

        string? finalizedToken = dto.PlaidAccessToken;
        string? finalizedPlaidAccountId = dto.PlaidAccountId;
        decimal finalizedBalance = dto.Balance;

        if (dto.IsAutomated)
        {
            if (string.IsNullOrWhiteSpace(finalizedToken))
            {
                var allowServerMock = (config?.GetValue<bool>("Dev:GenerateMockPlaidToken") ?? false) 
                      || (env?.IsDevelopment() ?? false);
                if (allowServerMock)
                {
                    finalizedToken = $"mock_access_token_{Guid.NewGuid():N}";
                }
                else
                {
                    return BadRequest("Plaid access token is required for automated accounts.");
                }
            }

            try
            {
                var (liveBalance, plaidId) = await syncService.FetchLiveBalanceAsync(finalizedToken, dto.PlaidAccountId!);
                
                finalizedBalance = liveBalance;
                finalizedPlaidAccountId = plaidId;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error creating Plaid link token: {ex.Message}");
                return StatusCode(500, "An error occurred while setting up bank synchronization. Please try again later.");
            }
        }

        if (!int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var userId))
            return Unauthorized();
        using var context = await _context.CreateDbContextAsync();

        var exists = await context.Account
            .AnyAsync(a => 
                a.AccountName == dto.AccountName 
                && a.UserId == userId);
        if (exists) return BadRequest("An account with the same name already exists for this user.");

        var newAccount = new Account
        {
            UserId = userId,
            AccountName = dto.AccountName,
            Institution = dto.Institution!,
            Type = dto.Type,
            IsAutomated = dto.IsAutomated,
            Balance = finalizedBalance,
            PlaidAccessToken = finalizedToken,
            PlaidAccountId = finalizedPlaidAccountId
        };

        context.Account.Add(newAccount);
        await context.SaveChangesAsync();
        
        return CreatedAtRoute("GetById", new { id = newAccount.Id }, newAccount.ToDto());
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] AccountCreateDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        if (dto.AccountName == null || dto.AccountName.Trim() == "") return BadRequest("The AccountName field is required.");
        if (dto.Balance < 0) return BadRequest("Balance cannot be negative.");
        if (dto.Institution != null && dto.Institution.Length > 100) return BadRequest("Institution name cannot exceed 100 characters.");

        if (!int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var userId))
            return Unauthorized();
        using var context = await _context.CreateDbContextAsync();
        
        var accountToUpdate = await context.Account
            .FirstOrDefaultAsync(a => 
                a.Id == id 
                && a.UserId == userId 
                && a.DeletedAt == null);
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
        if (!int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var userId))
            return Unauthorized();
        using var context = await _context.CreateDbContextAsync();
        
        var rowsAffected = await context.Account
            .Where(a => 
                a.Id == id 
                && a.UserId == userId 
                && a.DeletedAt == null)
            .ExecuteUpdateAsync(a => a.SetProperty(p => p.DeletedAt, DateTime.UtcNow));

        if (rowsAffected == 0) return NotFound();

        return NoContent();
    }
}