namespace BiatecDiscordBot.Models;

public class QuestionOption
{
    public int Id { get; set; }
    public int QuestionId { get; set; }
    public string Text { get; set; } = string.Empty;
    public string? RoleName { get; set; }
    public ulong? RoleId { get; set; }
    public int OrderIndex { get; set; }

    public Question Question { get; set; } = null!;
    public ICollection<UserAnswer> UserAnswers { get; set; } = new List<UserAnswer>();
}
