namespace BiatecDiscordBot.Models.DTOs;

public class QuestionCreateRequest
{
    public string Text { get; set; } = string.Empty;
    public ulong GuildId { get; set; }
    public int OrderIndex { get; set; }
    public List<QuestionOptionCreateRequest> Options { get; set; } = new();
}

public class QuestionOptionCreateRequest
{
    public string Text { get; set; } = string.Empty;
    public string? RoleName { get; set; }
    public ulong? RoleId { get; set; }
    public int OrderIndex { get; set; }
}
