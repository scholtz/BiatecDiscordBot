using BiatecDiscordBot.Controllers;
using BiatecDiscordBot.Models.DTOs;
using BiatecDiscordBot.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace BiatecDiscordBot.Tests.Controllers;

public class DiscordControllerTests
{
    private readonly Mock<IDiscordBotService> _botServiceMock;
    private readonly Mock<ILogger<DiscordController>> _loggerMock;
    private readonly DiscordController _controller;

    public DiscordControllerTests()
    {
        _botServiceMock = new Mock<IDiscordBotService>();
        _loggerMock = new Mock<ILogger<DiscordController>>();
        _controller = new DiscordController(_botServiceMock.Object, _loggerMock.Object);
    }

    [Fact]
    public void GetStatus_ShouldReturnConnectedStatus()
    {
        // Arrange
        _botServiceMock.Setup(s => s.IsConnected).Returns(true);

        // Act
        var result = _controller.GetStatus();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task GetGuildUsers_ShouldReturnUsers()
    {
        // Arrange
        const string serverName = "VoteCoin";
        var users = new List<DiscordUserDto>
        {
            new() { DiscordId = 1, Username = "User1" },
            new() { DiscordId = 2, Username = "User2" }
        };

        _botServiceMock
            .Setup(s => s.GetGuildIdByServerNameAsync(serverName))
            .ReturnsAsync(123UL);

        _botServiceMock
            .Setup(s => s.GetGuildUsersAsync(123))
            .ReturnsAsync(users);

        // Act
        var result = await _controller.GetGuildUsers(serverName);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(users, okResult.Value);
    }

    [Fact]
    public async Task SendMessage_ShouldReturnBadRequest_WhenMessageEmpty()
    {
        // Arrange
        var request = new SendMessageRequest { UserId = 1, Message = "" };

        // Act
        var result = await _controller.SendMessage(request);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task SendMessage_ShouldReturnOk_WhenSuccessful()
    {
        // Arrange
        var request = new SendMessageRequest { UserId = 1, Message = "Hello" };
        _botServiceMock
            .Setup(s => s.SendDirectMessageAsync(1, "Hello"))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.SendMessage(request);

        // Assert
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task SendMessage_ShouldReturnBadRequest_WhenFailed()
    {
        // Arrange
        var request = new SendMessageRequest { UserId = 1, Message = "Hello" };
        _botServiceMock
            .Setup(s => s.SendDirectMessageAsync(1, "Hello"))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.SendMessage(request);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task AssignRole_ShouldReturnOk_WhenSuccessful()
    {
        // Arrange
        const string serverName = "VoteCoin";
        _botServiceMock
            .Setup(s => s.GetGuildIdByServerNameAsync(serverName))
            .ReturnsAsync(1UL);

        _botServiceMock
            .Setup(s => s.AssignRoleAsync(1, 2, 3))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.AssignRole(serverName, 2, 3);

        // Assert
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task AssignRole_ShouldReturnBadRequest_WhenFailed()
    {
        // Arrange
        const string serverName = "VoteCoin";
        _botServiceMock
            .Setup(s => s.GetGuildIdByServerNameAsync(serverName))
            .ReturnsAsync(1UL);

        _botServiceMock
            .Setup(s => s.AssignRoleAsync(1, 2, 3))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.AssignRole(serverName, 2, 3);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task SendChannelMessage_ShouldReturnBadRequest_WhenMessageEmpty()
    {
        // Act
        var result = await _controller.SendChannelMessage(123, "");

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task SendQuestion_ShouldReturnOk_WhenSuccessful()
    {
        // Arrange
        _botServiceMock
            .Setup(s => s.SendQuestionAsync(1, 2))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.SendQuestion(1, 2);

        // Assert
        Assert.IsType<OkObjectResult>(result);
    }
}
