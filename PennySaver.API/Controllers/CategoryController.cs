namespace PennySaver.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class CategoryController(IDbContextFactory<PennySaverDbContext> dbContext) : ControllerBase
{
    private readonly IDbContextFactory<PennySaverDbContext> _context = dbContext;

    private int? GetCurrentUserId() =>
        int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var userId)
            ? userId : null;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<CategoryResponseDto>>> GetAll()
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized();

        using var context = await _context.CreateDbContextAsync();

        var categoriesInDb = context.Category
            .Where(c => 
                c.UserId == userId.Value
                && c.IsActive)
            .ToList();

        var categories = categoriesInDb
            .Select(c => c.ToDto())
            .ToList();

        return categories;
    }
    
    [HttpGet("{id}", Name = "GetCategoryById")]
    public async Task<ActionResult<CategoryResponseDto>> GetById(int id)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized();
            
        using var context = await _context.CreateDbContextAsync();

        var category = context.Category
            .FirstOrDefault(c => 
                c.Id == id 
                && c.UserId == userId.Value
                && c.IsActive);

        if (category == null) return NotFound();
        
        return category.ToDto();
    }
    
    [HttpPost]
    public async Task<ActionResult<CategoryResponseDto>> Create([FromBody] CategoryCreateDto category)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        if (category == null) return BadRequest("Category data is required.");
        if (string.IsNullOrWhiteSpace(category.Name)) return BadRequest("Name is required.");
        if (category.Name.Length > 100) return BadRequest("Name cannot exceed 100 characters.");
        if (!string.IsNullOrWhiteSpace(category.Description) && category.Description.Length > 500) return BadRequest("Description cannot exceed 500 characters.");

        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized();

        using var context = await _context.CreateDbContextAsync();
        
        bool exists = await context.Category
            .AnyAsync(c => 
                c.Name == category.Name 
                && c.UserId == userId);
        if (exists) return BadRequest("Category with the same name already exists for this user.");

        var newCategory = new Category
        {
            UserId = userId.Value,
            Name = category.Name,
            Description = category.Description,
            ColorCode = category.ColorCode,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.Category.Add(newCategory);
        await context.SaveChangesAsync();

        return CreatedAtRoute("GetCategoryById", new { id = newCategory.Id }, newCategory.ToDto());
    }
    
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] CategoryCreateDto category)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        if (category == null) return BadRequest("Category data is required.");
        if (string.IsNullOrWhiteSpace(category.Name)) return BadRequest("Name is required.");
        if (category.Name.Length > 100) return BadRequest("Name cannot exceed 100 characters.");
        if (!string.IsNullOrWhiteSpace(category.Description) && category.Description.Length > 500) return BadRequest("Description cannot exceed 500 characters.");

        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized();

        using var context = await _context.CreateDbContextAsync();

        var categoryToUpdate = await context.Category
            .FirstOrDefaultAsync(c => 
                c.Id == id 
                && c.UserId == userId.Value);
        if (categoryToUpdate == null) return NotFound();

        categoryToUpdate.Name = category.Name;
        categoryToUpdate.Description = category.Description;
        categoryToUpdate.ColorCode = category.ColorCode;
        categoryToUpdate.UpdatedAt = DateTime.UtcNow;
        
        context.Entry(categoryToUpdate).State = EntityState.Modified;
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

        var rowsAffected = await context.Category
            .Where(c => 
                c.Id == id 
                && c.UserId == userId.Value)
            .ExecuteUpdateAsync(c => c.SetProperty(p => p.IsActive, false)
                                      .SetProperty(p => p.UpdatedAt, DateTime.UtcNow));

        if (rowsAffected == 0) return NotFound();

        return NoContent();
    }
}