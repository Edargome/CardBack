using CardBack.Application.Auth;
using CardBack.Application.Ports;
using CardBack.Domain.Entities;
using System.Security.Claims;

namespace CardBack.Application.Auth;

public sealed class AuthService
{
    private readonly IUserRepository _users;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _tokens;

    public AuthService(IUserRepository users, IPasswordHasher passwordHasher, IJwtTokenService tokens)
    {
        _users = users;
        _passwordHasher = passwordHasher;
        _tokens = tokens;
    }

    public async Task<TokenResponse> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var username = (request.Username ?? string.Empty).Trim().ToLowerInvariant();
        var password = request.Password ?? string.Empty;

        var user = await _users.FindByUsernameAsync(username, ct);
        if (user is null || !user.IsActive)
            throw new UnauthorizedAccessException("Invalid credentials.");

        if (!_passwordHasher.Verify(password, user.PasswordHash))
            throw new UnauthorizedAccessException("Invalid credentials.");

        return await IssueTokensAndPersistRefreshAsync(user, ct);
    }

    public async Task<TokenResponse> RefreshAsync(RefreshRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.AccessToken) || string.IsNullOrWhiteSpace(request.RefreshToken))
            throw new UnauthorizedAccessException("Invalid tokens.");

        ClaimsPrincipal principal;
        try
        {
            principal = _tokens.GetPrincipalFromExpiredToken(request.AccessToken);
        }
        catch
        {
            throw new UnauthorizedAccessException("Invalid access token.");
        }

        var userIdStr = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value
                     ?? principal.FindFirst("sub")?.Value;

        if (!Guid.TryParse(userIdStr, out var userId))
            throw new UnauthorizedAccessException("Invalid token subject.");

        var user = await _users.FindByIdAsync(userId, ct);
        if (user is null || !user.IsActive)
            throw new UnauthorizedAccessException("Invalid tokens.");

        // Validar refresh contra hash guardado + expiración
        var incomingHash = _tokens.HashToken(request.RefreshToken);

        if (string.IsNullOrWhiteSpace(user.RefreshTokenHash) ||
            user.RefreshTokenExpiresAt is null ||
            user.RefreshTokenExpiresAt <= DateTimeOffset.UtcNow ||
            !FixedTimeEquals(user.RefreshTokenHash, incomingHash))
        {
            // opcional: invalidar refresh en caso de intento sospechoso
            user.ClearRefreshToken();
            await _users.UpdateAsync(user, ct);

            throw new UnauthorizedAccessException("Invalid refresh token.");
        }

        // Rotación: emitir refresh nuevo y reemplazar el anterior
        return await IssueTokensAndPersistRefreshAsync(user, ct);
    }

    private async Task<TokenResponse> IssueTokensAndPersistRefreshAsync(User user, CancellationToken ct)
    {
        var accessToken = _tokens.CreateAccessToken(user);
        var refreshToken = _tokens.CreateRefreshToken();

        var refreshHash = _tokens.HashToken(refreshToken);
        var refreshExpiresAt = DateTimeOffset.UtcNow.AddDays(7); // valor por defecto; puedes parametrizar

        user.SetRefreshToken(refreshHash, refreshExpiresAt);
        await _users.UpdateAsync(user, ct);

        return new TokenResponse(accessToken, refreshToken);
    }

    // Comparación en “tiempo constante” para evitar timing attacks simples
    private static bool FixedTimeEquals(string a, string b)
    {
        if (a.Length != b.Length) return false;

        var diff = 0;
        for (var i = 0; i < a.Length; i++)
            diff |= a[i] ^ b[i];

        return diff == 0;
    }
}
