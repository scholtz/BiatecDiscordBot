using BiatecDiscordBot.Data;
using Microsoft.EntityFrameworkCore;

namespace BiatecDiscordBot.Tests;

/// <summary>
/// Helper to create in-memory database contexts for testing.
/// </summary>
public static class TestDbContextFactory
{
    public static BotDbContext Create(string? dbName = null)
    {
        var options = new DbContextOptionsBuilder<BotDbContext>()
            .UseInMemoryDatabase(databaseName: dbName ?? Guid.NewGuid().ToString())
            .Options;

        var context = new BotDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }
}
