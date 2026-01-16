using CardBack.Application.Auth;
using CardBack.Application.Ports;
using CardBack.Infrastructure.Persistence;
using CardBack.Infrastructure.Security;
using CardBack.Infrastructure.Seed;
using CardBack.Application.Transactions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Security.Claims;
using CardBack.Application.Cards;
using Microsoft.EntityFrameworkCore;
using System.IO;

var builder = WebApplication.CreateBuilder(args);

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Ingresa: Bearer {tu_token}"
    });

    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// Config SQLite
var cs = builder.Configuration.GetConnectionString("Default")
         ?? "Data Source=app.db";

builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseSqlite(cs));

// JWT options
var jwtSection = builder.Configuration.GetSection("Jwt");
var jwtOptions = builder.Configuration.GetSection("Jwt").Get<JwtOptions>()
                 ?? throw new InvalidOperationException("Missing Jwt options");

builder.Services.AddSingleton(jwtOptions);

// DI - Hexagonal
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddSingleton<IPasswordHasher, PasswordHasherService>();
builder.Services.AddSingleton<IJwtTokenService, JwtTokenService>();

builder.Services.AddScoped<ICardRepository, CardRepository>();
builder.Services.AddScoped<CardService>();

builder.Services.AddScoped<ITransactionRepository, TransactionRepository>();
builder.Services.AddScoped<TransactionService>();

builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<DbSeeder>();

// Auth JWT Bearer (validación para endpoints protegidos si luego agregas más)
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opt =>
    {
        opt.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,

            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,

            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Key)),

            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

//CORS
builder.Services.AddCors(opt =>
{
    opt.AddPolicy("frontend", p =>
        p.WithOrigins("http://localhost:5173")
         .AllowAnyHeader()
         .AllowAnyMethod());
});

var app = builder.Build();

// Swagger
app.UseSwagger();
app.UseSwaggerUI();

app.UseAuthentication();
app.UseAuthorization();

app.UseCors("frontend");

// Seed usuarios precargados
using (var scope = app.Services.CreateScope())
{
    var seeder = scope.ServiceProvider.GetRequiredService<DbSeeder>();
    await seeder.SeedAsync();
}


static Guid GetUserId(HttpContext ctx)
{
    var id = ctx.User.FindFirstValue(ClaimTypes.NameIdentifier)
          ?? ctx.User.FindFirstValue("sub");

    return Guid.TryParse(id, out var guid)
        ? guid
        : throw new UnauthorizedAccessException("Invalid token subject.");
}

// Endpoints requeridos
app.MapPost("/auth/login", async (LoginRequest req, AuthService auth, CancellationToken ct) =>
{
    try
    {
        var tokens = await auth.LoginAsync(req, ct);
        return Results.Ok(tokens);
    }
    catch (UnauthorizedAccessException ex)
    {
        return Results.Unauthorized();
    }
})
.WithName("Login")
.WithTags("Auth");

app.MapPost("/auth/refresh", async (RefreshRequest req, AuthService auth, CancellationToken ct) =>
{
    try
    {
        var tokens = await auth.RefreshAsync(req, ct);
        return Results.Ok(tokens);
    }
    catch (UnauthorizedAccessException)
    {
        return Results.Unauthorized();
    }
})
.WithName("Refresh")
.WithTags("Auth");

var cardsGroup = app.MapGroup("/cards")
    .WithTags("Cards")
    .RequireAuthorization(); // <- JWT obligatorio

cardsGroup.MapGet("/", async (HttpContext ctx, CardService svc, CancellationToken ct) =>
{
    var userId = GetUserId(ctx);
    var cards = await svc.ListMineAsync(userId, ct);
    return Results.Ok(cards);
})
.WithName("ListMyCards");

cardsGroup.MapPost("/", async (HttpContext ctx, CardBack.Application.Cards.CreateCardRequest req, CardService svc, CancellationToken ct) =>
{
    var userId = GetUserId(ctx);
    var created = await svc.CreateAsync(userId, req, ct);
    return Results.Created($"/cards/{created.Id}", created);
})
.WithName("CreateCard");

cardsGroup.MapDelete("/{id:guid}", async (HttpContext ctx, Guid id, CardService svc, CancellationToken ct) =>
{
    var userId = GetUserId(ctx);
    await svc.DeleteAsync(userId, id, ct);
    return Results.NoContent();
})
.WithName("DeleteCard");

var txGroup = app.MapGroup("/transactions")
    .WithTags("Transactions")
    .RequireAuthorization();

txGroup.MapPost("/", async (HttpContext ctx, CreateTransactionRequest req, TransactionService svc, CancellationToken ct) =>
{
    var userId = GetUserId(ctx); // el helper que ya tienes para NameIdentifier/sub
    var created = await svc.PayAsync(userId, req, ct);
    return Results.Created($"/transactions/{created.Id}", created);
})
.WithName("CreateTransaction");

txGroup.MapGet("/", async (HttpContext ctx, string? from, string? to, TransactionService svc, CancellationToken ct) =>
{
    var userId = GetUserId(ctx);

    DateTimeOffset? fromDt = DateTimeOffset.TryParse(from, out var f) ? f : null;
    DateTimeOffset? toDt = DateTimeOffset.TryParse(to, out var t) ? t : null;

    var list = await svc.HistoryAsync(userId, fromDt, toDt, ct);
    return Results.Ok(list);
})
.WithName("ListTransactions");

// opcional: por tarjeta
app.MapGet("/cards/{cardId:guid}/transactions", async (HttpContext ctx, Guid cardId, TransactionService svc, CancellationToken ct) =>
{
    var userId = GetUserId(ctx);
    var list = await svc.HistoryByCardAsync(userId, cardId, ct);
    return Results.Ok(list);
})
.WithTags("Transactions")
.RequireAuthorization()
.WithName("ListTransactionsByCard");


app.Run();
