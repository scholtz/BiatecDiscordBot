namespace BiatecDiscordBot.Models;

public class DiscordUserEntity
{
    public int Id { get; set; }
    public ulong DiscordId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Discriminator { get; set; } = string.Empty;
    public ulong GuildId { get; set; }
    public DateTime JoinedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<UserAnswer> Answers { get; set; } = new List<UserAnswer>();
}
