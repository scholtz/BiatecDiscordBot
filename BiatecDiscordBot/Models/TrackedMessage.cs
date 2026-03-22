namespace BiatecDiscordBot.Models;

public class TrackedMessage
{
    public int Id { get; set; }
    public ulong MessageDiscordId { get; set; }
    public ulong AuthorDiscordId { get; set; }
    public string AuthorUsername { get; set; } = string.Empty;
    public ulong ChannelId { get; set; }
    public ulong GuildId { get; set; }
    public string Content { get; set; } = string.Empty;
    public MessageDirection Direction { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public enum MessageDirection
{
    Incoming,
    Outgoing
}
