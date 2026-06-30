namespace PennySaver.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class CategoryController(IDbContextFactory<PennySaverDbContext> dbContext) : ControllerBase
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
        var categories = context.Category.Where(c => c.UserId == userId).ToList();
        return Ok(categories);
    }
    
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        int userId = GetCurrentUserId();
        using var context = await _context.CreateDbContextAsync();
        var category = context.Category.FirstOrDefault(c => c.Id == id && c.UserId == userId);
        if (category == null) return NotFound();
        return Ok(category);
    }
    
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Category category)
    {
        //Overwrite any provided UserId with the logged-in user's ID to enforce ownership
        category.UserId = GetCurrentUserId();
        using var context = await _context.CreateDbContextAsync();
        
        bool exists = await context.Category.AnyAsync(c => c.CategoryName == category.CategoryName && c.UserId == category.UserId);
        if (exists) return BadRequest("Category with the same name already exists for this user.");

        context.Category.Add(category);
        await context.SaveChangesAsync();
        
        return CreatedAtAction(nameof(GetById), new { id = category.Id }, category);
    }
    
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] Category category)
    {
        if (id != category.Id) return BadRequest("ID mismatch.");
        if (!ModelState.IsValid) return BadRequest(ModelState);

        int userId = GetCurrentUserId();

        if (userId != category.UserId)
            return BadRequest("Cannot update a category that belongs to another user.");

        using var context = await _context.CreateDbContextAsync();
        
        var exists = await context.Category.AnyAsync(c => c.Id == id && c.UserId == userId);
        if (!exists) return NotFound();

        category.UserId = userId;
        context.Entry(category).State = EntityState.Modified;
        await context.SaveChangesAsync();
        
        return NoContent();
    }
    
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        int userId = GetCurrentUserId();
        using var context = await _context.CreateDbContextAsync();
        var category = await context.Category.FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);
        if (category == null) return NotFound();

        context.Category.Remove(category);
        await context.SaveChangesAsync();

        return NoContent();
    }
}