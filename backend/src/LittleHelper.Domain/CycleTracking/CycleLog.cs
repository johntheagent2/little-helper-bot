namespace LittleHelper.Domain.CycleTracking;

public sealed class CycleLog
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public DateOnly StartDate { get; private set; }
    public DateOnly? EndDate { get; private set; }
    public DateTimeOffset LoggedAt { get; private set; }

    private CycleLog()
    {
    }

    private CycleLog(Guid id, Guid userId, DateOnly startDate, DateOnly? endDate, DateTimeOffset loggedAt)
    {
        Id = id;
        UserId = userId;
        StartDate = startDate;
        EndDate = endDate;
        LoggedAt = loggedAt;
    }

    public static CycleLog Create(Guid userId, DateOnly startDate, DateOnly? endDate) =>
        new(Guid.NewGuid(), userId, startDate, endDate, DateTimeOffset.UtcNow);

    public void UpdateStartDate(DateOnly newStartDate) => StartDate = newStartDate;
}
