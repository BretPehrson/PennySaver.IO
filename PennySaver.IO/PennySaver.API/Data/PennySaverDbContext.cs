using Microsoft.EntityFrameworkCore;
using PennySaver.API.Models;

namespace PennySaver.API.Data;

public class PennySaverDbContext(DbContextOptions<PennySaverDbContext> options) 
    : DbContext(options)
{
    public DbSet<User> Users { get; set; }
    public DbSet<RefreshToken> RefreshTokens { get; set; }
    public DbSet<Accounts> Accounts { get; set; }
    public DbSet<Budgets> Budgets { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<Transaction> Transactions { get; set; }
}