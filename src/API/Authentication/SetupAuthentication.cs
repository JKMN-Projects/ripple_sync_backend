using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using RippleSync.Infrastructure.Security;
using System.Text;

namespace RippleSync.API.Authentication;

public static class SetupAuthentication
{
    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, JwtOptions jwtOptions)
    {
        services.AddAuthentication(opt =>
        {
            opt.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            opt.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            opt.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        }).AddJwtBearer(options =>
        {
            string? secret = jwtOptions.Key;
            byte[] key = Encoding.ASCII.GetBytes(secret!);

            options.IncludeErrorDetails = true;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ClockSkew = TimeSpan.Zero,
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidIssuer = jwtOptions.Url,
                ValidAudience = jwtOptions.Url,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key)
            };
        });

        // Authorization
        AuthorizationPolicy defaultPolicy = new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme)
            .Build();
        services.AddAuthorizationBuilder()
            .SetFallbackPolicy(defaultPolicy)
            .SetDefaultPolicy(defaultPolicy);

        return services;
    }
}