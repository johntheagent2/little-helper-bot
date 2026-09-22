using LittleHelper.Domain.Messaging;
using LittleHelper.Domain.Users;
using Moq;

namespace LittleHelper.Domain.Tests.Messaging;

public class MessageHandlerTests
{
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<ICommandHandler> _commandHandler = new();
    private readonly MessageHandler _handler;

    public MessageHandlerTests()
    {
        _handler = new MessageHandler(_users.Object, new[] { _commandHandler.Object });
    }

    private static IncomingMessage Message(string text, string? displayName = "Alex") =>
        new(PlatformType.Telegram, "12345", displayName, text);

    [Fact]
    public async Task GivenUnknownPlatformUser_WhenHandlingMessage_ThenCreatesNewUser()
    {
        _users
            .Setup(u => u.FindByPlatformIdAsync(PlatformType.Telegram, "12345", It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);
        _commandHandler.Setup(h => h.CanHandle(It.IsAny<string>())).Returns(false);

        await _handler.HandleAsync(Message("hello"));

        _users.Verify(u => u.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GivenKnownPlatformUser_WhenHandlingMessage_ThenDoesNotCreateAnotherUser()
    {
        var existingUser = User.Create(PlatformType.Telegram, "12345", "Alex");
        _users
            .Setup(u => u.FindByPlatformIdAsync(PlatformType.Telegram, "12345", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingUser);
        _commandHandler.Setup(h => h.CanHandle(It.IsAny<string>())).Returns(false);

        await _handler.HandleAsync(Message("hello"));

        _users.Verify(u => u.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GivenBlankText_WhenHandlingMessage_ThenAsksUserToSaySomething()
    {
        _users
            .Setup(u => u.FindByPlatformIdAsync(It.IsAny<PlatformType>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(User.Create(PlatformType.Telegram, "12345", "Alex"));

        var reply = await _handler.HandleAsync(Message("   "));

        Assert.Equal("Say something and I'll try to help.", reply);
    }

    [Theory]
    [InlineData("/help")]
    [InlineData("/start")]
    public async Task GivenHelpCommand_WhenHandlingMessage_ThenListsEveryHandlersHelpText(string command)
    {
        _users
            .Setup(u => u.FindByPlatformIdAsync(It.IsAny<PlatformType>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(User.Create(PlatformType.Telegram, "12345", "Alex"));
        _commandHandler.Setup(h => h.HelpText).Returns("/foo — does foo things");

        var reply = await _handler.HandleAsync(Message(command));

        Assert.Contains("/foo — does foo things", reply);
        Assert.Contains("/help — show this message", reply);
    }

    [Fact]
    public async Task GivenCommandAMatchingHandlerClaims_WhenHandlingMessage_ThenDelegatesToThatHandler()
    {
        var user = User.Create(PlatformType.Telegram, "12345", "Alex");
        _users
            .Setup(u => u.FindByPlatformIdAsync(It.IsAny<PlatformType>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _commandHandler.Setup(h => h.CanHandle("/foo")).Returns(true);
        _commandHandler
            .Setup(h => h.HandleAsync(user.Id, It.Is<string[]>(t => t[0] == "/foo"), It.IsAny<CancellationToken>()))
            .ReturnsAsync("handled");

        var reply = await _handler.HandleAsync(Message("/foo bar"));

        Assert.Equal("handled", reply);
    }

    [Fact]
    public async Task GivenNoHandlerClaimsTheCommand_WhenHandlingMessage_ThenReturnsPlaceholderReplyWithDisplayName()
    {
        _users
            .Setup(u => u.FindByPlatformIdAsync(It.IsAny<PlatformType>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(User.Create(PlatformType.Telegram, "12345", "Alex"));
        _commandHandler.Setup(h => h.CanHandle(It.IsAny<string>())).Returns(false);

        var reply = await _handler.HandleAsync(Message("/unknown"));

        Assert.Equal("Got it, Alex. Try /help to see what I can do.", reply);
    }

    [Fact]
    public async Task GivenNoHandlerClaimsTheCommandAndUserHasNoDisplayName_WhenHandlingMessage_ThenFallsBackToThere()
    {
        _users
            .Setup(u => u.FindByPlatformIdAsync(It.IsAny<PlatformType>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(User.Create(PlatformType.Telegram, "12345", null));
        _commandHandler.Setup(h => h.CanHandle(It.IsAny<string>())).Returns(false);

        var reply = await _handler.HandleAsync(Message("/unknown", displayName: null));

        Assert.Equal("Got it, there. Try /help to see what I can do.", reply);
    }
}
