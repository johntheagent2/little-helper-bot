namespace LittleHelper.Domain.CycleTracking;

// Port. Implemented by LittleHelper.Infrastructure — nothing in this project
// knows SQLite or EF Core exist.
public interface ICycleLogRepository
{
    // Ordered ascending by StartDate — CyclePredictor relies on that ordering.
    Task<IReadOnlyList<CycleLog>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    Task AddAsync(CycleLog log, CancellationToken cancellationToken = default);

    // If more than one entry shares that start date (a duplicate log), the
    // most recently logged one is returned.
    Task<CycleLog?> FindByStartDateAsync(Guid userId, DateOnly startDate, CancellationToken cancellationToken = default);

    Task<CycleLog?> FindMostRecentlyLoggedAsync(Guid userId, CancellationToken cancellationToken = default);

    Task DeleteAsync(CycleLog log, CancellationToken cancellationToken = default);

    Task UpdateAsync(CycleLog log, CancellationToken cancellationToken = default);
}
