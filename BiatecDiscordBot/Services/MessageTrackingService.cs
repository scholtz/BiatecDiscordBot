using BiatecDiscordBot.Data;
using BiatecDiscordBot.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BiatecDiscordBot.Services;

public class MessageTrackingService : IMessageTrackingService
{
    private readonly BotDbContext _db;
    private readonly ILogger<MessageTrackingService> _logger;

    public MessageTrackingService(BotDbContext db, ILogger<MessageTrackingService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task TrackMessageAsync(TrackedMessage message)
    {
        _db.TrackedMessages.Add(message);
        await _db.SaveChangesAsync();
        _logger.LogDebug("Tracked {Direction} message {MessageId} from {Author}.",
            message.Direction, message.MessageDiscordId, message.AuthorUsername);
    }

    public async Task<IReadOnlyList<TrackedMessage>> GetMessagesByGuildAsync(ulong guildId, int page = 1, int pageSize = 50)
    {
        return await _db.TrackedMessages
            .Where(m => m.GuildId == guildId)
            .OrderByDescending(m => m.Timestamp)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<IReadOnlyList<TrackedMessage>> GetMessagesByUserAsync(ulong userDiscordId, int page = 1, int pageSize = 50)
    {
        return await _db.TrackedMessages
            .Where(m => m.AuthorDiscordId == userDiscordId)
            .OrderByDescending(m => m.Timestamp)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<int> GetMessageCountByGuildAsync(ulong guildId)
    {
        return await _db.TrackedMessages
            .CountAsync(m => m.GuildId == guildId);
    }

    public async Task<IReadOnlyList<TrackedMessage>> GetMessagesByDirectionAsync(MessageDirection direction, int page = 1, int pageSize = 50)
    {
        return await _db.TrackedMessages
            .Where(m => m.Direction == direction)
            .OrderByDescending(m => m.Timestamp)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .AsNoTracking()
            .ToListAsync();
    }
}
