namespace LittleHelper.Domain.CycleTracking;

public sealed record CyclePrediction(
    DateOnly NextPeriodDate,
    DateOnly FertileWindowStart,
    DateOnly FertileWindowEnd,
    int AverageCycleLengthDays);
