using LittleHelper.Domain.CycleTracking;
using Moq;

namespace LittleHelper.Domain.Tests.CycleTracking;

public class CycleTrackingCommandHandlerTests
{
    private static readonly Guid UserId = Guid.NewGuid();

    private readonly Mock<ICycleLogRepository> _repository = new();
    private readonly CycleTrackingCommandHandler _handler;

    public CycleTrackingCommandHandlerTests()
    {
        _handler = new CycleTrackingCommandHandler(new CycleTrackingService(_repository.Object));
    }

    [Theory]
    [InlineData("/logcycle")]
    [InlineData("/cycle")]
    [InlineData("/cyclehistory")]
    [InlineData("/editcycle")]
    [InlineData("/deletecycle")]
    [InlineData("/undo")]
    public void GivenKnownCommand_WhenCheckingCanHandle_ThenReturnsTrue(string command)
    {
        Assert.True(_handler.CanHandle(command));
    }

    [Fact]
    public void GivenUnknownCommand_WhenCheckingCanHandle_ThenReturnsFalse()
    {
        Assert.False(_handler.CanHandle("/weather"));
    }

    [Fact]
    public async Task GivenNoDateArgument_WhenLoggingCycle_ThenDefaultsStartDateToToday()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        _repository.Setup(r => r.GetByUserIdAsync(UserId, It.IsAny<CancellationToken>())).ReturnsAsync(new[] { CycleLog.Create(UserId, today, null) });

        var reply = await _handler.HandleAsync(UserId, new[] { "/logcycle" });

        _repository.Verify(r => r.AddAsync(It.Is<CycleLog>(l => l.StartDate == today), It.IsAny<CancellationToken>()), Times.Once);
        Assert.Contains($"Logged period starting {today:yyyy-MM-dd}.", reply);
    }

    [Fact]
    public async Task GivenUnparsableStartDate_WhenLoggingCycle_ThenReturnsErrorWithoutSavingAnything()
    {
        var reply = await _handler.HandleAsync(UserId, new[] { "/logcycle", "not-a-date" });

        Assert.Contains("start date doesn't look right", reply);
        _repository.Verify(r => r.AddAsync(It.IsAny<CycleLog>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GivenEndDateBeforeStartDate_WhenLoggingCycle_ThenReturnsErrorWithoutSavingAnything()
    {
        var reply = await _handler.HandleAsync(UserId, new[] { "/logcycle", "2026-09-10", "2026-09-01" });

        Assert.Equal("End date can't be before the start date.", reply);
        _repository.Verify(r => r.AddAsync(It.IsAny<CycleLog>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GivenNoLogsYet_WhenPredicting_ThenTellsUserToLogFirst()
    {
        _repository.Setup(r => r.GetByUserIdAsync(UserId, It.IsAny<CancellationToken>())).ReturnsAsync(Array.Empty<CycleLog>());

        var reply = await _handler.HandleAsync(UserId, new[] { "/cycle" });

        Assert.Contains("No cycle logged yet", reply);
    }

    [Fact]
    public async Task GivenNonNumericCount_WhenRequestingCycleHistory_ThenReturnsUsageMessage()
    {
        var reply = await _handler.HandleAsync(UserId, new[] { "/cyclehistory", "abc" });

        Assert.StartsWith("Usage: /cyclehistory", reply);
    }

    [Fact]
    public async Task GivenNoLogsYet_WhenRequestingCycleHistory_ThenReturnsNoCyclesMessage()
    {
        _repository.Setup(r => r.GetByUserIdAsync(UserId, It.IsAny<CancellationToken>())).ReturnsAsync(Array.Empty<CycleLog>());

        var reply = await _handler.HandleAsync(UserId, new[] { "/cyclehistory" });

        Assert.Equal("No cycles logged yet.", reply);
    }

    [Fact]
    public async Task GivenMissingNewDate_WhenEditingCycle_ThenReturnsUsageMessage()
    {
        var reply = await _handler.HandleAsync(UserId, new[] { "/editcycle", "2026-09-01" });

        Assert.StartsWith("Usage: /editcycle", reply);
        _repository.Verify(r => r.FindByStartDateAsync(It.IsAny<Guid>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GivenNoEntryOnThatDate_WhenDeletingCycle_ThenReturnsNotFoundMessage()
    {
        _repository
            .Setup(r => r.FindByStartDateAsync(UserId, new DateOnly(2026, 9, 1), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CycleLog?)null);

        var reply = await _handler.HandleAsync(UserId, new[] { "/deletecycle", "2026-09-01" });

        Assert.Equal("No entry logged on 2026-09-01.", reply);
    }

    [Fact]
    public async Task GivenNoLogsAtAll_WhenUndoing_ThenReturnsNothingToUndoMessage()
    {
        _repository
            .Setup(r => r.FindMostRecentlyLoggedAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((CycleLog?)null);

        var reply = await _handler.HandleAsync(UserId, new[] { "/undo" });

        Assert.Equal("Nothing to undo — no cycle logs yet.", reply);
    }
}
