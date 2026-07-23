namespace PennySaver.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class BudgetController(IDbContextFactory<PennySaverDbContext> dbContext) : ControllerBase
{
    private readonly IDbContextFactory<PennySaverDbContext> _context = dbContext;

    private int? GetCurrentUserId() =>
        int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var userId)
            ? userId : null;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<BudgetResponseDto>>> GetAll()
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized();

        using var context = await _context.CreateDbContextAsync();

        var budgetsInDb = await context.Budget
            .Include(b => b.Category)
            .Where(b => 
                b.UserId == userId.Value 
                && b.IsActive)
            .ToListAsync();

        var budgets = budgetsInDb
            .Select(b => b.ToDto())
            .ToList();

        return budgets;
    }

    [HttpGet("{id}", Name = "GetById")]
    public async Task<ActionResult<BudgetResponseDto>> GetById(int id)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized();

        using var context = await _context.CreateDbContextAsync();

        var budget = await context.Budget
            .Include(b => b.Category)
            .FirstOrDefaultAsync(b => 
                b.Id == id 
                && b.UserId == userId.Value 
                && b.IsActive);

        if (budget == null) return NotFound();
        
        return budget.ToDto();
    }
    
    [HttpPost]
    public async Task<ActionResult<BudgetResponseDto>> Create([FromBody] BudgetCreateDto dto)
    {
        if (dto == null) return BadRequest("Budget data is required.");
        if (dto.EndDate.HasValue && dto.StartDate >= dto.EndDate.Value) return BadRequest("Start date must be before end date.");
        if (dto.TargetAmount < 0) return BadRequest("Target amount must be non-negative.");
        if (string.IsNullOrWhiteSpace(dto.Name)) return BadRequest("Name is required.");
        if (dto.Name.Length > 100) return BadRequest("Name cannot exceed 100 characters.");
        if (dto.CategoryId <= 0) return BadRequest("Valid category ID is required.");
        if (dto.Month < 1 || dto.Month > 12) return BadRequest("Month must be between 1 and 12.");
        if (dto.Year < DateTime.Now.Year) return BadRequest("Year must be the current year or later.");

        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized();

        using var context = await _context.CreateDbContextAsync();

        var exists = await context.Budget
            .AnyAsync(b => 
                b.Name == dto.Name 
                && b.UserId == userId.Value 
                && b.IsActive);
        if (exists) return BadRequest("A budget with this name already exists.");

        var categoryOwned = await context.Category
            .AnyAsync(c => 
                c.Id == dto.CategoryId 
                && c.UserId == userId.Value 
                && c.IsActive == true);
        if (!categoryOwned) return BadRequest("Invalid category assignment.");

        var budget = new Budget
        {
            UserId = userId.Value,
            Name = dto.Name,
            TargetAmount = dto.TargetAmount,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            CategoryId = dto.CategoryId,
            IsActive = dto.IsActive,
            Month = dto.Month,
            Year = dto.Year
        };

        context.Budget.Add(budget);
        await context.SaveChangesAsync();
        
        return CreatedAtRoute("GetById", new { id = budget.Id }, budget.ToDto());
    }
    
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] BudgetCreateDto dto)
    {
        if (dto == null) return BadRequest("Budget data is required.");
        if (dto.EndDate.HasValue && dto.StartDate >= dto.EndDate.Value) return BadRequest("Start date must be before end date.");
        if (dto.TargetAmount < 0) return BadRequest("Target amount must be non-negative.");
        if (string.IsNullOrWhiteSpace(dto.Name)) return BadRequest("Name is required.");
        if (dto.Name.Length > 100) return BadRequest("Name cannot exceed 100 characters.");
        if (dto.CategoryId <= 0) return BadRequest("Valid category ID is required.");

        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized();
            
        using var context = await _context.CreateDbContextAsync();
        
        var existing = await context.Budget
            .FirstOrDefaultAsync(b => 
                b.Id == id 
                && b.UserId == userId.Value 
                && b.IsActive == true);
        if (existing == null) return NotFound();

        var categoryOwned = await context.Category
            .AnyAsync(c => 
                c.Id == dto.CategoryId 
                && c.UserId == userId.Value 
                && c.IsActive == true);
        if (!categoryOwned) return BadRequest("Invalid category assignment.");

        existing.TargetAmount = dto.TargetAmount;
        existing.CategoryId = dto.CategoryId;
        existing.UpdatedAt = DateTime.UtcNow;
        existing.IsActive = dto.IsActive;
        existing.StartDate = dto.StartDate;
        existing.EndDate = dto.EndDate;

        await context.SaveChangesAsync();
        
        return NoContent();
    }
    
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized();
            
        using var context = await _context.CreateDbContextAsync();

        var rowsAffected = await context.Budget
            .Where(b => 
                b.Id == id 
                && b.UserId == userId.Value 
                && b.IsActive)
            .ExecuteUpdateAsync(b => b.SetProperty(p => p.IsActive, false)
                                      .SetProperty(p => p.DeletedAt, DateTime.UtcNow));

        if (rowsAffected == 0) return NotFound();

        return NoContent();
    }
}