using BiatecDiscordBot.Models.DTOs;

namespace BiatecDiscordBot.Services;

/// <summary>
/// Interface for Discord bot operations such as fetching guild members and sending messages.
/// </summary>
public interface IDiscordBotService
{
    /// <summary>
    /// Starts the Discord bot and connects to the gateway.
    /// </summary>
    Task StartAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Stops the Discord bot gracefully.
    /// </summary>
    Task StopAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Gets all users from a specific Discord guild.
    /// </summary>
    Task<IReadOnlyCollection<DiscordUserDto>> GetGuildUsersAsync(ulong guildId);

    /// <summary>
    /// Resolves a Discord server name to its guild ID.
    /// </summary>
    Task<ulong?> GetGuildIdByServerNameAsync(string serverName);

    /// <summary>
    /// Sends a direct message to a specific user.
    /// </summary>
    Task<bool> SendDirectMessageAsync(ulong userId, string message);

    /// <summary>
    /// Sends a direct message with embeds to a specific user.
    /// </summary>
    Task<bool> SendDirectMessageAsync(ulong userId, string message, IReadOnlyCollection<DiscordMessageEmbedRequest> embeds);

    /// <summary>
    /// Sends a message to a specific channel.
    /// </summary>
    Task<bool> SendChannelMessageAsync(ulong channelId, string message);

    /// <summary>
    /// Assigns a role to a user in a guild.
    /// </summary>
    Task<bool> AssignRoleAsync(ulong guildId, ulong userId, ulong roleId);

    /// <summary>
    /// Sends a question with multiple choice options to a user via DM.
    /// </summary>
    Task<bool> SendQuestionAsync(ulong userId, int questionId);

    /// <summary>
    /// Indicates whether the bot is currently connected.
    /// </summary>
    bool IsConnected { get; }
}
