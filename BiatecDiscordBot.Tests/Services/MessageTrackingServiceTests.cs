using BiatecDiscordBot.Models;
using BiatecDiscordBot.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace BiatecDiscordBot.Tests.Services;

public class MessageTrackingServiceTests
{
    private readonly Mock<ILogger<MessageTrackingService>> _loggerMock;

    public MessageTrackingServiceTests()
    {
        _loggerMock = new Mock<ILogger<MessageTrackingService>>();
    }

    [Fact]
    public async Task TrackMessageAsync_ShouldStoreMessage()
    {
        // Arrange
        using var db = TestDbContextFactory.Create();
        var service = new MessageTrackingService(db, _loggerMock.Object);

        var message = new TrackedMessage
        {
            MessageDiscordId = 1001,
            AuthorDiscordId = 2001,
            AuthorUsername = "TestUser",
            ChannelId = 3001,
            GuildId = 4001,
            Content = "Hello World",
            Direction = MessageDirection.Incoming
        };

        // Act
        await service.TrackMessageAsync(message);

        // Assert
        Assert.Equal(1, db.TrackedMessages.Count());
        var stored = db.TrackedMessages.First();
        Assert.Equal("Hello World", stored.Content);
        Assert.Equal(MessageDirection.Incoming, stored.Direction);
    }

    [Fact]
    public async Task GetMessagesByGuildAsync_ShouldReturnGuildMessages()
    {
        // Arrange
        using var db = TestDbContextFactory.Create();
        var service = new MessageTrackingService(db, _loggerMock.Object);

        await service.TrackMessageAsync(new TrackedMessage
        {
            MessageDiscordId = 1, AuthorDiscordId = 1, AuthorUsername = "User1",
            ChannelId = 1, GuildId = 100, Content = "Msg1", Direction = MessageDirection.Incoming
        });
        await service.TrackMessageAsync(new TrackedMessage
        {
            MessageDiscordId = 2, AuthorDiscordId = 2, AuthorUsername = "User2",
            ChannelId = 1, GuildId = 200, Content = "Msg2", Direction = MessageDirection.Incoming
        });
        await service.TrackMessageAsync(new TrackedMessage
        {
            MessageDiscordId = 3, AuthorDiscordId = 1, AuthorUsername = "User1",
            ChannelId = 1, GuildId = 100, Content = "Msg3", Direction = MessageDirection.Outgoing
        });

        // Act
        var result = await service.GetMessagesByGuildAsync(100);

        // Assert
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetMessagesByUserAsync_ShouldReturnUserMessages()
    {
        // Arrange
        using var db = TestDbContextFactory.Create();
        var service = new MessageTrackingService(db, _loggerMock.Object);

        await service.TrackMessageAsync(new TrackedMessage
        {
            MessageDiscordId = 1, AuthorDiscordId = 555, AuthorUsername = "TestUser",
            ChannelId = 1, GuildId = 100, Content = "Hello", Direction = MessageDirection.Incoming
        });
        await service.TrackMessageAsync(new TrackedMessage
        {
            MessageDiscordId = 2, AuthorDiscordId = 999, AuthorUsername = "OtherUser",
            ChannelId = 1, GuildId = 100, Content = "World", Direction = MessageDirection.Incoming
        });

        // Act
        var result = await service.GetMessagesByUserAsync(555);

        // Assert
        Assert.Single(result);
        Assert.Equal("Hello", result[0].Content);
    }

    [Fact]
    public async Task GetMessageCountByGuildAsync_ShouldReturnCorrectCount()
    {
        // Arrange
        using var db = TestDbContextFactory.Create();
        var service = new MessageTrackingService(db, _loggerMock.Object);

        for (int i = 0; i < 5; i++)
        {
            await service.TrackMessageAsync(new TrackedMessage
            {
                MessageDiscordId = (ulong)i, AuthorDiscordId = 1, AuthorUsername = "User",
                ChannelId = 1, GuildId = 100, Content = $"Msg {i}", Direction = MessageDirection.Incoming
            });
        }

        // Act
        var count = await service.GetMessageCountByGuildAsync(100);

        // Assert
        Assert.Equal(5, count);
    }

    [Fact]
    public async Task GetMessagesByDirectionAsync_ShouldFilterByDirection()
    {
        // Arrange
        using var db = TestDbContextFactory.Create();
        var service = new MessageTrackingService(db, _loggerMock.Object);

        await service.TrackMessageAsync(new TrackedMessage
        {
            MessageDiscordId = 1, AuthorDiscordId = 1, AuthorUsername = "User",
            ChannelId = 1, GuildId = 100, Content = "Incoming", Direction = MessageDirection.Incoming
        });
        await service.TrackMessageAsync(new TrackedMessage
        {
            MessageDiscordId = 2, AuthorDiscordId = 1, AuthorUsername = "Bot",
            ChannelId = 1, GuildId = 100, Content = "Outgoing", Direction = MessageDirection.Outgoing
        });

        // Act
        var incoming = await service.GetMessagesByDirectionAsync(MessageDirection.Incoming);
        var outgoing = await service.GetMessagesByDirectionAsync(MessageDirection.Outgoing);

        // Assert
        Assert.Single(incoming);
        Assert.Single(outgoing);
        Assert.Equal("Incoming", incoming[0].Content);
        Assert.Equal("Outgoing", outgoing[0].Content);
    }

    [Fact]
    public async Task GetMessagesByGuildAsync_ShouldSupportPagination()
    {
        // Arrange
        using var db = TestDbContextFactory.Create();
        var service = new MessageTrackingService(db, _loggerMock.Object);

        for (int i = 0; i < 10; i++)
        {
            await service.TrackMessageAsync(new TrackedMessage
            {
                MessageDiscordId = (ulong)i, AuthorDiscordId = 1, AuthorUsername = "User",
                ChannelId = 1, GuildId = 100, Content = $"Msg {i}", Direction = MessageDirection.Incoming
            });
        }

        // Act
        var page1 = await service.GetMessagesByGuildAsync(100, page: 1, pageSize: 3);
        var page2 = await service.GetMessagesByGuildAsync(100, page: 2, pageSize: 3);

        // Assert
        Assert.Equal(3, page1.Count);
        Assert.Equal(3, page2.Count);
    }
}
