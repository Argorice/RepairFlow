using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using RepairFlow.Api.Configuration;
using RepairFlow.Api.Domain.Entities;

namespace RepairFlow.Api.Services;

public sealed record AccessToken(string Value, DateTime ExpiresAt);

public interface ITokenService
{
    AccessToken CreateAccessToken(User user);

    string CreateRefreshToken();

    DateTime RefreshTokenExpiry();
}

public sealed class TokenService : ITokenService
{
    private readonly JwtOptions _options;

    public TokenService(IOptions<JwtOptions> options) => _options = options.Value;

    public AccessToken CreateAccessToken(User user)
    {
        var expiresAt = DateTime.UtcNow.AddMinutes(_options.AccessTokenMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Name, user.FullName),
            new(ClaimTypes.Role, user.Role.ToString())
        };

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(_options.Key)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: expiresAt,
            signingCredentials: credentials);

        return new AccessToken(new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }

    /// <summary>Refresh-токен — просто криптостойкая случайная строка: он проверяется по базе, а не по подписи.</summary>
    public string CreateRefreshToken() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));

    public DateTime RefreshTokenExpiry() => DateTime.UtcNow.AddDays(_options.RefreshTokenDays);
}
