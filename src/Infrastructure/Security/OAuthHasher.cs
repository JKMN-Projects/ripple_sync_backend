using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace RippleSync.Infrastructure.Security;
internal class OAuthHasher
{
    public (string State, string CodeVerifier, string CodeChallenge) GetOAuthStateAndCodes()
    {
        string state = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        string codeVerifier = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');

        byte[] challengeBytes = SHA256.HashData(Encoding.UTF8.GetBytes(codeVerifier));
        string codeChallenge = Convert.ToBase64String(challengeBytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');

        return (state, codeVerifier, codeChallenge);
    }
}
