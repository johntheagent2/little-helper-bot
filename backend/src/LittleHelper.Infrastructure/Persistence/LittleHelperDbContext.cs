using LittleHelper.Domain.CycleTracking;
using LittleHelper.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace LittleHelper.Infrastructure.Persistence;

public sealed class LittleHelperDbContext : DbContext
{
    public LittleHelperDbContext(DbContextOptions<LittleHelperDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<CycleLog> CycleLogs => Set<CycleLog>();

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

        modelBuilder.Entity<CycleLog>(entity =>
        {
            entity.ToTable("CycleLogs");
            entity.HasKey(c => c.Id);
            entity.Property(c => c.StartDate).IsRequired();
            entity.HasIndex(c => new { c.UserId, c.StartDate });
        });
    }
}
