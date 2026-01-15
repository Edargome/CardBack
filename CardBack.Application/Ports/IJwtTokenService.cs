using CardBack.Domain.Entities;
using System.Security.Claims;

namespace CardBack.Application.Ports;

public interface IJwtTokenService
{
    string CreateAccessToken(User user);
    string CreateRefreshToken();

    // Guardamos hash del refresh en BD
    string HashToken(string token);

    // Para refresh: extraer usuario del access token aunque esté expirado
    ClaimsPrincipal GetPrincipalFromExpiredToken(string accessToken);
}
