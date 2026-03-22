namespace BiatecDiscordBot.Models.DTOs;

public class SendMessageRequest
{
    public ulong UserId { get; set; }
    public ulong GuildId { get; set; }
    public string Message { get; set; } = string.Empty;
}
