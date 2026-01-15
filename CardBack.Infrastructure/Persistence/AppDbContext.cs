using CardBack.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CardBack.Infrastructure.Persistence;

public sealed class AppDbContext : DbContext
{
    public DbSet<User> Users => Set<User>();

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var user = modelBuilder.Entity<User>();
        user.ToTable("Users");

        user.HasKey(x => x.Id);

        user.Property(x => x.Username)
            .HasMaxLength(100)
            .IsRequired();

        user.HasIndex(x => x.Username).IsUnique();

        user.Property(x => x.PasswordHash)
            .HasMaxLength(500)
            .IsRequired();

        user.Property(x => x.IsActive)
            .IsRequired();

        user.Property(x => x.RefreshTokenHash)
            .HasMaxLength(128);

        user.Property(x => x.RefreshTokenExpiresAt);

        base.OnModelCreating(modelBuilder);
    }
}
