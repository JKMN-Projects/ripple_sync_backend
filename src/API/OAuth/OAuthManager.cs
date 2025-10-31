using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Configuration;
using RippleSync.Application.Common.Security;
using RippleSync.Application.Integrations;
using RippleSync.Application.Platforms;
using RippleSync.Domain.Platforms;
using RippleSync.Domain.Users;
using System;
using System.Text.Json;
using System.Threading;

namespace RippleSync.API.OAuth;

public class OAuthManager(
    IConfiguration configuration,
    HybridCache cache,
    IOAuthSecurer oauthSecurer,
    IPlatformFactory platformFactory,
    IntegrationManager integrationManager)
{
    public async Task<string> GetAuthorizationUrl(Guid userId, Platform platform, CancellationToken cancellationToken = default)
    {
        ISoMePlatform soMePlatform = platformFactory.Create(platform);

        var (state, codeVerifier, codeChallenge) = oauthSecurer.GetOAuthStateAndCodes();

        var secrets = configuration.GetSection("Integrations").GetSection(platform.ToString() ?? "")
                ?? throw new InvalidOperationException("No secrets found for " + platform.ToString());

        string redirectUri = configuration.GetSection("OAuth")["RedirectUrl"]
                ?? throw new InvalidOperationException("No Redirect found");

        string clientId = secrets["ClientId"]
                   ?? throw new InvalidOperationException("No ClientId found for X");

        AuthorizationConfiguration authConfigs = new AuthorizationConfiguration(clientId, redirectUri, state, codeChallenge);

        var authUrl = soMePlatform.GetAuthorizationUrl(authConfigs);

        await this.SaveToCache(state, new OAuthStateData(userId, (int)platform, codeVerifier), cancellationToken);

        return authUrl;
    }

    public async Task StoreToken(string state, string code, CancellationToken cancellationToken = default)
    {
        var authData = await LoadFromCache(state, cancellationToken);
        await cache.RemoveAsync($"oauth:{state}", cancellationToken);
        
        var platform = (Platform)authData.PlatformId;
        ISoMePlatform soMePlatform = platformFactory.Create(platform);

        var secrets = configuration.GetSection("Integrations").GetSection(platform.ToString() ?? "");

        string redirectUri = configuration.GetSection("OAuth")["RedirectUrl"]
            ?? throw new InvalidOperationException("No Redirect found");

        string clientId = secrets["ClientId"]
            ?? throw new InvalidOperationException("No ClientId found for X");

        string clientSecret = secrets["ClientSecret"]
                    ?? throw new InvalidOperationException("No ClientSecret found for X");

        TokenAccessConfiguration tokenConfigs = new TokenAccessConfiguration(clientId, clientSecret, redirectUri, code, authData.CodeVerifier);

        var tokenResponse = await soMePlatform.GetTokenUrlAsync(tokenConfigs, cancellationToken);

        await integrationManager.CreateIntegrationWithEncryptionAsync(
                authData.UserId, authData.PlatformId,
                tokenResponse, cancellationToken);

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
                async cancel => (OAuthStateData?)null, cancellationToken: cancellationToken) // Returns null if not found
                    ?? throw new InvalidOperationException("Invalid or expired state");
    }
}
