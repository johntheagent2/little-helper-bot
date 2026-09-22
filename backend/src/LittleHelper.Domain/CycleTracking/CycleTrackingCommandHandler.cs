using System.Globalization;
using LittleHelper.Domain.Messaging;

namespace LittleHelper.Domain.CycleTracking;

// Owns everything about the cycle-tracking commands — parsing, validation,
// and reply formatting. MessageHandler only knows this handler exists, not
// what a cycle prediction is.
public sealed class CycleTrackingCommandHandler : ICommandHandler
{
    private const string DateFormat = "yyyy-MM-dd";
    private const int DefaultHistoryCount = 3;
    private const int MaxHistoryCount = 12;

    private readonly CycleTrackingService _cycleTracking;

    public CycleTrackingCommandHandler(CycleTrackingService cycleTracking)
    {
        _cycleTracking = cycleTracking;
    }

    public string HelpText =>
        "/logcycle [2026-09-01 [2026-09-06]] — log a period's start (defaults to today if omitted) and optional end date\n" +
        "/cycle — show your current prediction\n" +
        $"/cyclehistory [count] — show your last N logged cycles (1-{MaxHistoryCount}, default {DefaultHistoryCount})\n" +
        "/editcycle 2026-09-01 2026-09-03 — fix a wrong start date (the wrong date, then the correct one)\n" +
        "/deletecycle 2026-09-01 — remove the entry logged with that start date\n" +
        "/undo — remove the most recently logged entry";

    public bool CanHandle(string command) =>
        command is "/logcycle" or "/cycle" or "/predict" or "/cyclehistory" or "/editcycle" or "/deletecycle" or "/undo";

    public Task<string> HandleAsync(Guid userId, string[] tokens, CancellationToken cancellationToken = default) =>
        tokens[0].ToLowerInvariant() switch
        {
            "/logcycle" => HandleLogCycleAsync(userId, tokens, cancellationToken),
            "/cyclehistory" => HandleCycleHistoryAsync(userId, tokens, cancellationToken),
            "/editcycle" => HandleEditCycleAsync(userId, tokens, cancellationToken),
            "/deletecycle" => HandleDeleteCycleAsync(userId, tokens, cancellationToken),
            "/undo" => HandleUndoAsync(userId, cancellationToken),
            _ => HandlePredictAsync(userId, cancellationToken),
        };

    private async Task<string> HandleLogCycleAsync(Guid userId, string[] tokens, CancellationToken cancellationToken)
    {
        DateOnly startDate;
        if (tokens.Length < 2)
        {
            startDate = DateOnly.FromDateTime(DateTime.UtcNow);
        }
        else if (!TryParseDate(tokens[1], out startDate))
        {
            return $"That start date doesn't look right — use {DateFormat}, e.g. /logcycle 2026-09-01";
        }

        DateOnly? endDate = null;
        if (tokens.Length >= 3)
        {
            if (!TryParseDate(tokens[2], out var parsedEndDate))
            {
                return $"That end date doesn't look right — use {DateFormat}, e.g. /logcycle 2026-09-01 2026-09-06";
            }

            if (parsedEndDate < startDate)
            {
                return "End date can't be before the start date.";
            }

            endDate = parsedEndDate;
        }

        var prediction = await _cycleTracking.LogCycleAsync(userId, startDate, endDate, cancellationToken);
        return $"Logged period starting {Format(startDate)}.\n{FormatPrediction(prediction)}";
    }

    private async Task<string> HandlePredictAsync(Guid userId, CancellationToken cancellationToken)
    {
        var prediction = await _cycleTracking.GetPredictionAsync(userId, cancellationToken);
        return prediction is null
            ? "No cycle logged yet — use /logcycle 2026-09-01 to log your last period's start date."
            : FormatPrediction(prediction);
    }

    private async Task<string> HandleCycleHistoryAsync(Guid userId, string[] tokens, CancellationToken cancellationToken)
    {
        var count = DefaultHistoryCount;
        if (tokens.Length >= 2)
        {
            if (!int.TryParse(tokens[1], NumberStyles.None, CultureInfo.InvariantCulture, out var requested) || requested < 1)
            {
                return $"Usage: /cyclehistory [count]  (1-{MaxHistoryCount}, default {DefaultHistoryCount})";
            }

            count = Math.Min(requested, MaxHistoryCount);
        }

        var logs = await _cycleTracking.GetRecentCyclesAsync(userId, count, cancellationToken);
        if (logs.Count == 0)
        {
            return "No cycles logged yet.";
        }

        var lines = new List<string> { $"Latest {logs.Count} logged cycle{(logs.Count == 1 ? "" : "s")}:" };
        for (var i = 0; i < logs.Count; i++)
        {
            var log = logs[i];
            var range = log.EndDate is { } endDate ? $"{Format(log.StartDate)} to {Format(endDate)}" : Format(log.StartDate);
            lines.Add($"{i + 1}. {range}");
        }

        return string.Join('\n', lines);
    }

    private async Task<string> HandleEditCycleAsync(Guid userId, string[] tokens, CancellationToken cancellationToken)
    {
        if (tokens.Length < 3 || !TryParseDate(tokens[1], out var oldStartDate) || !TryParseDate(tokens[2], out var newStartDate))
        {
            return "Usage: /editcycle 2026-09-01 2026-09-03  (the date you logged by mistake, then the correct one)";
        }

        var result = await _cycleTracking.EditCycleAsync(userId, oldStartDate, newStartDate, cancellationToken);
        return result.Outcome switch
        {
            CycleMutationOutcome.NotFound => $"No entry logged on {Format(oldStartDate)}.",
            CycleMutationOutcome.InvalidDateRange => "That new start date is after the entry's logged end date.",
            _ => $"Updated {Format(oldStartDate)} to {Format(newStartDate)}.\n{FormatPredictionOrEmpty(result.Prediction)}",
        };
    }

    private async Task<string> HandleDeleteCycleAsync(Guid userId, string[] tokens, CancellationToken cancellationToken)
    {
        if (tokens.Length < 2 || !TryParseDate(tokens[1], out var startDate))
        {
            return "Usage: /deletecycle 2026-09-01";
        }

        var result = await _cycleTracking.DeleteCycleAsync(userId, startDate, cancellationToken);
        return result.Outcome == CycleMutationOutcome.NotFound
            ? $"No entry logged on {Format(startDate)}."
            : $"Deleted the entry logged on {Format(startDate)}.\n{FormatPredictionOrEmpty(result.Prediction)}";
    }

    private async Task<string> HandleUndoAsync(Guid userId, CancellationToken cancellationToken)
    {
        var result = await _cycleTracking.UndoLastAsync(userId, cancellationToken);
        return result.Outcome == CycleMutationOutcome.NotFound
            ? "Nothing to undo — no cycle logs yet."
            : $"Removed the entry logged on {Format(result.AffectedStartDate!.Value)}.\n{FormatPredictionOrEmpty(result.Prediction)}";
    }

    private static string FormatPrediction(CyclePrediction prediction) =>
        $"Next period expected: {Format(prediction.NextPeriodDate)}\n" +
        $"Fertile window: {Format(prediction.FertileWindowStart)} to {Format(prediction.FertileWindowEnd)}\n" +
        $"Average cycle length: {prediction.AverageCycleLengthDays} days";

    private static string FormatPredictionOrEmpty(CyclePrediction? prediction) =>
        prediction is null ? "No cycle logs left." : FormatPrediction(prediction);

    private static bool TryParseDate(string text, out DateOnly date) =>
        DateOnly.TryParseExact(text, DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out date);

    private static string Format(DateOnly date) => date.ToString(DateFormat, CultureInfo.InvariantCulture);
}
