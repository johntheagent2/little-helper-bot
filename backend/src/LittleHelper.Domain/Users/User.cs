namespace LittleHelper.Domain.Users;

public sealed class User
{
    public Guid Id { get; private set; }
    public PlatformType Platform { get; private set; }
    public string PlatformUserId { get; private set; } = string.Empty;
    public string? DisplayName { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private User()
    {
    }

    private User(Guid id, PlatformType platform, string platformUserId, string? displayName, DateTimeOffset createdAt)
    {
        Id = id;
        Platform = platform;
        PlatformUserId = platformUserId;
        DisplayName = displayName;
        CreatedAt = createdAt;
    }

    public static User Create(PlatformType platform, string platformUserId, string? displayName) =>
        new(Guid.NewGuid(), platform, platformUserId, displayName, DateTimeOffset.UtcNow);
}
