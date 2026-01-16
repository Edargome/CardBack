using CardBack.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CardBack.Infrastructure.Persistence;

public sealed class AppDbContext : DbContext
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Card> Cards => Set<Card>();
    public DbSet<Transaction> Transactions => Set<Transaction>();

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

        var card = modelBuilder.Entity<Card>();
        card.ToTable("Cards");

        card.HasKey(x => x.Id);

        card.Property(x => x.UserId).IsRequired();

        card.Property(x => x.Brand)
            .HasMaxLength(30)
            .IsRequired();

        card.Property(x => x.Last4)
            .HasMaxLength(4)
            .IsRequired();

        card.Property(x => x.Token)
            .HasMaxLength(200)
            .IsRequired();

        card.Property(x => x.Nickname)
            .HasMaxLength(80)
            .IsRequired(false);

        card.Property(x => x.IsActive).IsRequired();

        card.Property(x => x.CreatedAt).IsRequired();

        // Índices útiles
        card.HasIndex(x => new { x.UserId, x.IsActive });
        card.HasIndex(x => x.Token).IsUnique();

        var tx = modelBuilder.Entity<Transaction>();
        tx.ToTable("Transactions");

        tx.HasKey(x => x.Id);

        tx.Property(x => x.UserId).IsRequired();
        tx.Property(x => x.CardId).IsRequired();

        tx.Property(x => x.Amount)
          .HasColumnType("decimal(18,2)")
          .IsRequired();

        tx.Property(x => x.Currency)
          .HasMaxLength(3)
          .IsRequired();

        tx.Property(x => x.Description)
          .HasMaxLength(200)
          .IsRequired(false);

        tx.Property(x => x.Status)
          .HasConversion<int>()
          .IsRequired();

        tx.Property(x => x.CreatedAt)
          .IsRequired();

        tx.HasIndex(x => new { x.UserId, x.CreatedAt });
        tx.HasIndex(x => new { x.CardId, x.CreatedAt });

    }
}
