using CardBack.Application.Auth;
using CardBack.Application.Ports;
using CardBack.Infrastructure.Persistence;
using CardBack.Infrastructure.Security;
using CardBack.Infrastructure.Seed;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Config SQLite
var cs = builder.Configuration.GetConnectionString("Default")
         ?? "Data Source=app.db";

builder.Services.AddDbContext<AppDbContext>(opt => opt.UseSqlite(cs));

// JWT options
var jwtSection = builder.Configuration.GetSection("Jwt");
var jwtOptions = jwtSection.Get<JwtOptions>() ?? new JwtOptions();
builder.Services.AddSingleton(jwtOptions);

// DI - Hexagonal
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddSingleton<IPasswordHasher, PasswordHasherService>();
builder.Services.AddSingleton<IJwtTokenService, JwtTokenService>();

builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<DbSeeder>();

// Auth JWT Bearer (validación para endpoints protegidos si luego agregas más)
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opt =>
    {
        opt.RequireHttpsMetadata = false; // cambia a true en prod si aplica
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

var app = builder.Build();

// Swagger
app.UseSwagger();
app.UseSwaggerUI();

app.UseAuthentication();
app.UseAuthorization();

// Seed usuarios precargados
using (var scope = app.Services.CreateScope())
{
    var seeder = scope.ServiceProvider.GetRequiredService<DbSeeder>();
    await seeder.SeedAsync();
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

app.Run();
