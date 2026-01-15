namespace CardBack.Application.Auth;

public sealed record LoginRequest(string Username, string Password);

public sealed record RefreshRequest(string AccessToken, string RefreshToken);

public sealed record TokenResponse(string AccessToken, string RefreshToken);
