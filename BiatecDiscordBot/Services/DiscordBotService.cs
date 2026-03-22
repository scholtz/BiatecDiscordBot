using BiatecDiscordBot.Data;
using BiatecDiscordBot.Models;
using BiatecDiscordBot.Models.DTOs;
using Discord;
using Discord.Net;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BiatecDiscordBot.Services;

public class DiscordBotSettings
{
    public string Token { get; set; } = string.Empty;
}

public class DiscordBotService : IDiscordBotService, IDisposable
{
    private readonly DiscordSocketClient _client;
    private readonly IMemoryCache _cache;
    private readonly ILogger<DiscordBotService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly DiscordBotSettings _settings;
    private readonly SemaphoreSlim _startLock = new(1, 1);
    private bool _disposed;
    private volatile bool _isReady;
    private TaskCompletionSource<bool>? _readyTcs;
    private const string GuildIdCacheKeyPrefix = "discord:guild-id:";
    private static readonly TimeSpan ReadyTimeout = TimeSpan.FromSeconds(30);
    private const string BotTokenPrefix = "Bot ";

    public DiscordBotService(
        IMemoryCache cache,
        ILogger<DiscordBotService> logger,
        IServiceProvider serviceProvider,
        IOptions<DiscordBotSettings> settings)
    {
        _cache = cache;
        _logger = logger;
        _serviceProvider = serviceProvider;
        _settings = settings.Value;

        var config = new DiscordSocketConfig
        {
            GatewayIntents = GatewayIntents.Guilds
                | GatewayIntents.GuildMembers
                | GatewayIntents.GuildMessages
                | GatewayIntents.DirectMessages
                | GatewayIntents.MessageContent,
            AlwaysDownloadUsers = true
        };

        _client = new DiscordSocketClient(config);
        _client.Log += LogAsync;
        _client.Connected += OnConnectedAsync;
        _client.Ready += OnReadyAsync;
        _client.Disconnected += OnDisconnectedAsync;
        _client.PresenceUpdated += OnPresenceUpdatedAsync;
        _client.MessageReceived += OnMessageReceivedAsync;
        _client.ReactionAdded += OnReactionAddedAsync;
    }

    public bool IsConnected => _isReady && _client.ConnectionState == ConnectionState.Connected;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var token = NormalizeBotToken(_settings.Token);

        if (string.IsNullOrWhiteSpace(token))
        {
            _logger.LogWarning("Discord bot token is not configured. Bot will not start.");
            return;
        }

        await _startLock.WaitAsync(cancellationToken);
        try
        {
            if (IsConnected)
            {
                _logger.LogInformation("Discord bot is already connected.");
                return;
            }

            _isReady = false;
            _readyTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            await _client.LoginAsync(TokenType.Bot,  token);
            await _client.StartAsync();

            using var readyTimeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            readyTimeoutCts.CancelAfter(ReadyTimeout);

            try
            {
                await _readyTcs.Task.WaitAsync(readyTimeoutCts.Token);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                throw new TimeoutException($"Discord bot did not reach the Ready state within {ReadyTimeout.TotalSeconds} seconds.");
            }

            _logger.LogInformation("Discord bot started successfully. Connected guilds: {GuildCount}.", _client.Guilds.Count);
        }
        catch (HttpException ex) when (ex.DiscordCode == 0 && ex.HttpCode == System.Net.HttpStatusCode.Unauthorized)
        {
            _logger.LogError(ex,
                "Discord authentication failed with 401 Unauthorized. Verify that the configured token is the raw bot token from the Discord Developer Portal Bot page and does not include the 'Bot ' prefix, quotes, or extra whitespace.");
            throw;
        }
        finally
        {
            _startLock.Release();
        }
    }

    private Task OnConnectedAsync()
    {
        _logger.LogInformation("Discord gateway connected. ConnectionState: {ConnectionState}, LoginState: {LoginState}.",
            _client.ConnectionState, _client.LoginState);

        return Task.CompletedTask;
    }

    private Task OnReadyAsync()
    {
        _isReady = true;
        CacheConnectedGuilds();
        _readyTcs?.TrySetResult(true);

        _logger.LogInformation("Discord client ready as {Username}. Connected guilds: {GuildCount}.",
            _client.CurrentUser?.Username,
            _client.Guilds.Count);

        return Task.CompletedTask;
    }

    private Task OnDisconnectedAsync(Exception? exception)
    {
        _isReady = false;

        if (_readyTcs is { Task.IsCompleted: false })
        {
            _readyTcs.TrySetException(exception ?? new InvalidOperationException("Discord client disconnected before reaching the Ready state."));
        }

        if (exception == null)
        {
            _logger.LogWarning("Discord gateway disconnected.");
        }
        else
        {
            _logger.LogError(exception, "Discord gateway disconnected.");
        }

        return Task.CompletedTask;
    }

    private Task OnPresenceUpdatedAsync(SocketUser user, SocketPresence before, SocketPresence after)
    {
        _logger.LogInformation("PresenceUpdated {User} {Before} {After}", user, before, after);

        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _isReady = false;

        if (_client.ConnectionState != ConnectionState.Disconnected)
        {
            await _client.StopAsync();
            _logger.LogInformation("Discord bot stopped.");
        }

        if (_client.LoginState != LoginState.LoggedOut)
        {
            await _client.LogoutAsync();
        }
    }

    public async Task<IReadOnlyCollection<DiscordUserDto>> GetGuildUsersAsync(ulong guildId)
    {
        var guild = _client.GetGuild(guildId);
        if (guild == null)
        {
            _logger.LogWarning("Guild {GuildId} not found.", guildId);
            return Array.Empty<DiscordUserDto>();
        }

        await guild.DownloadUsersAsync();

        var users = guild.Users.Select(u => new DiscordUserDto
        {
            DiscordId = u.Id,
            Username = u.Username,
            Discriminator = u.Discriminator,
            AvatarUrl = u.GetAvatarUrl(),
            IsBot = u.IsBot,
            JoinedAt = u.JoinedAt
        }).ToList();

        // Sync users to database
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BotDbContext>();

        foreach (var user in guild.Users.Where(u => !u.IsBot))
        {
            var existing = await db.DiscordUsers
                .FirstOrDefaultAsync(d => d.DiscordId == user.Id && d.GuildId == guildId);

            if (existing == null)
            {
                db.DiscordUsers.Add(new DiscordUserEntity
                {
                    DiscordId = user.Id,
                    Username = user.Username,
                    Discriminator = user.Discriminator,
                    GuildId = guildId,
                    JoinedAt = user.JoinedAt?.UtcDateTime ?? DateTime.UtcNow
                });
            }
            else
            {
                existing.Username = user.Username;
                existing.Discriminator = user.Discriminator;
                existing.UpdatedAt = DateTime.UtcNow;
            }
        }

        await db.SaveChangesAsync();
        return users;
    }

    public Task<ulong?> GetGuildIdByServerNameAsync(string serverName)
    {
        if (string.IsNullOrWhiteSpace(serverName))
        {
            return Task.FromResult<ulong?>(null);
        }

        var cacheKey = GetGuildIdCacheKey(serverName);
        if (_cache.TryGetValue<ulong>(cacheKey, out var cachedGuildId))
        {
            return Task.FromResult<ulong?>(cachedGuildId);
        }

        var guild = _client.Guilds.FirstOrDefault(g => string.Equals(g.Name, serverName, StringComparison.OrdinalIgnoreCase));
        if (guild == null)
        {
            _logger.LogWarning("Guild with server name {ServerName} was not found.", serverName);
            return Task.FromResult<ulong?>(null);
        }

        _cache.Set(cacheKey, guild.Id, new MemoryCacheEntryOptions
        {
            SlidingExpiration = TimeSpan.FromHours(6),
            AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(1)
        });

        return Task.FromResult<ulong?>(guild.Id);
    }

    public async Task<bool> SendDirectMessageAsync(ulong userId, string message)
    {
        try
        {
            var user = await _client.GetUserAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("User {UserId} not found.", userId);
                return false;
            }

            var dmChannel = await user.CreateDMChannelAsync();
            await dmChannel.SendMessageAsync(message);

            // Track outgoing message
            using var scope = _serviceProvider.CreateScope();
            var tracker = scope.ServiceProvider.GetRequiredService<IMessageTrackingService>();
            await tracker.TrackMessageAsync(new TrackedMessage
            {
                AuthorDiscordId = _client.CurrentUser.Id,
                AuthorUsername = _client.CurrentUser.Username,
                ChannelId = dmChannel.Id,
                Content = message,
                Direction = MessageDirection.Outgoing,
                MessageDiscordId = 0 // DM messages don't have guild context
            });

            _logger.LogInformation("Sent DM to user {UserId}.", userId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send DM to user {UserId}.", userId);
            return false;
        }
    }

    public async Task<bool> SendChannelMessageAsync(ulong channelId, string message)
    {
        try
        {
            if (_client.GetChannel(channelId) is not IMessageChannel channel)
            {
                _logger.LogWarning("Channel {ChannelId} not found.", channelId);
                return false;
            }

            await channel.SendMessageAsync(message);
            _logger.LogInformation("Sent message to channel {ChannelId}.", channelId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send message to channel {ChannelId}.", channelId);
            return false;
        }
    }

    public async Task<bool> AssignRoleAsync(ulong guildId, ulong userId, ulong roleId)
    {
        try
        {
            var guild = _client.GetGuild(guildId);
            if (guild == null)
            {
                _logger.LogWarning("Guild {GuildId} not found.", guildId);
                return false;
            }

            var user = guild.GetUser(userId);
            if (user == null)
            {
                _logger.LogWarning("User {UserId} not found in guild {GuildId}.", userId, guildId);
                return false;
            }

            var role = guild.GetRole(roleId);
            if (role == null)
            {
                _logger.LogWarning("Role {RoleId} not found in guild {GuildId}.", roleId, guildId);
                return false;
            }

            await user.AddRoleAsync(role);
            _logger.LogInformation("Assigned role {RoleName} to user {Username} in guild {GuildId}.",
                role.Name, user.Username, guildId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to assign role {RoleId} to user {UserId} in guild {GuildId}.",
                roleId, userId, guildId);
            return false;
        }
    }

    public async Task<bool> SendQuestionAsync(ulong userId, int questionId)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var questionService = scope.ServiceProvider.GetRequiredService<IQuestionService>();
            var question = await questionService.GetQuestionByIdAsync(questionId);

            if (question == null)
            {
                _logger.LogWarning("Question {QuestionId} not found.", questionId);
                return false;
            }

            var user = await _client.GetUserAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("User {UserId} not found.", userId);
                return false;
            }

            var dmChannel = await user.CreateDMChannelAsync();

            var embed = new EmbedBuilder()
                .WithTitle("📋 Question")
                .WithDescription(question.Text)
                .WithColor(Color.Blue)
                .WithTimestamp(DateTimeOffset.UtcNow);

            var components = new ComponentBuilder();
            foreach (var option in question.Options.OrderBy(o => o.OrderIndex))
            {
                components.WithButton(option.Text, $"answer_{question.Id}_{option.Id}", ButtonStyle.Primary);
            }

            await dmChannel.SendMessageAsync(embed: embed.Build(), components: components.Build());

            _logger.LogInformation("Sent question {QuestionId} to user {UserId}.", questionId, userId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send question {QuestionId} to user {UserId}.", questionId, userId);
            return false;
        }
    }

    private async Task OnMessageReceivedAsync(SocketMessage message)
    {
        if (message.Author.IsBot) return;

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var tracker = scope.ServiceProvider.GetRequiredService<IMessageTrackingService>();

            var guildId = (message.Channel as SocketGuildChannel)?.Guild.Id ?? 0;

            await tracker.TrackMessageAsync(new TrackedMessage
            {
                MessageDiscordId = message.Id,
                AuthorDiscordId = message.Author.Id,
                AuthorUsername = message.Author.Username,
                ChannelId = message.Channel.Id,
                GuildId = guildId,
                Content = message.Content,
                Direction = MessageDirection.Incoming,
                Timestamp = message.Timestamp.UtcDateTime
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error tracking incoming message {MessageId}.", message.Id);
        }
    }

    private async Task OnReactionAddedAsync(
        Cacheable<IUserMessage, ulong> cachedMessage,
        Cacheable<IMessageChannel, ulong> cachedChannel,
        SocketReaction reaction)
    {
        if (reaction.UserId == _client.CurrentUser?.Id) return;

        _logger.LogDebug("Reaction {Emote} added by {UserId} on message {MessageId}.",
            reaction.Emote.Name, reaction.UserId, cachedMessage.Id);

        await Task.CompletedTask;
    }

    private Task LogAsync(LogMessage log)
    {
        var severity = log.Severity switch
        {
            LogSeverity.Critical => LogLevel.Critical,
            LogSeverity.Error => LogLevel.Error,
            LogSeverity.Warning => LogLevel.Warning,
            LogSeverity.Info => LogLevel.Information,
            LogSeverity.Verbose => LogLevel.Debug,
            LogSeverity.Debug => LogLevel.Trace,
            _ => LogLevel.Information
        };

        _logger.Log(severity, log.Exception, "[Discord] {Message}", log.Message);
        return Task.CompletedTask;
    }

    private void CacheConnectedGuilds()
    {
        foreach (var guild in _client.Guilds)
        {
            _cache.Set(GetGuildIdCacheKey(guild.Name), guild.Id, new MemoryCacheEntryOptions
            {
                SlidingExpiration = TimeSpan.FromHours(6),
                AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(1)
            });
        }
    }

    private static string GetGuildIdCacheKey(string serverName) =>
        $"{GuildIdCacheKeyPrefix}{serverName.Trim().ToUpperInvariant()}";

    private static string NormalizeBotToken(string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return string.Empty;
        }

        var normalizedToken = token.Trim().Trim('"');

        if (normalizedToken.StartsWith(BotTokenPrefix, StringComparison.OrdinalIgnoreCase))
        {
            normalizedToken = normalizedToken[BotTokenPrefix.Length..].Trim();
        }

        return normalizedToken;
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _startLock.Dispose();
            _client.Dispose();
            _disposed = true;
        }
    }
}
