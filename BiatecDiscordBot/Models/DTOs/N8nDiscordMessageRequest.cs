namespace BiatecDiscordBot.Models.DTOs;

public class DiscordMessageRequest
{
    public ulong GuildId { get; set; }
    public ulong UserId { get; set; }
    public string Content { get; set; } = string.Empty;
    public List<DiscordMessageEmbedRequest> Embeds { get; set; } = [];
}

public class DiscordMessageEmbedRequest
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? Image { get; set; }
    public string? Thumbnail { get; set; }
    public string? Type { get; set; }
    public List<DiscordMessageEmbedFieldRequest> Fields { get; set; } = [];
}

public class DiscordMessageEmbedFieldRequest
{
    public string Name { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public bool Inline { get; set; }
}
