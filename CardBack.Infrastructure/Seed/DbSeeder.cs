using CardBack.Application.Ports;
using CardBack.Domain.Entities;
using CardBack.Infrastructure.Persistence;

namespace CardBack.Infrastructure.Seed;

public sealed class DbSeeder
{
    private readonly AppDbContext _db;
    private readonly IUserRepository _users;
    private readonly IPasswordHasher _hasher;

    public DbSeeder(AppDbContext db, IUserRepository users, IPasswordHasher hasher)
    {
        _db = db;
        _users = users;
        _hasher = hasher;
    }

    public async Task SeedAsync(CancellationToken ct = default)
    {
        // Asegura BD creada (si no usas migraciones)
        await _db.Database.EnsureCreatedAsync(ct);

        if (await _users.AnyUsersAsync(ct))
            return;

        var admin = new User("admin", _hasher.Hash("Admin123*"), isActive: true);
        var user = new User("user", _hasher.Hash("User123*"), isActive: true);

        await _users.AddAsync(admin, ct);
        await _users.AddAsync(user, ct);
    }
}
