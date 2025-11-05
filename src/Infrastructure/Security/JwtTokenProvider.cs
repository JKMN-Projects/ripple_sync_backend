using Microsoft.Extensions.Options;
using RippleSync.Application.Common.Security;
using RippleSync.Domain.Users;
using System.Security.Claims;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;

namespace RippleSync.Infrastructure.Security;

public sealed class JwtTokenProvider(
    IOptionsMonitor<JwtOptions> optionsMonitor,
    TimeProvider timeProvider) : IAuthenticationTokenProvider
{
    private JwtOptions Options => optionsMonitor.CurrentValue;

    public Task<AuthenticationToken> GenerateTokenAsync(User user, CancellationToken cancellationToken = default)
    {
        byte[] tokenKey = Encoding.UTF8.GetBytes(Options.Key);
        IEnumerable<Claim> claims = BuildUserClaims(user);
        DateTimeOffset expireDate = timeProvider.GetUtcNow().AddMinutes(Options.ValidityInMinutes);
        SecurityTokenDescriptor securityTokenDescriptor = new()
        {
            Subject = new ClaimsIdentity(claims),
            Audience = Options.Url,
            Issuer = Options.Url,
            Expires = expireDate.UtcDateTime,
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(tokenKey), SecurityAlgorithms.HmacSha256)
        };
        JwtSecurityTokenHandler jwtSecurityTokenHandler = new();
        SecurityToken securityToken = jwtSecurityTokenHandler.CreateToken(securityTokenDescriptor);
        string token = jwtSecurityTokenHandler.WriteToken(securityToken);
        long expiresInMilliSeconds = expireDate.ToUnixTimeMilliseconds();
        AuthenticationToken authenticationToken = new(
            AccessToken: token,
            TokenType: "Bearer",
            ExpiresInMilliSeconds: expiresInMilliSeconds,
            Claims: claims);
        return Task.FromResult(authenticationToken);
    }

    private static IEnumerable<Claim> BuildUserClaims(User user)
    {
        yield return new Claim(ClaimTypes.NameIdentifier, user.Id.ToString());
        yield return new Claim(ClaimTypes.Email, user.Email);
    }

    public Task<RefreshToken> GenerateRefreshTokenAsync(User user, CancellationToken cancellationToken = default)
    {
        string token = Guid.NewGuid().ToString();
        DateTimeOffset createdAt = timeProvider.GetUtcNow();
        long expiresAt = createdAt.AddDays(Options.RefreshTokenValidityInDays).ToUnixTimeMilliseconds();
        RefreshToken refreshToken = RefreshToken.Create(token, timeProvider, expiresAt);
        return Task.FromResult(refreshToken);
    }
}
