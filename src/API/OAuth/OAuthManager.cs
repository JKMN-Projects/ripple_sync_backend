using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Configuration;
using RippleSync.Domain.Platforms;
using RippleSync.Domain.Users;
using System;
using System.Threading;

namespace RippleSync.API.OAuth;

public class OAuthManager
{
    public async Task SaveToCache()
    {
        await cache.SetAsync(
            $"oauth:{state}",
            oauthData,
            new HybridCacheEntryOptions
            {
                Expiration = TimeSpan.FromMinutes(10)
            }, cancellationToken: cancellationToken);

    }
    public async TokenResponse LoadFromCache()
    {

    }

    public async GetPlatformAuthorizationUrl()
    {
        //URL of platform OAuth
        Uri? authorizationUrl = null;

        //Save userId and platformId in temp storage using new state generated
        // Storing
        OAuthStateData oauthData = new(User.GetUserId(), platformId, codeVerifier);

        await cache.SetAsync(
            $"oauth:{state}",
            oauthData,
            new HybridCacheEntryOptions
            {
                Expiration = TimeSpan.FromMinutes(10)
            }, cancellationToken: cancellationToken);

        var secrets = configuration.GetSection("Integrations").GetSection(platform.ToString());

        string redirectUri = configuration.GetSection("OAuth")["RedirectUrl"]
            ?? throw new InvalidOperationException("No Redirect found");

        string clientId;
        QueryString queries;

        switch (platform)
        {
            //Provide state and more to OAuth.

            case Platform.X:
                clientId = secrets["ClientId"]
                    ?? throw new InvalidOperationException("No ClientId found for X");

                queries = new QueryString()
                    .Add("response_type", "code")
                    .Add("client_id", clientId)
                    .Add("redirect_uri", redirectUri)
                    .Add("scope", "tweet.read+tweet.write+users.read+offline.access")
                    .Add("state", state)
                    .Add("code_challenge", codeChallenge)
                    .Add("code_challenge_method", "S256");

                authorizationUrl = new Uri("https://x.com/i/oauth2/authorize" + queries.ToUriComponent());
                break;
            case Platform.LinkedIn:
                clientId = secrets["ClientId"]
                    ?? throw new InvalidOperationException("No ClientId found for X");

                queries = new QueryString()
                    .Add("response_type", "code")
                    .Add("client_id", clientId)
                    .Add("redirect_uri", redirectUri)
                    .Add("scope", "w_member_social")
                    .Add("state", state)
                    .Add("code_challenge", codeChallenge)
                    .Add("code_challenge_method", "S256");

                authorizationUrl = new Uri("https://www.linkedin.com/oauth/v2/authorization" + queries.ToUriComponent());
                break;
            case Platform.Facebook:
                break;
            case Platform.Instagram:
                break;
            case Platform.Threads:
                break;
            default:
                return safeResult;
        }

        if (authorizationUrl == null) return safeResult;
    }
}
