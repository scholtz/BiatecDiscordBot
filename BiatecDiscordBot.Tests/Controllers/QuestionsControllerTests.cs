using BiatecDiscordBot.Controllers;
using BiatecDiscordBot.Models;
using BiatecDiscordBot.Models.DTOs;
using BiatecDiscordBot.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace BiatecDiscordBot.Tests.Controllers;

public class QuestionsControllerTests
{
    private readonly Mock<IQuestionService> _questionServiceMock;
    private readonly Mock<ILogger<QuestionsController>> _loggerMock;
    private readonly QuestionsController _controller;

    public QuestionsControllerTests()
    {
        _questionServiceMock = new Mock<IQuestionService>();
        _loggerMock = new Mock<ILogger<QuestionsController>>();
        _controller = new QuestionsController(_questionServiceMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task CreateQuestion_ShouldReturnCreated_WhenValidRequest()
    {
        // Arrange
        var request = new QuestionCreateRequest
        {
            Text = "What color?",
            GuildId = 123,
            OrderIndex = 1,
            Options = new List<QuestionOptionCreateRequest>
            {
                new() { Text = "Red", OrderIndex = 1 }
            }
        };

        _questionServiceMock
            .Setup(s => s.CreateQuestionAsync(request))
            .ReturnsAsync(new Question { Id = 1, Text = "What color?", GuildId = 123 });

        // Act
        var result = await _controller.CreateQuestion(request);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(201, createdResult.StatusCode);
    }

    [Fact]
    public async Task CreateQuestion_ShouldReturnBadRequest_WhenTextEmpty()
    {
        // Arrange
        var request = new QuestionCreateRequest
        {
            Text = "",
            GuildId = 123,
            Options = new List<QuestionOptionCreateRequest>
            {
                new() { Text = "Option", OrderIndex = 1 }
            }
        };

        // Act
        var result = await _controller.CreateQuestion(request);

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(400, badRequest.StatusCode);
    }

    [Fact]
    public async Task CreateQuestion_ShouldReturnBadRequest_WhenNoOptions()
    {
        // Arrange
        var request = new QuestionCreateRequest
        {
            Text = "Valid question",
            GuildId = 123,
            Options = new List<QuestionOptionCreateRequest>()
        };

        // Act
        var result = await _controller.CreateQuestion(request);

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(400, badRequest.StatusCode);
    }

    [Fact]
    public async Task GetQuestion_ShouldReturnNotFound_WhenQuestionDoesNotExist()
    {
        // Arrange
        _questionServiceMock
            .Setup(s => s.GetQuestionByIdAsync(999))
            .ReturnsAsync((Question?)null);

        // Act
        var result = await _controller.GetQuestion(999);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task GetQuestion_ShouldReturnOk_WhenQuestionExists()
    {
        // Arrange
        var question = new Question { Id = 1, Text = "Test", GuildId = 123 };
        _questionServiceMock
            .Setup(s => s.GetQuestionByIdAsync(1))
            .ReturnsAsync(question);

        // Act
        var result = await _controller.GetQuestion(1);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(question, okResult.Value);
    }

    [Fact]
    public async Task DeactivateQuestion_ShouldReturnNoContent_WhenSuccessful()
    {
        // Arrange
        _questionServiceMock
            .Setup(s => s.DeactivateQuestionAsync(1))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.DeactivateQuestion(1);

        // Assert
        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task DeactivateQuestion_ShouldReturnNotFound_WhenQuestionDoesNotExist()
    {
        // Arrange
        _questionServiceMock
            .Setup(s => s.DeactivateQuestionAsync(999))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.DeactivateQuestion(999);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task GetActiveQuestions_ShouldReturnOk()
    {
        // Arrange
        var questions = new List<Question>
        {
            new() { Id = 1, Text = "Q1", GuildId = 123 }
        };

        _questionServiceMock
            .Setup(s => s.GetActiveQuestionsAsync(123))
            .ReturnsAsync(questions);

        // Act
        var result = await _controller.GetActiveQuestions(123);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(questions, okResult.Value);
    }
}
