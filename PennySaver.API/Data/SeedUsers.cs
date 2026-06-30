using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PennySaver.API.Models;

namespace PennySaver.API.Data;

public static class SeedUsers
{
    public static void Seed(IDbContextFactory<PennySaverDbContext> context)
    {
        using var dbContext = context.CreateDbContext();
        if (dbContext.User.Any())
        {
            return; // Exit the method if users already exist
        }

        var testUser = new User
        {
            Email = "admin@example.com",
            CreatedAt = DateTime.UtcNow
        };

        var password = new PasswordHasher<User>();
        testUser.PasswordHash = password.HashPassword(testUser, "Password123!");
        dbContext.User.Add(testUser);
        dbContext.SaveChanges();
    }
}