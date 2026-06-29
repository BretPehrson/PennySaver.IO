namespace PennySaver.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class TransactionsController(IDbContextFactory<PennySaverDbContext> dbContext) : ControllerBase
{
    private readonly IDbContextFactory<PennySaverDbContext> _context = dbContext;

    private int GetCurrentUserId() =>
        int.TryParse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, out var userId) 
        ? userId 
        : throw new UnauthorizedAccessException();

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] TransactionStatus? status)
    {
        int userId = GetCurrentUserId();
        using var context = await _context.CreateDbContextAsync();
        
        var query =  context.Transactions
            .Include(t => t.Account)
            .Include(t => t.Category)
            .Where(t => t.Account.UserId == userId)
            .AsQueryable();

        if (status.HasValue)
            query = query.Where(t => t.Status == status.Value);
        else
            query = query.Where(t => t.Status != TransactionStatus.Voided);

        var transactions = await query.ToListAsync();

        return Ok(transactions);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        int userId = GetCurrentUserId();
        using var context = await _context.CreateDbContextAsync();
        
        var transaction = await context.Transactions
            .Include(t => t.Account)
            .Include(t => t.Category)
            .FirstOrDefaultAsync(t => t.Id == id && t.Account.UserId == userId);
        if (transaction == null) return NotFound();
        
        return Ok(transaction);
    }
    
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Transaction transaction)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        // Enforce ownership by ensuring the provided AccountId and CategoryId belong to the logged-in user
        int userId = GetCurrentUserId();

        using var context = await _context.CreateDbContextAsync();

        var accountOwned = await context.Accounts.AnyAsync(a => a.Id == transaction.AccountId && a.UserId == userId);
        var categoryOwned = await context.Categories.AnyAsync(c => c.Id == transaction.CategoryId && c.UserId == userId);
        if (!accountOwned || !categoryOwned) return BadRequest("Invalid account or category assignment.");

        context.Transactions.Add(transaction);
        await context.SaveChangesAsync();
        
        return Ok(transaction);
    }
    
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] Transaction transaction)
    {
        int userId = GetCurrentUserId();
        using var context = await _context.CreateDbContextAsync();

        if (userId != transaction.Account.UserId)
            return BadRequest("Cannot update a transaction that belongs to another user.");
        
        var existing = await context.Transactions
            .Include(t => t.Account)
            .FirstOrDefaultAsync(t => t.Id == id && t.Account.UserId == userId);
        if (existing == null) return NotFound();

        if (existing.Status == TransactionStatus.Voided) return BadRequest("Cannot update a voided transaction.");

        existing.Amount = transaction.Amount;
        existing.Description = transaction.Description;
        existing.Status = transaction.Status;
        existing.CategoryId = transaction.CategoryId;

        await context.SaveChangesAsync();
        
        return NoContent();
    }
    
    [HttpDelete("{id}")]
    public async Task<IActionResult> Archive(int id)
    {
        int userId = GetCurrentUserId();
        using var context = await _context.CreateDbContextAsync();
        
        var transaction = await context.Transactions
            .Include(t => t.Account)
            .FirstOrDefaultAsync(t => t.Id == id && t.Account.UserId == userId && t.Status != TransactionStatus.Voided);
        if (transaction == null) return NotFound();

        transaction.Status = TransactionStatus.Voided;
        await context.SaveChangesAsync();

        return NoContent();
    }
}