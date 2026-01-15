namespace CardBack.Infrastructure.Security;

public sealed class JwtOptions
{
    public string Issuer { get; set; } = "CardBack";
    public string Audience { get; set; } = "CardBackClients";
    public string Key { get; set; } = "CHANGE_ME_TO_A_LONG_SECURE_KEY_32+_CHARS";
    public int AccessTokenMinutes { get; set; } = 15;
    public int RefreshTokenDays { get; set; } = 7;
}
