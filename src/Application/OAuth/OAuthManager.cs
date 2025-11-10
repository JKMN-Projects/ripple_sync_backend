using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Configuration;
using RippleSync.Application.Common.Security;
using RippleSync.Application.Integrations;
using RippleSync.Application.Platforms;
using RippleSync.Domain.Platforms;
using System.Text.Json;

namespace RippleSync.Application.OAuth;

public class OAuthManager(
    IConfiguration configuration,
    HybridCache cache,
    IOAuthSecurer oauthSecurer,
    IPlatformFactory platformFactory,
    IntegrationManager integrationManager)
{

    /// <summary>
    /// Get the Auth url of the platform integration
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="platform"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public async Task<string> GetAuthorizationUrl(Guid userId, Platform platform, CancellationToken cancellationToken = default)
    {
        var soMePlatform = platformFactory.Create(platform);

        var (state, codeVerifier, codeChallenge) = oauthSecurer.GetOAuthStateAndCodes();

        var redirectUri = configuration.GetSection("OAuth")["RedirectUrl"]
                ?? throw new InvalidOperationException("No Redirect found");

        var authConfigs = new AuthorizationConfiguration(redirectUri, state, codeChallenge);

        var authUrl = soMePlatform.GetAuthorizationUrl(authConfigs);

        await SaveToCache(state, new OAuthStateData(userId, (int)platform, codeVerifier), cancellationToken);

        return authUrl;
    }

    /// <summary>
    /// Fetches Token from platform integration and stores it.
    /// </summary>
    /// <param name="state"></param>
    /// <param name="code"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public async Task StoreToken(string state, string code, CancellationToken cancellationToken = default)
    {
        var authData = await LoadFromCache(state, cancellationToken);
        await cache.RemoveAsync($"oauth:{state}", cancellationToken);

        var platform = (Platform)authData.PlatformId;
        var soMePlatform = platformFactory.Create(platform);

        var redirectUri = configuration.GetSection("OAuth")["RedirectUrl"]
            ?? throw new InvalidOperationException("No Redirect found");

        var tokenConfigs = new TokenAccessConfiguration(redirectUri, code, authData.CodeVerifier);

        var tokenRequest = soMePlatform.GetTokenRequest(tokenConfigs);

        using var httpClient = new HttpClient();

        TokenResponse? tokenRes = null;

        HttpResponseMessage response;
        string content = string.Empty;
        try
        {
            response = await httpClient.SendAsync(tokenRequest, cancellationToken);

            content = await response.Content.ReadAsStringAsync(cancellationToken);

            response.EnsureSuccessStatusCode();

            tokenRes = JsonSerializer.Deserialize<TokenResponse>(content);
        }
        catch (Exception e)
        {
            throw new InvalidCastException($"Failed to acquire token, content: {content}", e);
        }

        if (tokenRes == null || string.IsNullOrWhiteSpace(tokenRes?.AccessToken))
            throw new InvalidOperationException("Recieved token was null");

        await integrationManager.CreateIntegrationWithEncryptionAsync(
                authData.UserId, authData.PlatformId,
                tokenRes, cancellationToken);

    }

    private async Task SaveToCache(string state, OAuthStateData oauthData, CancellationToken cancellationToken = default)
    {
        await cache.SetAsync(
            $"oauth:{state}",
            oauthData,
            new HybridCacheEntryOptions
            {
                Expiration = TimeSpan.FromMinutes(10)
            }, cancellationToken: cancellationToken);

    }


    private async Task<OAuthStateData> LoadFromCache(string state, CancellationToken cancellationToken = default)
    {
        return await cache.GetOrCreateAsync(
                $"oauth:{state}",
                cancel => new ValueTask<OAuthStateData?>((OAuthStateData?)null), cancellationToken: cancellationToken) // Returns null if not found
                    ?? throw new InvalidOperationException("Invalid or expired state");
    }
}
