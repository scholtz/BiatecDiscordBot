using BiatecDiscordBot.Models;

namespace BiatecDiscordBot.Services;

/// <summary>
/// Interface for tracking Discord messages (incoming and outgoing).
/// </summary>
public interface IMessageTrackingService
{
    /// <summary>
    /// Tracks an incoming or outgoing message.
    /// </summary>
    Task TrackMessageAsync(TrackedMessage message);

    /// <summary>
    /// Gets tracked messages for a specific guild.
    /// </summary>
    Task<IReadOnlyList<TrackedMessage>> GetMessagesByGuildAsync(ulong guildId, int page = 1, int pageSize = 50);

    /// <summary>
    /// Gets tracked messages for a specific user.
    /// </summary>
    Task<IReadOnlyList<TrackedMessage>> GetMessagesByUserAsync(ulong userDiscordId, int page = 1, int pageSize = 50);

    /// <summary>
    /// Gets total message count for a guild.
    /// </summary>
    Task<int> GetMessageCountByGuildAsync(ulong guildId);

    /// <summary>
    /// Gets tracked messages by direction (incoming/outgoing).
    /// </summary>
    Task<IReadOnlyList<TrackedMessage>> GetMessagesByDirectionAsync(MessageDirection direction, int page = 1, int pageSize = 50);
}
