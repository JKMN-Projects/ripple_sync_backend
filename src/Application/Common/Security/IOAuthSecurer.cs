namespace RippleSync.Application.Common.Security;

public interface IOAuthSecurer
{
    Task EncryptToken(string token);
    (string State, string CodeVerifier, string CodeChallenge) GetOAuthStateAndCodes();
}