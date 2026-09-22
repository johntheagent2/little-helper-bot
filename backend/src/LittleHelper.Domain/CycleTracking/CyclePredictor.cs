namespace LittleHelper.Domain.CycleTracking;

// Pure domain calculation, no I/O. Estimates the next period, fertile window,
// and average cycle length from a user's logged history.
public static class CyclePredictor
{
    private const int DefaultCycleLengthDays = 28;

    // The luteal phase (ovulation -> next period) stays close to 14 days
    // regardless of overall cycle length, unlike the follicular phase which
    // varies a lot person to person — so it's the stable anchor for estimating
    // ovulation date from the next predicted period date.
    private const int LutealPhaseDays = 14;

    // Standard 6-day fertile window: sperm can survive ~5 days pre-ovulation,
    // the egg is viable for ~1 day after.
    private const int FertileWindowDaysBeforeOvulation = 5;
    private const int FertileWindowDaysAfterOvulation = 1;

    // Only the most recent cycles count toward the average, so a stale outlier
    // from a year ago doesn't keep skewing today's prediction.
    private const int MaxCyclesConsidered = 6;

    // Gaps outside this range are almost certainly data-entry mistakes (or a
    // missed/duplicate log) rather than real cycle variation, so they're
    // excluded from the average instead of distorting it.
    private const int MinPlausibleCycleLengthDays = 15;
    private const int MaxPlausibleCycleLengthDays = 45;

    public static CyclePrediction? Predict(IReadOnlyList<CycleLog> logsAscending)
    {
        if (logsAscending.Count == 0)
        {
            return null;
        }

        var lastStartDate = logsAscending[^1].StartDate;
        var averageCycleLengthDays = CalculateAverageCycleLength(logsAscending);

        var nextPeriodDate = lastStartDate.AddDays(averageCycleLengthDays);
        var ovulationDate = nextPeriodDate.AddDays(-LutealPhaseDays);
        var fertileWindowStart = ovulationDate.AddDays(-FertileWindowDaysBeforeOvulation);
        var fertileWindowEnd = ovulationDate.AddDays(FertileWindowDaysAfterOvulation);

        return new CyclePrediction(nextPeriodDate, fertileWindowStart, fertileWindowEnd, averageCycleLengthDays);
    }

    private static int CalculateAverageCycleLength(IReadOnlyList<CycleLog> logsAscending)
    {
        var recent = logsAscending.Count > MaxCyclesConsidered
            ? logsAscending.Skip(logsAscending.Count - MaxCyclesConsidered).ToList()
            : logsAscending;

        var gaps = new List<int>();
        for (var i = 1; i < recent.Count; i++)
        {
            var gapDays = recent[i].StartDate.DayNumber - recent[i - 1].StartDate.DayNumber;
            if (gapDays is >= MinPlausibleCycleLengthDays and <= MaxPlausibleCycleLengthDays)
            {
                gaps.Add(gapDays);
            }
        }

        return gaps.Count == 0 ? DefaultCycleLengthDays : (int)Math.Round(gaps.Average());
    }
}
