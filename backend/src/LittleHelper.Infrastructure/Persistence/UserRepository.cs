using LittleHelper.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace LittleHelper.Infrastructure.Persistence;

public sealed class UserRepository : IUserRepository
{
    private readonly LittleHelperDbContext _db;

    public UserRepository(LittleHelperDbContext db)
    {
        _db = db;
    }

    public Task<User?> FindByPlatformIdAsync(PlatformType platform, string platformUserId, CancellationToken cancellationToken = default) =>
        _db.Users.SingleOrDefaultAsync(u => u.Platform == platform && u.PlatformUserId == platformUserId, cancellationToken);

    public async Task AddAsync(User user, CancellationToken cancellationToken = default)
    {
        _db.Users.Add(user);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
