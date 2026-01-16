using CardBack.Application.Ports;
using CardBack.Domain.Entities;
using CardBack.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

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
        // Diagnóstico: confirma cuál archivo DB se está usando
        var dataSource = _db.Database.GetDbConnection().DataSource;
        Console.WriteLine($"[DB] DataSource = {dataSource}");

        // 1) Intentar migrar. Si falla (por no haber migraciones, o falta historial),
        // caer a EnsureCreated para que al menos cree el esquema.
        if ( dataSource == null )
        {
            Console.WriteLine("[DB] Trying MigrateAsync...");
            await _db.Database.MigrateAsync(ct);
        }
        else
        {
            Console.WriteLine("[DB] Falling back to EnsureCreatedAsync...");
            await _db.Database.EnsureCreatedAsync(ct);
        }

        // Imprime tablas reales
        await PrintTablesAsync(ct);

        // Si no existe Cards, no intentamos sembrar tarjetas
        var cardsExists = await TableExistsAsync("Cards", ct);
        Console.WriteLine($"[DB] Cards table exists? {cardsExists}");

        // Seed usuarios
        if (!await _users.AnyUsersAsync(ct))
        {
            Console.WriteLine("[SEED] Inserting users...");

            var admin = new User("admin", _hasher.Hash("Admin123*"), isActive: true);
            var user = new User("user", _hasher.Hash("User123*"), isActive: true);

            var edwin = new User("edwin", _hasher.Hash("Edwin123*"), isActive: true);
            var sofia = new User("sofia", _hasher.Hash("Sofia123*"), isActive: true);

            await _users.AddAsync(admin, ct);
            await _users.AddAsync(user, ct);
            await _users.AddAsync(edwin, ct);
            await _users.AddAsync(sofia, ct);
        }
        else
        {
            Console.WriteLine("[SEED] Users already exist. Skipping user seed.");
        }

        // Seed tarjetas (solo si existe tabla)
        if (!cardsExists)
        {
            Console.WriteLine("[SEED] Cards table missing. Skipping card seed.");
            Console.WriteLine("[SEED] => Ensure your schema includes Cards (migrations or EnsureCreated with Card DbSet).");
            return;
        }

        if (!await _db.Cards.AnyAsync(ct))
        {
            Console.WriteLine("[SEED] Inserting cards...");

            var admin = await _users.FindByUsernameAsync("admin", ct);
            var user = await _users.FindByUsernameAsync("user", ct);
            var edwin = await _users.FindByUsernameAsync("edwin", ct);
            var sofia = await _users.FindByUsernameAsync("sofia", ct);

            var toInsert = new List<Card>();

            if (admin is not null)
            {
                toInsert.Add(new Card(admin.Id, "Visa", "1111", "tok_admin_visa_1111", "Admin Visa"));
                toInsert.Add(new Card(admin.Id, "MasterCard", "2222", "tok_admin_mc_2222", "Admin MC"));
            }

            if (user is not null)
            {
                toInsert.Add(new Card(user.Id, "Visa", "3333", "tok_user_visa_3333", "Personal"));
            }

            if (edwin is not null)
            {
                toInsert.Add(new Card(edwin.Id, "Visa", "4444", "tok_edwin_visa_4444", "Edwin - Principal"));
                toInsert.Add(new Card(edwin.Id, "Amex", "5555", "tok_edwin_amex_5555", "Edwin - Business"));
                toInsert.Add(new Card(edwin.Id, "MasterCard", "6666", "tok_edwin_mc_6666", "Edwin - Backup"));
            }

            if (sofia is not null)
            {
                toInsert.Add(new Card(sofia.Id, "Visa", "7777", "tok_sofia_visa_7777", "Sofia - Personal"));
            }

            _db.Cards.AddRange(toInsert);
            await _db.SaveChangesAsync(ct);
        }
        else
        {
            Console.WriteLine("[SEED] Cards already exist. Skipping card seed.");
        }
    }

    private async Task PrintTablesAsync(CancellationToken ct)
    {
        try
        {
            await using var conn = _db.Database.GetDbConnection();
            await conn.OpenAsync(ct);

            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT name FROM sqlite_master WHERE type='table' ORDER BY name;";

            await using var reader = await cmd.ExecuteReaderAsync(ct);

            Console.WriteLine("[DB] Tables:");
            while (await reader.ReadAsync(ct))
            {
                Console.WriteLine(" - " + reader.GetString(0));
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DB] Failed to list tables: {ex.Message}");
        }
    }

    private async Task<bool> TableExistsAsync(string tableName, CancellationToken ct)
    {
        try
        {
            await using var conn = _db.Database.GetDbConnection();
            await conn.OpenAsync(ct);

            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT 1 FROM sqlite_master WHERE type='table' AND name=$name LIMIT 1;";

            var p = cmd.CreateParameter();
            p.ParameterName = "$name";
            p.Value = tableName;
            cmd.Parameters.Add(p);

            var result = await cmd.ExecuteScalarAsync(ct);
            return result is not null;
        }
        catch
        {
            return false;
        }
    }
}
