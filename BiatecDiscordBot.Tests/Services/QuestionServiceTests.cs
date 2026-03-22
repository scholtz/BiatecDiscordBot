using BiatecDiscordBot.Models;
using BiatecDiscordBot.Models.DTOs;
using BiatecDiscordBot.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace BiatecDiscordBot.Tests.Services;

public class QuestionServiceTests
{
    private readonly Mock<ILogger<QuestionService>> _loggerMock;

    public QuestionServiceTests()
    {
        _loggerMock = new Mock<ILogger<QuestionService>>();
    }

    [Fact]
    public async Task CreateQuestionAsync_ShouldCreateQuestionWithOptions()
    {
        // Arrange
        using var db = TestDbContextFactory.Create();
        var service = new QuestionService(db, _loggerMock.Object);
        var request = new QuestionCreateRequest
        {
            Text = "What is your favorite color?",
            GuildId = 123456789,
            OrderIndex = 1,
            Options = new List<QuestionOptionCreateRequest>
            {
                new() { Text = "Red", RoleName = "Red Team", RoleId = 111, OrderIndex = 1 },
                new() { Text = "Blue", RoleName = "Blue Team", RoleId = 222, OrderIndex = 2 }
            }
        };

        // Act
        var result = await service.CreateQuestionAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Id > 0);
        Assert.Equal("What is your favorite color?", result.Text);
        Assert.Equal((ulong)123456789, result.GuildId);
        Assert.True(result.IsActive);
        Assert.Equal(2, result.Options.Count);
    }

    [Fact]
    public async Task GetActiveQuestionsAsync_ShouldReturnOnlyActiveQuestions()
    {
        // Arrange
        using var db = TestDbContextFactory.Create();
        var service = new QuestionService(db, _loggerMock.Object);
        ulong guildId = 123456789;

        await service.CreateQuestionAsync(new QuestionCreateRequest
        {
            Text = "Active Question",
            GuildId = guildId,
            OrderIndex = 1,
            Options = new List<QuestionOptionCreateRequest>
            {
                new() { Text = "Option A", OrderIndex = 1 }
            }
        });

        var inactiveQuestion = await service.CreateQuestionAsync(new QuestionCreateRequest
        {
            Text = "Inactive Question",
            GuildId = guildId,
            OrderIndex = 2,
            Options = new List<QuestionOptionCreateRequest>
            {
                new() { Text = "Option B", OrderIndex = 1 }
            }
        });

        await service.DeactivateQuestionAsync(inactiveQuestion.Id);

        // Act
        var result = await service.GetActiveQuestionsAsync(guildId);

        // Assert
        Assert.Single(result);
        Assert.Equal("Active Question", result[0].Text);
    }

    [Fact]
    public async Task GetQuestionByIdAsync_ShouldReturnQuestionWithOptions()
    {
        // Arrange
        using var db = TestDbContextFactory.Create();
        var service = new QuestionService(db, _loggerMock.Object);
        var created = await service.CreateQuestionAsync(new QuestionCreateRequest
        {
            Text = "Test Question",
            GuildId = 123,
            OrderIndex = 1,
            Options = new List<QuestionOptionCreateRequest>
            {
                new() { Text = "Option 1", OrderIndex = 1 },
                new() { Text = "Option 2", OrderIndex = 2 }
            }
        });

        // Act
        var result = await service.GetQuestionByIdAsync(created.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Test Question", result.Text);
        Assert.Equal(2, result.Options.Count);
    }

    [Fact]
    public async Task GetQuestionByIdAsync_ShouldReturnNullForNonExistentId()
    {
        // Arrange
        using var db = TestDbContextFactory.Create();
        var service = new QuestionService(db, _loggerMock.Object);

        // Act
        var result = await service.GetQuestionByIdAsync(999);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task RecordAnswerAsync_ShouldCreateUserAndAnswer()
    {
        // Arrange
        using var db = TestDbContextFactory.Create();
        var service = new QuestionService(db, _loggerMock.Object);

        var question = await service.CreateQuestionAsync(new QuestionCreateRequest
        {
            Text = "Test Question",
            GuildId = 123,
            OrderIndex = 1,
            Options = new List<QuestionOptionCreateRequest>
            {
                new() { Text = "Option 1", OrderIndex = 1 }
            }
        });

        var optionId = question.Options.First().Id;

        // Act
        var answer = await service.RecordAnswerAsync(discordUserId: 555, guildId: 123, questionOptionId: optionId);

        // Assert
        Assert.NotNull(answer);
        Assert.True(answer.Id > 0);
        Assert.Equal(optionId, answer.QuestionOptionId);
    }

    [Fact]
    public async Task RecordAnswerAsync_ShouldReuseExistingUser()
    {
        // Arrange
        using var db = TestDbContextFactory.Create();
        var service = new QuestionService(db, _loggerMock.Object);

        var question = await service.CreateQuestionAsync(new QuestionCreateRequest
        {
            Text = "Test",
            GuildId = 123,
            OrderIndex = 1,
            Options = new List<QuestionOptionCreateRequest>
            {
                new() { Text = "A", OrderIndex = 1 },
                new() { Text = "B", OrderIndex = 2 }
            }
        });

        var options = question.Options.ToList();

        // Act - answer twice as same user
        await service.RecordAnswerAsync(555, 123, options[0].Id);
        await service.RecordAnswerAsync(555, 123, options[1].Id);

        // Assert
        var users = db.DiscordUsers.Where(u => u.DiscordId == 555).ToList();
        Assert.Single(users);
        Assert.Equal(2, db.UserAnswers.Count());
    }

    [Fact]
    public async Task DeactivateQuestionAsync_ShouldSetIsActiveToFalse()
    {
        // Arrange
        using var db = TestDbContextFactory.Create();
        var service = new QuestionService(db, _loggerMock.Object);

        var question = await service.CreateQuestionAsync(new QuestionCreateRequest
        {
            Text = "Test",
            GuildId = 123,
            OrderIndex = 1,
            Options = new List<QuestionOptionCreateRequest>
            {
                new() { Text = "A", OrderIndex = 1 }
            }
        });

        // Act
        var result = await service.DeactivateQuestionAsync(question.Id);

        // Assert
        Assert.True(result);
        var updated = await service.GetQuestionByIdAsync(question.Id);
        Assert.NotNull(updated);
        Assert.False(updated.IsActive);
    }

    [Fact]
    public async Task DeactivateQuestionAsync_ShouldReturnFalseForNonExistentId()
    {
        // Arrange
        using var db = TestDbContextFactory.Create();
        var service = new QuestionService(db, _loggerMock.Object);

        // Act
        var result = await service.DeactivateQuestionAsync(999);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task GetUserAnswersAsync_ShouldReturnAnswersForUser()
    {
        // Arrange
        using var db = TestDbContextFactory.Create();
        var service = new QuestionService(db, _loggerMock.Object);

        var question = await service.CreateQuestionAsync(new QuestionCreateRequest
        {
            Text = "Test",
            GuildId = 123,
            OrderIndex = 1,
            Options = new List<QuestionOptionCreateRequest>
            {
                new() { Text = "A", OrderIndex = 1 }
            }
        });

        var optionId = question.Options.First().Id;
        await service.RecordAnswerAsync(555, 123, optionId);

        // Act
        var answers = await service.GetUserAnswersAsync(555);

        // Assert
        Assert.Single(answers);
    }
}
