
using RippleSync.Application.Common.Security;
using RippleSync.Domain.Users;
using System.Globalization;
using System.Text;
using System.Text.Json;

namespace RippleSync.Tests.Shared.TestDoubles.Security;
public static class AuthenticationTokenProviderDoubles
{
    public class Dummy : IAuthenticationTokenProvider
    {
        public virtual Task<RefreshToken> GenerateRefreshTokenAsync(User user, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public virtual Task<AuthenticationToken> GenerateTokenAsync(User user, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }

    public class Composite : IAuthenticationTokenProvider
    {
        private readonly IAuthenticationTokenProvider first;
        private readonly IAuthenticationTokenProvider second;
        public Composite(IAuthenticationTokenProvider first, IAuthenticationTokenProvider second)
        {
            this.first = first;
            this.second = second;
        }
        public Task<AuthenticationToken> GenerateTokenAsync(User user, CancellationToken cancellationToken = default)
        {
            try
            {
                return first.GenerateTokenAsync(user, cancellationToken);
            }
            catch (NotImplementedException)
            {
                return second.GenerateTokenAsync(user, cancellationToken);
            }
        }
        public Task<RefreshToken> GenerateRefreshTokenAsync(User user, CancellationToken cancellationToken = default)
        {
            try
            {
                return first.GenerateRefreshTokenAsync(user, cancellationToken);
            }
            catch (NotImplementedException)
            {
                return second.GenerateRefreshTokenAsync(user, cancellationToken);
            }
        }
    }

    public static class Spies
    {
        public class GenerateTokenSpy : Dummy
        {
            public User? LastReceivedUser { get; private set; }
            public int InvokationCount { get; private set; }

            private readonly IAuthenticationTokenProvider spiedProvider;
            public GenerateTokenSpy(IAuthenticationTokenProvider spiedProvider)
            {
                this.spiedProvider = spiedProvider;
            }

            public override async Task<AuthenticationToken> GenerateTokenAsync(User user, CancellationToken cancellationToken = default)
            {
                LastReceivedUser = user;
                InvokationCount++;
                return await spiedProvider.GenerateTokenAsync(user, cancellationToken);
            }
        }
    }

    public static class Fakes
    {
        public class SimpleTokenProvider : IAuthenticationTokenProvider
        {
            public Task<AuthenticationToken> GenerateTokenAsync(User user, CancellationToken cancellationToken = default)
            {
                AuthenticationToken fakeToken = new(
                    AccessToken: Convert.ToBase64String(Guid.NewGuid().ToByteArray()),
                    TokenType: "Fake",
                    ExpiresInMilliSeconds: 3600);
                return Task.FromResult(fakeToken);
            }
            public Task<RefreshToken> GenerateRefreshTokenAsync(User user, CancellationToken cancellationToken = default)
            {
                return Task.FromResult(RefreshToken.Create(
                    token: Guid.NewGuid().ToString(),
                    createdAt: DateTime.UtcNow,
                    expiresAt: DateTime.UtcNow.AddDays(30)));
            }
        }

        /// <summary>
        /// This fake is used to generate JWT like tokens in JSON format for testing purposes. The value can be deserializable from the webtokens to verify correctness.
        /// </summary>
        public class JsonSerializedTokenProvider : IAuthenticationTokenProvider
        {
            private readonly string signature;
            public string TokenType { get; } = "JsonSerialized JWT";

            public JsonSerializedTokenProvider(string? signature = null)
            {
                this.signature = signature ?? Convert.ToBase64String(Guid.NewGuid().ToByteArray());
            }

            public Task<AuthenticationToken> GenerateTokenAsync(User user, CancellationToken cancellationToken = default)
            {
                object header = new
                {
                    alg = "JSON",
                    typ = "JWT"
                };
                DateTimeOffset expiration = DateTimeOffset.UtcNow.AddHours(1);
                int expiresInSeconds = (int)(expiration - DateTimeOffset.UtcNow).TotalSeconds;
                object payload = new
                {
                    sub = user.Id,
                    email = user.Email,
                    exp = expiration.ToString("o")
                };
                string headerJson = JsonSerializer.Serialize(header);
                string headerBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(headerJson));
                string payloadJson = JsonSerializer.Serialize(payload);
                string payloadBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(payloadJson));
                string token = $"{headerBase64}.{payloadBase64}.{signature}";

                AuthenticationToken fakeToken = new(
                    AccessToken: token,
                    TokenType: TokenType,
                    ExpiresInMilliSeconds: expiresInSeconds);
                return Task.FromResult(fakeToken);
            }

            public bool IsValidToken(string token)
            {
                string[] parts = token.Split('.');
                if (parts.Length != 3)
                {
                    return false;
                }
                try
                {
                    string headerJson = Encoding.UTF8.GetString(Convert.FromBase64String(parts[0]));
                    string payloadJson = Encoding.UTF8.GetString(Convert.FromBase64String(parts[1]));
                    JsonDocument.Parse(headerJson);
                    JsonDocument.Parse(payloadJson);

                    string tokenSignature = parts[2];
                    return tokenSignature == signature;
                }
                catch
                {
                    return false;
                }
            }

            public static (IEnumerable<(string Key, string Value)> Claims, DateTimeOffset Expiration) DecodeToken(string token)
            {
                string[] parts = token.Split('.');
                if (parts.Length != 3)
                {
                    throw new ArgumentException("Invalid token format", nameof(token));
                }
                string payloadJson = Encoding.UTF8.GetString(Convert.FromBase64String(parts[1]));
                using JsonDocument payloadDoc = JsonDocument.Parse(payloadJson);
                List<(string Key, string Value)> claims = [];
                foreach (JsonProperty property in payloadDoc.RootElement.EnumerateObject())
                {
                    claims.Add((property.Name, property.Value.ToString() ?? ""));
                }
                string expString = payloadDoc.RootElement.GetProperty("exp").GetString() ?? throw new ArgumentException("Token does not contain expiration claim", nameof(token));
                DateTimeOffset expiration = DateTimeOffset.Parse(expString, CultureInfo.InvariantCulture);
                return (claims, expiration);
            }

            public Task<RefreshToken> GenerateRefreshTokenAsync(User user, CancellationToken cancellationToken = default)
            {
                return Task.FromResult(RefreshToken.Create(
                    token: Convert.ToBase64String(Guid.NewGuid().ToByteArray()),
                    createdAt: DateTime.UtcNow,
                    expiresAt: DateTime.UtcNow.AddDays(30)));
            }
        }
    }
}
