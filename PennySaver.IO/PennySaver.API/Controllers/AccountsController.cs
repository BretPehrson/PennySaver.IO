namespace PennySaver.API.Controllers
{
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.EntityFrameworkCore;
    using PennySaver.API.Data;
    using PennySaver.API.Models;

    //[Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class AccountsController(IDbContextFactory<PennySaverDbContext> dbContext) : ControllerBase
    {
        private readonly IDbContextFactory<PennySaverDbContext> _dbContext = dbContext;

        [HttpGet]
        public IActionResult GetAll()
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userId, out var parsedUserId))
                return Unauthorized();

            using var dbContext = _dbContext.CreateDbContext();

            var userAccounts = dbContext.Accounts.Where(a => a.UserId == parsedUserId).ToList();

            return Ok(userAccounts);
        }

        [HttpGet("{id}")]
        public IActionResult GetById(int id)
        {
            using var dbContext = _dbContext.CreateDbContext();
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userId, out var parsedUserId))
                return Unauthorized();

            var account = dbContext.Accounts.FirstOrDefault(a => a.Id == id && a.UserId == parsedUserId);
            if (account == null)
                return NotFound();
            return Ok(account);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Accounts account)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            using var dbContext = _dbContext.CreateDbContext();
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userId, out var parsedUserId))
                return Unauthorized();

            account.UserId = parsedUserId;
            dbContext.Accounts.Add(account);
            await dbContext.SaveChangesAsync();
            
            return CreatedAtAction(nameof(GetById), new { id = account.Id }, account);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] Accounts account)
        {
            if (!ModelState.IsValid || id != account.Id)
                return BadRequest(ModelState);

            using var dbContext = _dbContext.CreateDbContext();
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userId, out var parsedUserId))
                return Unauthorized();
            if (account.UserId != parsedUserId)
                return Forbid();
            if (account == null)
                return NotFound();

            dbContext.Entry(account).State = EntityState.Modified;

            try
            {
                await dbContext.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (await dbContext.Accounts.FindAsync(id) == null)
                    return NotFound();
                
                throw;
            }
            
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            using var dbContext = _dbContext.CreateDbContext();
            var existingAccount = await dbContext.Accounts.FindAsync(id);
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userId, out var parsedUserId))
                return Unauthorized();
            if (existingAccount!.UserId != parsedUserId)
                return Forbid();
            if (existingAccount == null)
                return NotFound();

            dbContext.Accounts.Remove(existingAccount);
            await dbContext.SaveChangesAsync();

            return NoContent();
        }
    }
}