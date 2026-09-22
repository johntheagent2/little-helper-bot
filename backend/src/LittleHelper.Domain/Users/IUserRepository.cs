namespace LittleHelper.Domain.Users;

// Port. Implemented by LittleHelper.Infrastructure — nothing in this project
// knows SQLite or EF Core exist.
public interface IUserRepository
{
    Task<User?> FindByPlatformIdAsync(PlatformType platform, string platformUserId, CancellationToken cancellationToken = default);

    Task AddAsync(User user, CancellationToken cancellationToken = default);
}
