using LittleHelper.Domain.Users;

namespace LittleHelper.Domain.Messaging;

// The one thing every inbound message goes through right now: resolve-or-create
// the sending user, then acknowledge. No command routing/feature dispatch yet —
// that's follow-up work once a real command set is defined. This exists so the
// bot's full relay path (Telegram -> bot -> here -> bot -> Telegram) can be
// exercised end to end before any feature logic is built.
public sealed class MessageHandler
{
    private readonly IUserRepository _users;

    public MessageHandler(IUserRepository users)
    {
        _users = users;
    }

    public async Task<string> HandleAsync(IncomingMessage message, CancellationToken cancellationToken = default)
    {
        var user = await _users.FindByPlatformIdAsync(message.Platform, message.PlatformUserId, cancellationToken);
        if (user is null)
        {
            user = User.Create(message.Platform, message.PlatformUserId, message.DisplayName);
            await _users.AddAsync(user, cancellationToken);
        }

        return $"Got it, {user.DisplayName ?? "there"}. No commands are wired up yet.";
    }
}
