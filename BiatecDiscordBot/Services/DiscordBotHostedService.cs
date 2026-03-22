using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BiatecDiscordBot.Services;

/// <summary>
/// Background service that manages the Discord bot lifecycle.
/// </summary>
public class DiscordBotHostedService : IHostedService
{
    private readonly IDiscordBotService _botService;
    private readonly ILogger<DiscordBotHostedService> _logger;

    public DiscordBotHostedService(IDiscordBotService botService, ILogger<DiscordBotHostedService> logger)
    {
        _botService = botService;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting Discord bot hosted service...");
        await _botService.StartAsync(cancellationToken);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping Discord bot hosted service...");
        await _botService.StopAsync(cancellationToken);
    }
}
