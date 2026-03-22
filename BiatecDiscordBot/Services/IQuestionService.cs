using BiatecDiscordBot.Models;
using BiatecDiscordBot.Models.DTOs;

namespace BiatecDiscordBot.Services;

/// <summary>
/// Interface for managing questions and user answers.
/// </summary>
public interface IQuestionService
{
    /// <summary>
    /// Creates a new question with options.
    /// </summary>
    Task<Question> CreateQuestionAsync(QuestionCreateRequest request);

    /// <summary>
    /// Gets all active questions for a guild.
    /// </summary>
    Task<IReadOnlyList<Question>> GetActiveQuestionsAsync(ulong guildId);

    /// <summary>
    /// Gets a question by ID including its options.
    /// </summary>
    Task<Question?> GetQuestionByIdAsync(int questionId);

    /// <summary>
    /// Records a user's answer to a question option.
    /// </summary>
    Task<UserAnswer> RecordAnswerAsync(ulong discordUserId, ulong guildId, int questionOptionId);

    /// <summary>
    /// Gets all answers for a specific user.
    /// </summary>
    Task<IReadOnlyList<UserAnswer>> GetUserAnswersAsync(ulong discordUserId);

    /// <summary>
    /// Deactivates a question.
    /// </summary>
    Task<bool> DeactivateQuestionAsync(int questionId);
}
