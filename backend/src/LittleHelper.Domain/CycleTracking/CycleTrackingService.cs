namespace LittleHelper.Domain.CycleTracking;

// Orchestrates logging, correcting, and predicting cycles. No I/O beyond the
// repository port.
public sealed class CycleTrackingService
{
    private readonly ICycleLogRepository _cycleLogs;

    public CycleTrackingService(ICycleLogRepository cycleLogs)
    {
        _cycleLogs = cycleLogs;
    }

    public async Task<CyclePrediction> LogCycleAsync(
        Guid userId,
        DateOnly startDate,
        DateOnly? endDate,
        CancellationToken cancellationToken = default)
    {
        await _cycleLogs.AddAsync(CycleLog.Create(userId, startDate, endDate), cancellationToken);
        return (await GetPredictionAsync(userId, cancellationToken))!;
    }

    public async Task<CyclePrediction?> GetPredictionAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var logs = await _cycleLogs.GetByUserIdAsync(userId, cancellationToken);
        return CyclePredictor.Predict(logs);
    }

    // Most recent first. count is expected to already be clamped by the caller.
    public async Task<IReadOnlyList<CycleLog>> GetRecentCyclesAsync(Guid userId, int count, CancellationToken cancellationToken = default)
    {
        var logs = await _cycleLogs.GetByUserIdAsync(userId, cancellationToken);
        return logs.TakeLast(count).Reverse().ToList();
    }

    public async Task<CycleMutationResult> DeleteCycleAsync(Guid userId, DateOnly startDate, CancellationToken cancellationToken = default)
    {
        var log = await _cycleLogs.FindByStartDateAsync(userId, startDate, cancellationToken);
        if (log is null)
        {
            return new CycleMutationResult(CycleMutationOutcome.NotFound, null, null);
        }

        await _cycleLogs.DeleteAsync(log, cancellationToken);
        var prediction = await GetPredictionAsync(userId, cancellationToken);
        return new CycleMutationResult(CycleMutationOutcome.Success, startDate, prediction);
    }

    public async Task<CycleMutationResult> EditCycleAsync(
        Guid userId,
        DateOnly oldStartDate,
        DateOnly newStartDate,
        CancellationToken cancellationToken = default)
    {
        var log = await _cycleLogs.FindByStartDateAsync(userId, oldStartDate, cancellationToken);
        if (log is null)
        {
            return new CycleMutationResult(CycleMutationOutcome.NotFound, null, null);
        }

        if (log.EndDate is { } endDate && newStartDate > endDate)
        {
            return new CycleMutationResult(CycleMutationOutcome.InvalidDateRange, null, null);
        }

        log.UpdateStartDate(newStartDate);
        await _cycleLogs.UpdateAsync(log, cancellationToken);
        var prediction = await GetPredictionAsync(userId, cancellationToken);
        return new CycleMutationResult(CycleMutationOutcome.Success, newStartDate, prediction);
    }

    public async Task<CycleMutationResult> UndoLastAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var log = await _cycleLogs.FindMostRecentlyLoggedAsync(userId, cancellationToken);
        if (log is null)
        {
            return new CycleMutationResult(CycleMutationOutcome.NotFound, null, null);
        }

        await _cycleLogs.DeleteAsync(log, cancellationToken);
        var prediction = await GetPredictionAsync(userId, cancellationToken);
        return new CycleMutationResult(CycleMutationOutcome.Success, log.StartDate, prediction);
    }
}
