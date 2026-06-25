using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PennySaver.API.Models;

namespace PennySaver.API.Data;

public static class SeedUsers
{
    public static void Seed(IDbContextFactory<PennySaverDbContext> context)
    {
        var dbContext = context.CreateDbContext();
        if (dbContext.Users.Any())
        {
            return; // Exit the method if users already exist
        }

        var testUser = new User
        {
            Username = "admin",
            Email = "admin@example.com",
            CreatedAt = DateTime.UtcNow
        };

        var password = new PasswordHasher<User>();
        testUser.PasswordHash = password.HashPassword(testUser, "Password123!");
        dbContext.Users.Add(testUser);
        dbContext.SaveChanges();
    }
}