using Microsoft.EntityFrameworkCore;
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
        var dataSource = _db.Database.GetDbConnection().DataSource;
        Console.WriteLine($"[DB] DataSource = {dataSource}");

        // 1) Crea esquema si no existe (sin migraciones)
        await _db.Database.EnsureCreatedAsync(ct);

        // 2) Si falta alguna tabla clave (BD vieja), resetea en DEV
        var missing = await GetMissingTablesAsync(new[] { "Users", "Cards", "Transactions" }, ct);
        if (missing.Count > 0)
        {
            Console.WriteLine($"[DB] Missing tables: {string.Join(", ", missing)}");
            Console.WriteLine("[DB] Resetting database (DEV) to match current model...");

            await _db.Database.EnsureDeletedAsync(ct);
            await _db.Database.EnsureCreatedAsync(ct);
        }

        await PrintTablesAsync(ct);

        // =========================
        // SEED: USERS
        // =========================
        var admin = await EnsureUserAsync("admin", "Admin123*", ct);
        var user = await EnsureUserAsync("user", "User123*", ct);
        var edwin = await EnsureUserAsync("edwin", "Edwin123*", ct);
        var sofia = await EnsureUserAsync("sofia", "Sofia123*", ct);

        // =========================
        // SEED: CARDS
        // =========================
        // Nota: Para evitar duplicados, validamos por (UserId + Token) o (UserId + Last4 + Brand)
        var adminVisa = await EnsureCardAsync(admin.Id, "Visa", "1111", "tok_admin_visa_1111", "Admin Visa", ct);
        var adminMc = await EnsureCardAsync(admin.Id, "MasterCard", "2222", "tok_admin_mc_2222", "Admin MC", ct);

        var userVisa = await EnsureCardAsync(user.Id, "Visa", "3333", "tok_user_visa_3333", "Personal", ct);

        var edwinVisa = await EnsureCardAsync(edwin.Id, "Visa", "4444", "tok_edwin_visa_4444", "Edwin Principal", ct);
        var edwinAmex = await EnsureCardAsync(edwin.Id, "Amex", "5555", "tok_edwin_amex_5555", "Edwin Business", ct);

        var sofiaVisa = await EnsureCardAsync(sofia.Id, "Visa", "7777", "tok_sofia_visa_7777", "Sofia Personal", ct);

        // =========================
        // SEED: TRANSACTIONS
        // =========================
        // Para evitar duplicados, validamos por (CardId + Amount + Currency + Description + CreatedAtDate)
        await EnsureTransactionAsync(admin.Id, adminVisa.Id, 150000m, "COP", "Pago suscripción", TransactionStatus.Approved, ct);
        await EnsureTransactionAsync(admin.Id, adminVisa.Id, 50000m, "COP", "Pago streaming", TransactionStatus.Approved, ct);
        await EnsureTransactionAsync(admin.Id, adminMc.Id, 3500000m, "COP", "Compra equipo", TransactionStatus.Declined, ct);

        await EnsureTransactionAsync(user.Id, userVisa.Id, 22000m, "COP", "Recarga", TransactionStatus.Approved, ct);

        await EnsureTransactionAsync(edwin.Id, edwinVisa.Id, 980000m, "COP", "Pago hosting", TransactionStatus.Approved, ct);
        await EnsureTransactionAsync(edwin.Id, edwinAmex.Id, 1200000m, "COP", "Pago proveedor", TransactionStatus.Approved, ct);

        await EnsureTransactionAsync(sofia.Id, sofiaVisa.Id, 76000m, "COP", "Pago mercado", TransactionStatus.Approved, ct);

        Console.WriteLine("[SEED] Done.");
    }

    // ---------------------------
    // Users
    // ---------------------------
    private async Task<User> EnsureUserAsync(string username, string plainPassword, CancellationToken ct)
    {
        var existing = await _users.FindByUsernameAsync(username, ct);
        if (existing is not null) return existing;

        var u = new User(username, _hasher.Hash(plainPassword), isActive: true);
        await _users.AddAsync(u, ct);

        Console.WriteLine($"[SEED] User created: {username} / {plainPassword}");
        return u;
    }

    // ---------------------------
    // Cards
    // ---------------------------
    private async Task<Card> EnsureCardAsync(Guid userId, string brand, string last4, string token, string nickname, CancellationToken ct)
    {
        // Buscar si ya existe una card similar
        var existing = await _db.Set<Card>()
            .Where(c => c.UserId == userId && (c.Token == token || (c.Brand == brand && c.Last4 == last4)))
            .FirstOrDefaultAsync(ct);

        if (existing is not null) return existing;

        var card = new Card(userId, brand, last4, token, nickname);
        _db.Set<Card>().Add(card);
        await _db.SaveChangesAsync(ct);

        Console.WriteLine($"[SEED] Card created: {brand} ****{last4} ({nickname}) for user {userId}");
        return card;
    }

    // ---------------------------
    // Transactions
    // ---------------------------
    private async Task<Transaction> EnsureTransactionAsync(
    Guid userId,
    Guid cardId,
    decimal amount,
    string currency,
    string description,
    TransactionStatus status,
    CancellationToken ct)
    {
        var cur = (currency ?? "COP").Trim().ToUpperInvariant();
        var desc = (description ?? string.Empty).Trim();
        var roundedAmount = decimal.Round(amount, 2);

        // 1) Intentar encontrar una transacción existente con los mismos campos principales
        //    (UserId + CardId + Amount + Currency + Description)
        //    Nota: para seed en DEV, esto es suficiente para evitar duplicados.
        await using var conn = _db.Database.GetDbConnection();
        await conn.OpenAsync(ct);

        await using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = @"
SELECT ""Id""
FROM ""Transactions""
WHERE ""UserId"" = $u
  AND ""CardId"" = $c
  AND ""Amount"" = $amt
  AND ""Currency"" = $cur
  AND ""Description"" = $desc
LIMIT 1;";

            var pU = cmd.CreateParameter(); pU.ParameterName = "$u"; pU.Value = userId.ToString();
            var pC = cmd.CreateParameter(); pC.ParameterName = "$c"; pC.Value = cardId.ToString();
            var pA = cmd.CreateParameter(); pA.ParameterName = "$amt"; pA.Value = roundedAmount;
            var pCur = cmd.CreateParameter(); pCur.ParameterName = "$cur"; pCur.Value = cur;
            var pD = cmd.CreateParameter(); pD.ParameterName = "$desc"; pD.Value = desc;

            cmd.Parameters.Add(pU);
            cmd.Parameters.Add(pC);
            cmd.Parameters.Add(pA);
            cmd.Parameters.Add(pCur);
            cmd.Parameters.Add(pD);

            var existingIdObj = await cmd.ExecuteScalarAsync(ct);
            if (existingIdObj is not null)
            {
                var existingId = ParseGuid(existingIdObj);
                var existing = await _db.Set<Transaction>().FindAsync(new object[] { existingId }, ct);
                if (existing is not null) return existing;
            }
        }

        // 2) Si no existe, insertar
        var tx = new Transaction(
            userId: userId,
            cardId: cardId,
            amount: roundedAmount,
            currency: cur,
            description: desc,
            status: status
        );

        _db.Set<Transaction>().Add(tx);
        await _db.SaveChangesAsync(ct);

        Console.WriteLine($"[SEED] Tx created: {roundedAmount} {cur} - {desc} ({status})");
        return tx;
    }

    private static Guid ParseGuid(object value)
    {
        // SQLite puede devolver string, byte[] u otros. Cubrimos los casos comunes.
        if (value is Guid g) return g;

        if (value is string s && Guid.TryParse(s, out var gs))
            return gs;

        if (value is byte[] bytes && bytes.Length == 16)
            return new Guid(bytes);

        // fallback
        return Guid.Parse(value.ToString()!);
    }


    // ---------------------------
    // DB helpers (SQLite tables)
    // ---------------------------
    private async Task<List<string>> GetMissingTablesAsync(IEnumerable<string> required, CancellationToken ct)
    {
        var existing = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        await using var conn = _db.Database.GetDbConnection();
        await conn.OpenAsync(ct);

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT name FROM sqlite_master WHERE type='table' ORDER BY name;";
        await using var reader = await cmd.ExecuteReaderAsync(ct);

        while (await reader.ReadAsync(ct))
        {
            existing.Add(reader.GetString(0));
        }

        return required.Where(t => !existing.Contains(t)).ToList();
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
                Console.WriteLine(" - " + reader.GetString(0));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DB] Failed to list tables: {ex.Message}");
        }
    }
}
