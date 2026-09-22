using LittleHelper.Domain.Users;

namespace LittleHelper.Domain.Messaging;

// Every inbound message goes through here: resolve-or-create the sending
// user, then route on the leading command token to whichever registered
// ICommandHandler claims it. Unrecognized text (or no leading slash at all)
// falls back to a placeholder reply — no free-text intent parsing.
public sealed class MessageHandler
{
    private readonly IUserRepository _users;
    private readonly IReadOnlyList<ICommandHandler> _commandHandlers;

    public MessageHandler(IUserRepository users, IEnumerable<ICommandHandler> commandHandlers)
    {
        _users = users;
        _commandHandlers = commandHandlers.ToList();
    }

    public async Task<string> HandleAsync(IncomingMessage message, CancellationToken cancellationToken = default)
    {
        var user = await _users.FindByPlatformIdAsync(message.Platform, message.PlatformUserId, cancellationToken);
        if (user is null)
        {
            user = User.Create(message.Platform, message.PlatformUserId, message.DisplayName);
            await _users.AddAsync(user, cancellationToken);
        }

        var tokens = message.Text.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (tokens.Length == 0)
        {
            return "Say something and I'll try to help.";
        }

        var command = tokens[0].ToLowerInvariant();
        if (command is "/help" or "/start")
        {
            return BuildHelpText();
        }

        var handler = _commandHandlers.FirstOrDefault(h => h.CanHandle(command));
        return handler is null
            ? $"Got it, {user.DisplayName ?? "there"}. Try /help to see what I can do."
            : await handler.HandleAsync(user.Id, tokens, cancellationToken);
    }

    private string BuildHelpText()
    {
        var lines = new List<string> { "Commands:" };
        lines.AddRange(_commandHandlers.Select(h => h.HelpText));
        lines.Add("/help — show this message");
        return string.Join('\n', lines);
    }
}
