using LittleHelper.Domain.Messaging;
using LittleHelper.Domain.Users;
using Microsoft.AspNetCore.Mvc;

namespace LittleHelper.Api.Controllers;

// Contract: openapi/messages.yaml. Mirrors bot/src/modules/messaging-gateway
// /domain/BackendGateway.ts's BackendMessageRequest/BackendMessageResponse.
[ApiController]
[Route("messages")]
public sealed class MessagesController : ControllerBase
{
    private readonly MessageHandler _handler;

    public MessagesController(MessageHandler handler)
    {
        _handler = handler;
    }

    [HttpPost]
    public async Task<IActionResult> Post(BackendMessageRequest request, CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<PlatformType>(request.Platform, ignoreCase: true, out var platform))
        {
            return BadRequest(new { error = $"Unknown platform: {request.Platform}" });
        }

        var message = new IncomingMessage(platform, request.PlatformUserId, request.DisplayName, request.Text);
        var replyText = await _handler.HandleAsync(message, cancellationToken);
        return Ok(new BackendMessageResponse(replyText));
    }
}

public sealed record BackendMessageRequest(string Platform, string PlatformUserId, string? DisplayName, string Text);

public sealed record BackendMessageResponse(string Text);
