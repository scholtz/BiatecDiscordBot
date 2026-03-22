namespace BiatecDiscordBot.Models.DTOs;

public class DiscordUserDto
{
    public ulong DiscordId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Discriminator { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public bool IsBot { get; set; }
    public DateTimeOffset? JoinedAt { get; set; }
}
