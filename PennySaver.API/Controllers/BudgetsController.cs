namespace PennySaver.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class BudgetsController(IDbContextFactory<PennySaverDbContext> dbContext) : ControllerBase
{
    private readonly IDbContextFactory<PennySaverDbContext> _context = dbContext;

    private int GetCurrentUserId() =>
        int.TryParse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, out var userId) 
        ? userId 
        : throw new UnauthorizedAccessException();

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        int userId = GetCurrentUserId();
        using var context = await _context.CreateDbContextAsync();
        var budgets = await context.Budgets
            .Include(b => b.Category)
            .Where(b => b.UserId == userId)
            .ToListAsync();
        return Ok(budgets);
    }
    
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Budgets budget)
    {
        budget.UserId = GetCurrentUserId();
        using var context = await _context.CreateDbContextAsync();

        var category = await context.Categories.FirstOrDefaultAsync(c => c.Id == budget.CategoryId && c.UserId == budget.UserId);
        if (category == null) return BadRequest("Invalid category.");

        budget.UserId = category.UserId;
        context.Budgets.Add(budget);
        await context.SaveChangesAsync();
        
        return Ok(budget);
    }
    
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] Budgets budget)
    {
        if (id != budget.Id) return BadRequest("ID mismatch.");
        if (!ModelState.IsValid) return BadRequest(ModelState);

        int userId = GetCurrentUserId();
        using var context = await _context.CreateDbContextAsync();
        
        var existing = await context.Budgets.AnyAsync(b => b.Id == id && b.UserId == userId);
        if (!existing) return NotFound();

        var categoryOwned = await context.Categories
            .AnyAsync(c => c.Id == budget.CategoryId && c.UserId == userId);
        if (!categoryOwned) return BadRequest("Invalid category assignment.");

        budget.UserId = userId;
        
        context.Entry(budget).State = EntityState.Modified;
        await context.SaveChangesAsync();
        
        return NoContent();
    }
    
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        int userId = GetCurrentUserId();
        using var context = await _context.CreateDbContextAsync();
        var budget = await context.Budgets.FirstOrDefaultAsync(b => b.Id == id && b.UserId == userId);
        if (budget == null) return NotFound();

        context.Budgets.Remove(budget);
        await context.SaveChangesAsync();

        return NoContent();
    }
}