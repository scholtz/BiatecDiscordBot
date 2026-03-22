using BiatecDiscordBot.Models;
using BiatecDiscordBot.Models.DTOs;
using BiatecDiscordBot.Services;
using Microsoft.AspNetCore.Mvc;

namespace BiatecDiscordBot.Controllers;

/// <summary>
/// API endpoints for managing questions and recording answers.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class QuestionsController : ControllerBase
{
    private readonly IQuestionService _questionService;
    private readonly ILogger<QuestionsController> _logger;

    public QuestionsController(IQuestionService questionService, ILogger<QuestionsController> logger)
    {
        _questionService = questionService;
        _logger = logger;
    }

    /// <summary>
    /// Creates a new question with multiple choice options.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(Question), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateQuestion([FromBody] QuestionCreateRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Text))
        {
            return BadRequest(new { Error = "Question text is required." });
        }

        if (request.Options.Count == 0)
        {
            return BadRequest(new { Error = "At least one option is required." });
        }

        var question = await _questionService.CreateQuestionAsync(request);
        return CreatedAtAction(nameof(GetQuestion), new { questionId = question.Id }, question);
    }

    /// <summary>
    /// Gets all active questions for a guild.
    /// </summary>
    /// <param name="guildId">The Discord guild ID.</param>
    [HttpGet("guild/{guildId}")]
    [ProducesResponseType(typeof(IReadOnlyList<Question>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetActiveQuestions(ulong guildId)
    {
        var questions = await _questionService.GetActiveQuestionsAsync(guildId);
        return Ok(questions);
    }

    /// <summary>
    /// Gets a specific question by ID.
    /// </summary>
    /// <param name="questionId">The question ID.</param>
    [HttpGet("{questionId}")]
    [ProducesResponseType(typeof(Question), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetQuestion(int questionId)
    {
        var question = await _questionService.GetQuestionByIdAsync(questionId);
        if (question == null)
        {
            return NotFound(new { Error = "Question not found." });
        }

        return Ok(question);
    }

    /// <summary>
    /// Records a user's answer to a question option.
    /// </summary>
    /// <param name="discordUserId">The Discord user ID.</param>
    /// <param name="guildId">The guild ID.</param>
    /// <param name="optionId">The selected option ID.</param>
    [HttpPost("answer")]
    [ProducesResponseType(typeof(UserAnswer), StatusCodes.Status200OK)]
    public async Task<IActionResult> RecordAnswer(
        [FromQuery] ulong discordUserId,
        [FromQuery] ulong guildId,
        [FromQuery] int optionId)
    {
        var answer = await _questionService.RecordAnswerAsync(discordUserId, guildId, optionId);
        return Ok(answer);
    }

    /// <summary>
    /// Gets all answers for a specific user.
    /// </summary>
    /// <param name="discordUserId">The Discord user ID.</param>
    [HttpGet("answers/{discordUserId}")]
    [ProducesResponseType(typeof(IReadOnlyList<UserAnswer>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUserAnswers(ulong discordUserId)
    {
        var answers = await _questionService.GetUserAnswersAsync(discordUserId);
        return Ok(answers);
    }

    /// <summary>
    /// Deactivates a question.
    /// </summary>
    /// <param name="questionId">The question ID to deactivate.</param>
    [HttpDelete("{questionId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeactivateQuestion(int questionId)
    {
        var success = await _questionService.DeactivateQuestionAsync(questionId);
        if (!success)
        {
            return NotFound(new { Error = "Question not found." });
        }

        return NoContent();
    }
}
