using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace SharedKernel;

public sealed class JwtOptions
{
    public string Issuer { get; set; } = "coachapp.local";
    public string SigningKey { get; set; } = "local-development-signing-key-change-me";
    public int AccessTokenMinutes { get; set; } = 60;
    public int RefreshTokenDays { get; set; } = 7;
}

public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string hash);
}

public sealed class PasswordHasher : IPasswordHasher
{
    public string Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, 100_000, HashAlgorithmName.SHA256, 32);
        return $"{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
    }

    public bool Verify(string password, string hash)
    {
        var parts = hash.Split('.');
        if (parts.Length != 2) return false;
        var salt = Convert.FromBase64String(parts[0]);
        var expected = Convert.FromBase64String(parts[1]);
        var actual = Rfc2898DeriveBytes.Pbkdf2(password, salt, 100_000, HashAlgorithmName.SHA256, 32);
        return CryptographicOperations.FixedTimeEquals(actual, expected);
    }
}

public interface IJwtTokenService
{
    string CreateToken(AppUser user, JwtOptions options, IClock clock);
    ClaimsPrincipal? Validate(string token, JwtOptions options, IClock clock);
}

public sealed class JwtTokenService : IJwtTokenService
{
    public string CreateToken(AppUser user, JwtOptions options, IClock clock)
    {
        var header = Base64Url(JsonSerializer.SerializeToUtf8Bytes(new { alg = "HS256", typ = "JWT" }));
        var payload = Base64Url(JsonSerializer.SerializeToUtf8Bytes(new Dictionary<string, object?>
        {
            ["iss"] = options.Issuer,
            ["sub"] = user.Id,
            ["email"] = user.Email,
            ["name"] = user.DisplayName,
            ["role"] = user.Role,
            ["institute_id"] = user.InstituteId,
            ["exp"] = new DateTimeOffset(clock.UtcNow.AddMinutes(options.AccessTokenMinutes)).ToUnixTimeSeconds()
        }));
        var signature = Sign($"{header}.{payload}", options.SigningKey);
        return $"{header}.{payload}.{signature}";
    }

    public ClaimsPrincipal? Validate(string token, JwtOptions options, IClock clock)
    {
        var parts = token.Split('.');
        if (parts.Length != 3) return null;
        if (!CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(parts[2]), Encoding.UTF8.GetBytes(Sign($"{parts[0]}.{parts[1]}", options.SigningKey)))) return null;

        using var doc = JsonDocument.Parse(Base64UrlDecode(parts[1]));
        var root = doc.RootElement;
        if (root.GetProperty("iss").GetString() != options.Issuer) return null;
        if (root.GetProperty("exp").GetInt64() < new DateTimeOffset(clock.UtcNow).ToUnixTimeSeconds()) return null;

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, root.GetProperty("sub").GetString() ?? string.Empty),
            new(ClaimTypes.Email, root.GetProperty("email").GetString() ?? string.Empty),
            new(ClaimTypes.Name, root.GetProperty("name").GetString() ?? string.Empty),
            new(ClaimTypes.Role, root.GetProperty("role").GetString() ?? string.Empty)
        };
        if (root.TryGetProperty("institute_id", out var instituteId) && instituteId.ValueKind == JsonValueKind.String)
        {
            claims.Add(new Claim("institute_id", instituteId.GetString() ?? string.Empty));
        }

        return new ClaimsPrincipal(new ClaimsIdentity(claims, "CoachJwt"));
    }

    private static string Sign(string value, string key)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(key));
        return Base64Url(hmac.ComputeHash(Encoding.UTF8.GetBytes(value)));
    }

    private static string Base64Url(byte[] bytes) => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static byte[] Base64UrlDecode(string value)
    {
        var padded = value.Replace('-', '+').Replace('_', '/');
        padded += new string('=', (4 - padded.Length % 4) % 4);
        return Convert.FromBase64String(padded);
    }
}
