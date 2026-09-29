using System.Collections.Concurrent;
using BLL.Services;
using Microsoft.AspNetCore.Mvc;

namespace EmailAssistant.Controllers;

[ApiController]
[Route("api/auth/gmail")]
public class AuthController : ControllerBase
{
    private static readonly ConcurrentDictionary<string, DateTimeOffset> PendingStates = new();
    private readonly IGmailTokenService _gmailTokenService;

    public AuthController(IGmailTokenService gmailTokenService)
    {
        _gmailTokenService = gmailTokenService;
    }

    [HttpGet("login")]
    public IActionResult Login()
    {
        var state = Guid.NewGuid().ToString("N");
        PendingStates[state] = DateTimeOffset.UtcNow.AddMinutes(10);
        return Redirect(_gmailTokenService.CreateAuthorizationUrl(state));
    }

    [HttpGet("callback")]
    public async Task<IActionResult> Callback(
        [FromQuery] string? code,
        [FromQuery] string? state,
        [FromQuery] string? error,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(error))
        {
            return BadRequest($"Google authorization was cancelled or failed: {error}");
        }

        if (string.IsNullOrWhiteSpace(state) ||
            !PendingStates.TryRemove(state, out var expiresAt) ||
            expiresAt < DateTimeOffset.UtcNow)
        {
            return BadRequest("Invalid or expired OAuth state.");
        }

        if (string.IsNullOrWhiteSpace(code))
        {
            return BadRequest("Google authorization code was not returned.");
        }

        try
        {
            await _gmailTokenService.ExchangeAuthorizationCodeAsync(code, cancellationToken);
            return Content("Gmail authorization successful. You can close this window and fetch emails again.", "text/plain");
        }
        catch (Exception ex)
        {
            return Problem(
                detail: ex.Message,
                title: "Gmail authorization failed",
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }
}
