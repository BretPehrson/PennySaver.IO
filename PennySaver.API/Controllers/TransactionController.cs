namespace PennySaver.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class TransactionController(IDbContextFactory<PennySaverDbContext> dbContext) : ControllerBase
{
    private readonly IDbContextFactory<PennySaverDbContext> _context = dbContext;

    private int GetCurrentUserId() =>
        int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var userId) 
        ? userId 
        : throw new UnauthorizedAccessException();

    [HttpGet]
    public async Task<ActionResult<IEnumerable<TransactionResponseDto>>> GetAll([FromQuery] TransactionStatus? status)
    {
        int userId = GetCurrentUserId();
        using var context = await _context.CreateDbContextAsync();
        
        var query =  context.Transaction
            .Include(t => t.Account)
            .Include(t => t.Category)
            .Where(t => t.Account!.UserId == userId)
            .AsQueryable();

        if (status.HasValue)
            query = query.Where(t => t.Status == status.Value);
        else
            query = query.Where(t => t.Status != TransactionStatus.Voided);

        var transactions = await query.ToListAsync()
            .ContinueWith(t => t.Result.Select(tr => tr.ToDto()).ToList());

        return transactions;
    }

    [HttpGet("{id}", Name = "GetTransactionById")]
    public async Task<ActionResult<TransactionResponseDto>> GetById(int id)
    {
        int userId = GetCurrentUserId();
        using var context = await _context.CreateDbContextAsync();
        
        var transaction = await context.Transaction
            .Include(t => t.Account)
            .Include(t => t.Category)
            .FirstOrDefaultAsync(t => t.Id == id && t.Account!.UserId == userId);
        if (transaction == null) return NotFound();
        
        return Ok(transaction.ToDto());
    }
    
    [HttpPost]
    public async Task<ActionResult<TransactionResponseDto>> Create([FromBody] TransactionCreateDto transaction)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        // Enforce ownership by ensuring the provided AccountId and CategoryId belong to the logged-in user
        int userId = GetCurrentUserId();

        using var context = await _context.CreateDbContextAsync();

        var accountOwned = await context.Account.AnyAsync(a => a.Id == transaction.AccountId && a.UserId == userId);
        var categoryOwned = await context.Category.AnyAsync(c => c.Id == transaction.CategoryId && c.UserId == userId);
        if (!accountOwned || !categoryOwned) return BadRequest("Invalid account or category assignment.");

        var newTransaction = new Transaction
        {
            AccountId = transaction.AccountId,
            Amount = transaction.Amount,
            Description = transaction.Description,
            Status = transaction.Status,
            CategoryId = transaction.CategoryId
        };

        context.Transaction.Add(newTransaction);
        await context.SaveChangesAsync();
        
        return CreatedAtRoute("GetTransactionById", new { id = newTransaction.Id }, newTransaction.ToDto());
    }
    
    [HttpPut("{id}")]
    public async Task<ActionResult<TransactionResponseDto>> Update(int id, [FromBody] TransactionCreateDto dto)
    {
        int userId = GetCurrentUserId();
        using var context = await _context.CreateDbContextAsync();
        
        var existing = await context.Transaction
            .Include(t => t.Account)
            .FirstOrDefaultAsync(t => t.Id == id && t.Account!.UserId == userId);

        if (existing == null) return NotFound();
        if (existing.Status == TransactionStatus.Voided) return BadRequest("Cannot update a voided transaction.");

        if (existing.AccountId != dto.AccountId)
        {
            var targetAccount = await context.Account
                .AnyAsync(a => a.Id == dto.AccountId && a.UserId == userId);
            
            if (!targetAccount) return BadRequest("Account not found.");

            existing.AccountId = dto.AccountId;
        }

        existing.Amount = dto.Amount;
        existing.Description = dto.Description;
        existing.Status = dto.Status;
        existing.CategoryId = dto.CategoryId;

        await context.SaveChangesAsync();
        
        return NoContent();
    }
    
    [HttpDelete("{id}")]
    public async Task<IActionResult> Archive(int id)
    {
        int userId = GetCurrentUserId();
        using var context = await _context.CreateDbContextAsync();
        
        var transaction = await context.Transaction
            .Include(t => t.Account)
            .FirstOrDefaultAsync(t => t.Id == id && t.Account!.UserId == userId && t.Status != TransactionStatus.Voided);
        if (transaction == null) return NotFound();

        transaction.Status = TransactionStatus.Voided;
        await context.SaveChangesAsync();

        return NoContent();
    }
}