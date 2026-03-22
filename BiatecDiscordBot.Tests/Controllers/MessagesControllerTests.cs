using BiatecDiscordBot.Controllers;
using BiatecDiscordBot.Models;
using BiatecDiscordBot.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace BiatecDiscordBot.Tests.Controllers;

public class MessagesControllerTests
{
    private readonly Mock<IMessageTrackingService> _trackingServiceMock;
    private readonly Mock<ILogger<MessagesController>> _loggerMock;
    private readonly MessagesController _controller;

    public MessagesControllerTests()
    {
        _trackingServiceMock = new Mock<IMessageTrackingService>();
        _loggerMock = new Mock<ILogger<MessagesController>>();
        _controller = new MessagesController(_trackingServiceMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task GetMessagesByGuild_ShouldReturnOkWithPagination()
    {
        // Arrange
        var messages = new List<TrackedMessage>
        {
            new() { Id = 1, GuildId = 100, Content = "Test", Direction = MessageDirection.Incoming }
        };

        _trackingServiceMock
            .Setup(s => s.GetMessagesByGuildAsync(100, 1, 50))
            .ReturnsAsync(messages);
        _trackingServiceMock
            .Setup(s => s.GetMessageCountByGuildAsync(100))
            .ReturnsAsync(1);

        // Act
        var result = await _controller.GetMessagesByGuild(100);

        // Assert
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task GetMessagesByUser_ShouldReturnOk()
    {
        // Arrange
        var messages = new List<TrackedMessage>
        {
            new() { Id = 1, AuthorDiscordId = 555, Content = "Hello", Direction = MessageDirection.Incoming }
        };

        _trackingServiceMock
            .Setup(s => s.GetMessagesByUserAsync(555, 1, 50))
            .ReturnsAsync(messages);

        // Act
        var result = await _controller.GetMessagesByUser(555);

        // Assert
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task GetMessagesByDirection_ShouldReturnOk()
    {
        // Arrange
        var messages = new List<TrackedMessage>();

        _trackingServiceMock
            .Setup(s => s.GetMessagesByDirectionAsync(MessageDirection.Incoming, 1, 50))
            .ReturnsAsync(messages);

        // Act
        var result = await _controller.GetMessagesByDirection(MessageDirection.Incoming);

        // Assert
        Assert.IsType<OkObjectResult>(result);
    }
}
