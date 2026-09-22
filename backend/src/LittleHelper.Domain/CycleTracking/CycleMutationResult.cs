namespace LittleHelper.Domain.CycleTracking;

// AffectedStartDate is only populated where the caller couldn't already know
// it up front — e.g. /undo doesn't say which date it's removing.
public sealed record CycleMutationResult(CycleMutationOutcome Outcome, DateOnly? AffectedStartDate, CyclePrediction? Prediction);
