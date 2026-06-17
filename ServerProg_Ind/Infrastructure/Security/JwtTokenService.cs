using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using ServerProg_Ind.Contracts;
using ServerProg_Ind.Domain;

namespace ServerProg_Ind.Infrastructure.Security;

public sealed class AuthenticationOptions
{
    public string Issuer { get; init; } = string.Empty;
    public string Audience { get; init; } = string.Empty;
    public string SigningKey { get; init; } = string.Empty;
}

public interface IJwtTokenService
{
    AuthResponse CreateToken(User user, TimeSpan lifetime);
    SymmetricSecurityKey CreateSecurityKey();
}

public sealed class JwtTokenService(IOptions<AuthenticationOptions> options) : IJwtTokenService
{
    private readonly AuthenticationOptions _options = options.Value;

    public AuthResponse CreateToken(User user, TimeSpan lifetime)
    {
        var expiresAtUtc = DateTime.UtcNow.Add(lifetime);
        var credentials = new SigningCredentials(CreateSecurityKey(), SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims:
            [
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim("name", user.DisplayName),
                new Claim("handle", user.Handle)
            ],
            expires: expiresAtUtc,
            signingCredentials: credentials);

        var encoded = new JwtSecurityTokenHandler().WriteToken(token);
        return new AuthResponse(encoded, expiresAtUtc, new UserProfileDto(user.Id, user.DisplayName, user.Email, user.Handle));
    }

    public SymmetricSecurityKey CreateSecurityKey()
    {
        return new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey));
    }
}
