using LittleHelper.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace LittleHelper.Infrastructure.Persistence;

public sealed class LittleHelperDbContext : DbContext
{
    public LittleHelperDbContext(DbContextOptions<LittleHelperDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("Users");
            entity.HasKey(u => u.Id);
            entity.Property(u => u.Platform).HasConversion<string>().IsRequired();
            entity.Property(u => u.PlatformUserId).IsRequired();
            entity.HasIndex(u => new { u.Platform, u.PlatformUserId }).IsUnique();
        });
    }
}
