namespace BiatecDiscordBot.Models;

public class UserAnswer
{
    public int Id { get; set; }
    public int DiscordUserEntityId { get; set; }
    public int QuestionOptionId { get; set; }
    public DateTime AnsweredAt { get; set; } = DateTime.UtcNow;

    public DiscordUserEntity DiscordUser { get; set; } = null!;
    public QuestionOption QuestionOption { get; set; } = null!;
}
