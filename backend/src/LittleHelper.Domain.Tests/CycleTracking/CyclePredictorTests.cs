using LittleHelper.Domain.CycleTracking;

namespace LittleHelper.Domain.Tests.CycleTracking;

public class CyclePredictorTests
{
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly DateOnly BaseDate = new(2026, 1, 1);

    private static CycleLog Log(int dayOffset) => CycleLog.Create(UserId, BaseDate.AddDays(dayOffset), null);

    [Fact]
    public void GivenNoLogs_WhenPredicting_ThenReturnsNull()
    {
        var logs = Array.Empty<CycleLog>();

        var prediction = CyclePredictor.Predict(logs);

        Assert.Null(prediction);
    }

    [Fact]
    public void GivenSingleLog_WhenPredicting_ThenUsesDefaultTwentyEightDayCycle()
    {
        var logs = new[] { Log(0) };

        var prediction = CyclePredictor.Predict(logs);

        Assert.NotNull(prediction);
        Assert.Equal(28, prediction!.AverageCycleLengthDays);
        Assert.Equal(BaseDate.AddDays(28), prediction.NextPeriodDate);
        Assert.Equal(BaseDate.AddDays(28 - 14 - 5), prediction.FertileWindowStart);
        Assert.Equal(BaseDate.AddDays(28 - 14 + 1), prediction.FertileWindowEnd);
    }

    [Fact]
    public void GivenTwoPlausibleGapLogs_WhenPredicting_ThenAveragesToThatGap()
    {
        var logs = new[] { Log(0), Log(32) };

        var prediction = CyclePredictor.Predict(logs);

        Assert.Equal(32, prediction!.AverageCycleLengthDays);
        Assert.Equal(BaseDate.AddDays(32 + 32), prediction.NextPeriodDate);
    }

    [Fact]
    public void GivenMultiplePlausibleGaps_WhenPredicting_ThenAveragesAllOfThem()
    {
        // Gaps of 28 and 32 average to 30.
        var logs = new[] { Log(0), Log(28), Log(60) };

        var prediction = CyclePredictor.Predict(logs);

        Assert.Equal(30, prediction!.AverageCycleLengthDays);
    }

    [Fact]
    public void GivenOnlyImplausiblyShortGap_WhenPredicting_ThenFallsBackToDefault()
    {
        var logs = new[] { Log(0), Log(5) };

        var prediction = CyclePredictor.Predict(logs);

        Assert.Equal(28, prediction!.AverageCycleLengthDays);
    }

    [Fact]
    public void GivenMixOfPlausibleAndImplausibleGaps_WhenPredicting_ThenOnlyPlausibleGapsCount()
    {
        // 5-day gap is a data-entry mistake and excluded; only the 30-day gap counts.
        var logs = new[] { Log(0), Log(5), Log(35) };

        var prediction = CyclePredictor.Predict(logs);

        Assert.Equal(30, prediction!.AverageCycleLengthDays);
    }

    [Fact]
    public void GivenMoreThanSixCycles_WhenPredicting_ThenOnlyLastSixLogsAreConsidered()
    {
        // log0->log1 gap of 20 sits outside the 6-log window and must be ignored;
        // if it counted, the average would round to 28 instead of 30.
        var logs = new[]
        {
            Log(0), Log(20), Log(50), Log(80), Log(110), Log(140), Log(170), Log(200),
        };

        var prediction = CyclePredictor.Predict(logs);

        Assert.Equal(30, prediction!.AverageCycleLengthDays);
        Assert.Equal(BaseDate.AddDays(200 + 30), prediction.NextPeriodDate);
    }
}
