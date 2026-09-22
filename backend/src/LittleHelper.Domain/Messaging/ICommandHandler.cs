namespace LittleHelper.Domain.Messaging;

// Port: a feature module owns parsing, validating, and replying to whatever
// slash commands it recognizes. MessageHandler routes to whichever handler
// claims the command's leading token — it never knows what a command means.
public interface ICommandHandler
{
    // command is already lowercased, e.g. "/logcycle".
    bool CanHandle(string command);

    Task<string> HandleAsync(Guid userId, string[] tokens, CancellationToken cancellationToken = default);

    // One or more lines describing this handler's commands, shown by /help.
    string HelpText { get; }
}
