using BiatecDiscordBot.Models;
using BiatecDiscordBot.Services;
using Microsoft.AspNetCore.Mvc;

namespace BiatecDiscordBot.Controllers;

/// <summary>
/// API endpoints for viewing tracked Discord messages.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class MessagesController : ControllerBase
{
    private readonly IMessageTrackingService _messageTrackingService;
    private readonly ILogger<MessagesController> _logger;

    public MessagesController(IMessageTrackingService messageTrackingService, ILogger<MessagesController> logger)
    {
        _messageTrackingService = messageTrackingService;
        _logger = logger;
    }

    /// <summary>
    /// Gets tracked messages for a specific guild.
    /// </summary>
    /// <param name="guildId">The Discord guild ID.</param>
    /// <param name="page">Page number (1-based).</param>
    /// <param name="pageSize">Number of items per page.</param>
    [HttpGet("guild/{guildId}")]
    [ProducesResponseType(typeof(IReadOnlyList<TrackedMessage>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMessagesByGuild(ulong guildId, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        var messages = await _messageTrackingService.GetMessagesByGuildAsync(guildId, page, pageSize);
        var count = await _messageTrackingService.GetMessageCountByGuildAsync(guildId);
        return Ok(new { Total = count, Page = page, PageSize = pageSize, Messages = messages });
    }

    /// <summary>
    /// Gets tracked messages for a specific user.
    /// </summary>
    /// <param name="userId">The Discord user ID.</param>
    /// <param name="page">Page number (1-based).</param>
    /// <param name="pageSize">Number of items per page.</param>
    [HttpGet("user/{userId}")]
    [ProducesResponseType(typeof(IReadOnlyList<TrackedMessage>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMessagesByUser(ulong userId, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        var messages = await _messageTrackingService.GetMessagesByUserAsync(userId, page, pageSize);
        return Ok(new { Page = page, PageSize = pageSize, Messages = messages });
    }

    /// <summary>
    /// Gets tracked messages by direction (incoming or outgoing).
    /// </summary>
    /// <param name="direction">Message direction: 0 = Incoming, 1 = Outgoing.</param>
    /// <param name="page">Page number (1-based).</param>
    /// <param name="pageSize">Number of items per page.</param>
    [HttpGet("direction/{direction}")]
    [ProducesResponseType(typeof(IReadOnlyList<TrackedMessage>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMessagesByDirection(MessageDirection direction, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        var messages = await _messageTrackingService.GetMessagesByDirectionAsync(direction, page, pageSize);
        return Ok(new { Direction = direction.ToString(), Page = page, PageSize = pageSize, Messages = messages });
    }
}
