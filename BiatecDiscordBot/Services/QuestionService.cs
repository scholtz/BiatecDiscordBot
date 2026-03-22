using BiatecDiscordBot.Data;
using BiatecDiscordBot.Models;
using BiatecDiscordBot.Models.DTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BiatecDiscordBot.Services;

public class QuestionService : IQuestionService
{
    private readonly BotDbContext _db;
    private readonly ILogger<QuestionService> _logger;

    public QuestionService(BotDbContext db, ILogger<QuestionService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<Question> CreateQuestionAsync(QuestionCreateRequest request)
    {
        var question = new Question
        {
            Text = request.Text,
            GuildId = request.GuildId,
            OrderIndex = request.OrderIndex,
            Options = request.Options.Select(o => new QuestionOption
            {
                Text = o.Text,
                RoleName = o.RoleName,
                RoleId = o.RoleId,
                OrderIndex = o.OrderIndex
            }).ToList()
        };

        _db.Questions.Add(question);
        await _db.SaveChangesAsync();
        _logger.LogInformation("Created question {QuestionId} for guild {GuildId}.", question.Id, question.GuildId);
        return question;
    }

    public async Task<IReadOnlyList<Question>> GetActiveQuestionsAsync(ulong guildId)
    {
        return await _db.Questions
            .Include(q => q.Options.OrderBy(o => o.OrderIndex))
            .Where(q => q.GuildId == guildId && q.IsActive)
            .OrderBy(q => q.OrderIndex)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<Question?> GetQuestionByIdAsync(int questionId)
    {
        return await _db.Questions
            .Include(q => q.Options.OrderBy(o => o.OrderIndex))
            .FirstOrDefaultAsync(q => q.Id == questionId);
    }

    public async Task<UserAnswer> RecordAnswerAsync(ulong discordUserId, ulong guildId, int questionOptionId)
    {
        // Ensure user exists in database
        var user = await _db.DiscordUsers
            .FirstOrDefaultAsync(u => u.DiscordId == discordUserId && u.GuildId == guildId);

        if (user == null)
        {
            user = new DiscordUserEntity
            {
                DiscordId = discordUserId,
                GuildId = guildId,
                Username = "Unknown",
                JoinedAt = DateTime.UtcNow
            };
            _db.DiscordUsers.Add(user);
            await _db.SaveChangesAsync();
        }

        var answer = new UserAnswer
        {
            DiscordUserEntityId = user.Id,
            QuestionOptionId = questionOptionId
        };

        _db.UserAnswers.Add(answer);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Recorded answer from user {UserId} for option {OptionId}.",
            discordUserId, questionOptionId);
        return answer;
    }

    public async Task<IReadOnlyList<UserAnswer>> GetUserAnswersAsync(ulong discordUserId)
    {
        return await _db.UserAnswers
            .Include(a => a.QuestionOption)
                .ThenInclude(o => o.Question)
            .Where(a => a.DiscordUser.DiscordId == discordUserId)
            .OrderByDescending(a => a.AnsweredAt)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<bool> DeactivateQuestionAsync(int questionId)
    {
        var question = await _db.Questions.FindAsync(questionId);
        if (question == null) return false;

        question.IsActive = false;
        await _db.SaveChangesAsync();
        _logger.LogInformation("Deactivated question {QuestionId}.", questionId);
        return true;
    }
}
