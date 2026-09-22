using LittleHelper.Domain.CycleTracking;
using Moq;

namespace LittleHelper.Domain.Tests.CycleTracking;

public class CycleTrackingServiceTests
{
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly DateOnly Date = new(2026, 9, 1);

    private readonly Mock<ICycleLogRepository> _repository = new();
    private readonly CycleTrackingService _service;

    public CycleTrackingServiceTests()
    {
        _service = new CycleTrackingService(_repository.Object);
    }

    [Fact]
    public async Task GivenNewCycle_WhenLoggingIt_ThenAddsLogAndReturnsPrediction()
    {
        _repository
            .Setup(r => r.GetByUserIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { CycleLog.Create(UserId, Date, null) });

        var prediction = await _service.LogCycleAsync(UserId, Date, null);

        _repository.Verify(
            r => r.AddAsync(It.Is<CycleLog>(l => l.UserId == UserId && l.StartDate == Date), It.IsAny<CancellationToken>()),
            Times.Once);
        Assert.NotNull(prediction);
    }

    [Fact]
    public async Task GivenNoLogsForUser_WhenGettingPrediction_ThenReturnsNull()
    {
        _repository
            .Setup(r => r.GetByUserIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<CycleLog>());

        var prediction = await _service.GetPredictionAsync(UserId);

        Assert.Null(prediction);
    }

    [Fact]
    public async Task GivenMoreLogsThanRequested_WhenGettingRecentCycles_ThenReturnsMostRecentFirst()
    {
        var logs = new[]
        {
            CycleLog.Create(UserId, Date, null),
            CycleLog.Create(UserId, Date.AddDays(28), null),
            CycleLog.Create(UserId, Date.AddDays(56), null),
        };
        _repository.Setup(r => r.GetByUserIdAsync(UserId, It.IsAny<CancellationToken>())).ReturnsAsync(logs);

        var recent = await _service.GetRecentCyclesAsync(UserId, 2);

        Assert.Equal(new[] { Date.AddDays(56), Date.AddDays(28) }, recent.Select(l => l.StartDate));
    }

    [Fact]
    public async Task GivenNoLogOnThatDate_WhenDeletingCycle_ThenReturnsNotFound()
    {
        _repository
            .Setup(r => r.FindByStartDateAsync(UserId, Date, It.IsAny<CancellationToken>()))
            .ReturnsAsync((CycleLog?)null);

        var result = await _service.DeleteCycleAsync(UserId, Date);

        Assert.Equal(CycleMutationOutcome.NotFound, result.Outcome);
        _repository.Verify(r => r.DeleteAsync(It.IsAny<CycleLog>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GivenLogOnThatDate_WhenDeletingCycle_ThenDeletesItAndReturnsAffectedDate()
    {
        var log = CycleLog.Create(UserId, Date, null);
        _repository.Setup(r => r.FindByStartDateAsync(UserId, Date, It.IsAny<CancellationToken>())).ReturnsAsync(log);
        _repository.Setup(r => r.GetByUserIdAsync(UserId, It.IsAny<CancellationToken>())).ReturnsAsync(Array.Empty<CycleLog>());

        var result = await _service.DeleteCycleAsync(UserId, Date);

        Assert.Equal(CycleMutationOutcome.Success, result.Outcome);
        Assert.Equal(Date, result.AffectedStartDate);
        _repository.Verify(r => r.DeleteAsync(log, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GivenNoLogOnOldDate_WhenEditingCycle_ThenReturnsNotFound()
    {
        _repository
            .Setup(r => r.FindByStartDateAsync(UserId, Date, It.IsAny<CancellationToken>()))
            .ReturnsAsync((CycleLog?)null);

        var result = await _service.EditCycleAsync(UserId, Date, Date.AddDays(1));

        Assert.Equal(CycleMutationOutcome.NotFound, result.Outcome);
    }

    [Fact]
    public async Task GivenNewStartDateAfterLoggedEndDate_WhenEditingCycle_ThenReturnsInvalidDateRange()
    {
        var log = CycleLog.Create(UserId, Date, Date.AddDays(3));
        _repository.Setup(r => r.FindByStartDateAsync(UserId, Date, It.IsAny<CancellationToken>())).ReturnsAsync(log);

        var result = await _service.EditCycleAsync(UserId, Date, Date.AddDays(5));

        Assert.Equal(CycleMutationOutcome.InvalidDateRange, result.Outcome);
        _repository.Verify(r => r.UpdateAsync(It.IsAny<CycleLog>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GivenValidCorrection_WhenEditingCycle_ThenUpdatesStartDateAndReturnsIt()
    {
        var log = CycleLog.Create(UserId, Date, null);
        var newStartDate = Date.AddDays(1);
        _repository.Setup(r => r.FindByStartDateAsync(UserId, Date, It.IsAny<CancellationToken>())).ReturnsAsync(log);
        _repository.Setup(r => r.GetByUserIdAsync(UserId, It.IsAny<CancellationToken>())).ReturnsAsync(new[] { log });

        var result = await _service.EditCycleAsync(UserId, Date, newStartDate);

        Assert.Equal(CycleMutationOutcome.Success, result.Outcome);
        Assert.Equal(newStartDate, result.AffectedStartDate);
        Assert.Equal(newStartDate, log.StartDate);
        _repository.Verify(r => r.UpdateAsync(log, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GivenNoLogsForUser_WhenUndoingLast_ThenReturnsNotFound()
    {
        _repository
            .Setup(r => r.FindMostRecentlyLoggedAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((CycleLog?)null);

        var result = await _service.UndoLastAsync(UserId);

        Assert.Equal(CycleMutationOutcome.NotFound, result.Outcome);
    }

    [Fact]
    public async Task GivenMostRecentlyLoggedEntry_WhenUndoingLast_ThenDeletesItAndReturnsItsStartDate()
    {
        var log = CycleLog.Create(UserId, Date, null);
        _repository.Setup(r => r.FindMostRecentlyLoggedAsync(UserId, It.IsAny<CancellationToken>())).ReturnsAsync(log);
        _repository.Setup(r => r.GetByUserIdAsync(UserId, It.IsAny<CancellationToken>())).ReturnsAsync(Array.Empty<CycleLog>());

        var result = await _service.UndoLastAsync(UserId);

        Assert.Equal(CycleMutationOutcome.Success, result.Outcome);
        Assert.Equal(Date, result.AffectedStartDate);
        _repository.Verify(r => r.DeleteAsync(log, It.IsAny<CancellationToken>()), Times.Once);
    }
}
