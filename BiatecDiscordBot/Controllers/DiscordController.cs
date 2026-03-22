using BiatecDiscordBot.Models.DTOs;
using BiatecDiscordBot.Services;
using Microsoft.AspNetCore.Mvc;

namespace BiatecDiscordBot.Controllers;

/// <summary>
/// API endpoints for Discord operations like fetching guild members and sending messages.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class DiscordController : ControllerBase
{
    private readonly IDiscordBotService _botService;
    private readonly ILogger<DiscordController> _logger;

    public DiscordController(IDiscordBotService botService, ILogger<DiscordController> logger)
    {
        _botService = botService;
        _logger = logger;
    }

    /// <summary>
    /// Gets the connection status of the Discord bot.
    /// </summary>
    [HttpGet("status")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public IActionResult GetStatus()
    {
        return Ok(new { Connected = _botService.IsConnected });
    }

    /// <summary>
    /// Gets all users from a specific Discord guild.
    /// </summary>
    /// <param name="serverName">The Discord server name.</param>
    [HttpGet("servers/{serverName}/users")]
    [ProducesResponseType(typeof(IReadOnlyCollection<DiscordUserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetGuildUsers(string serverName)
    {
        var guildId = await _botService.GetGuildIdByServerNameAsync(serverName);
        if (!guildId.HasValue)
        {
            return NotFound(new { Error = $"Server '{serverName}' was not found." });
        }

        var users = await _botService.GetGuildUsersAsync(guildId.Value);
        return Ok(users);
    }

    /// <summary>
    /// Sends a direct message to a specific Discord user.
    /// </summary>
    [HttpPost("messages/send")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SendMessage([FromBody] SendMessageRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
        {
            return BadRequest(new { Error = "Message cannot be empty." });
        }

        var success = await _botService.SendDirectMessageAsync(request.UserId, request.Message);
        if (!success)
        {
            return BadRequest(new { Error = "Failed to send message. User may not exist or DMs may be disabled." });
        }

        return Ok(new { Success = true, Message = "Message sent successfully." });
    }

    /// <summary>
    /// Sends a message to a specific Discord channel.
    /// </summary>
    /// <param name="channelId">The Discord channel ID.</param>
    /// <param name="message">The message content.</param>
    [HttpPost("channels/{channelId}/messages")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SendChannelMessage(ulong channelId, [FromBody] string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return BadRequest(new { Error = "Message cannot be empty." });
        }

        var success = await _botService.SendChannelMessageAsync(channelId, message);
        if (!success)
        {
            return BadRequest(new { Error = "Failed to send message. Channel may not exist." });
        }

        return Ok(new { Success = true });
    }

    /// <summary>
    /// Assigns a Discord role to a user in a guild.
    /// </summary>
    /// <param name="serverName">The server name.</param>
    /// <param name="userId">The user ID.</param>
    /// <param name="roleId">The role ID.</param>
    [HttpPost("servers/{serverName}/users/{userId}/roles/{roleId}")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AssignRole(string serverName, ulong userId, ulong roleId)
    {
        var guildId = await _botService.GetGuildIdByServerNameAsync(serverName);
        if (!guildId.HasValue)
        {
            return NotFound(new { Error = $"Server '{serverName}' was not found." });
        }

        var success = await _botService.AssignRoleAsync(guildId.Value, userId, roleId);
        if (!success)
        {
            return BadRequest(new { Error = "Failed to assign role." });
        }

        return Ok(new { Success = true });
    }

    /// <summary>
    /// Sends a question to a specific user via DM.
    /// </summary>
    /// <param name="userId">The Discord user ID.</param>
    /// <param name="questionId">The question ID to send.</param>
    [HttpPost("users/{userId}/questions/{questionId}")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SendQuestion(ulong userId, int questionId)
    {
        var success = await _botService.SendQuestionAsync(userId, questionId);
        if (!success)
        {
            return BadRequest(new { Error = "Failed to send question." });
        }

        return Ok(new { Success = true });
    }
}
