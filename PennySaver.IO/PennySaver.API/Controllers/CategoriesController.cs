using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PennySaver.API.Data;
using PennySaver.API.Models;

namespace PennySaver.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class CategoriesController(IDbContextFactory<PennySaverDbContext> dbContext) : ControllerBase
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
        var categories = context.Categories.Where(c => c.UserId == userId).ToList();
        return Ok(categories);
    }
    
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        int userId = GetCurrentUserId();
        using var context = await _context.CreateDbContextAsync();
        var category = context.Categories.FirstOrDefault(c => c.Id == id && c.UserId == userId);
        if (category == null) return NotFound();
        return Ok(category);
    }
    
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Category category)
    {
        category.UserId = GetCurrentUserId();
        using var context = await _context.CreateDbContextAsync();
        context.Categories.Add(category);
        await context.SaveChangesAsync();
        
        return CreatedAtAction(nameof(GetById), new { id = category.Id }, category);
    }
    
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] Category category)
    {
        if (id != category.Id) return BadRequest("ID mismatch.");
        if (!ModelState.IsValid) return BadRequest(ModelState);

        int userId = GetCurrentUserId();
        using var context = await _context.CreateDbContextAsync();
        
        var exists = await context.Categories.AnyAsync(c => c.Id == id && c.UserId == userId);
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
        var category = await context.Categories.FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);
        if (category == null) return NotFound();

        context.Categories.Remove(category);
        await context.SaveChangesAsync();

        return NoContent();
    }
}