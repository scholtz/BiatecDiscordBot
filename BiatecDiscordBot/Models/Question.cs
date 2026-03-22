namespace BiatecDiscordBot.Models;

public class Question
{
    public int Id { get; set; }
    public string Text { get; set; } = string.Empty;
    public ulong GuildId { get; set; }
    public bool IsActive { get; set; } = true;
    public int OrderIndex { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<QuestionOption> Options { get; set; } = new List<QuestionOption>();
}
