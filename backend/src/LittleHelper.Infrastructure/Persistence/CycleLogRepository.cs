using LittleHelper.Domain.CycleTracking;
using Microsoft.EntityFrameworkCore;

namespace LittleHelper.Infrastructure.Persistence;

public sealed class CycleLogRepository : ICycleLogRepository
{
    private readonly LittleHelperDbContext _db;

    public CycleLogRepository(LittleHelperDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<CycleLog>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default) =>
        await _db.CycleLogs
            .Where(c => c.UserId == userId)
            .OrderBy(c => c.StartDate)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(CycleLog log, CancellationToken cancellationToken = default)
    {
        _db.CycleLogs.Add(log);
        await _db.SaveChangesAsync(cancellationToken);
    }

    // SQLite's EF Core provider can't translate ORDER BY on DateTimeOffset
    // (LoggedAt) into SQL, so these two sort client-side after fetching the
    // (small, personal-scale) candidate set.

    public async Task<CycleLog?> FindByStartDateAsync(Guid userId, DateOnly startDate, CancellationToken cancellationToken = default)
    {
        var matches = await _db.CycleLogs
            .Where(c => c.UserId == userId && c.StartDate == startDate)
            .ToListAsync(cancellationToken);
        return matches.OrderByDescending(c => c.LoggedAt).FirstOrDefault();
    }

    public async Task<CycleLog?> FindMostRecentlyLoggedAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var logs = await _db.CycleLogs
            .Where(c => c.UserId == userId)
            .ToListAsync(cancellationToken);
        return logs.OrderByDescending(c => c.LoggedAt).FirstOrDefault();
    }

    public async Task DeleteAsync(CycleLog log, CancellationToken cancellationToken = default)
    {
        _db.CycleLogs.Remove(log);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(CycleLog log, CancellationToken cancellationToken = default)
    {
        _db.CycleLogs.Update(log);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
