using Infrastructure.FakePlatform;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using RippleSync.Application.Common.Queries;
using RippleSync.Application.Common.Repositories;
using RippleSync.Application.Common.Security;
using RippleSync.Application.Platforms;
using RippleSync.Domain.Platforms;
using RippleSync.Infrastructure.IntegrationRepository;
using RippleSync.Infrastructure.PlatformRepository;
using RippleSync.Infrastructure.PostRepository;
using RippleSync.Infrastructure.Security;
using RippleSync.Infrastructure.SoMePlatforms.Facebook;
using RippleSync.Infrastructure.SoMePlatforms.Instagram;
using RippleSync.Infrastructure.SoMePlatforms.LinkedIn;
using RippleSync.Infrastructure.SoMePlatforms.Threads;
using RippleSync.Infrastructure.SoMePlatforms.X;
using RippleSync.Infrastructure.UserRepository;

namespace RippleSync.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString)
    {
        services.AddSingleton<IPasswordHasher, Rfc2898PasswordHasher>();
        services.AddSingleton<IAuthenticationTokenProvider, JwtTokenProvider>();

        services.AddSingleton<IOAuthSecurer, OAuthSecurer>();

        services.AddKeyedSingleton<ISoMePlatform, SoMePlatformLinkedIn>(Platform.LinkedIn);
        services.AddKeyedSingleton<ISoMePlatform, SoMePlatformX>(Platform.X);
        services.AddKeyedSingleton<ISoMePlatform, SoMePlatformFacebook>(Platform.Facebook);
        services.AddKeyedSingleton<ISoMePlatform, SoMePlatformInstagram>(Platform.Instagram);
        services.AddKeyedSingleton<ISoMePlatform, SoMePlatformThreads>(Platform.Threads);
        services.AddKeyedSingleton<ISoMePlatform, SoMePlatformFake>(Platform.FakePlatform);

        services.AddScoped<NpgsqlConnection>(sp => new NpgsqlConnection(connectionString));

        services.AddScoped<IPlatformQueries, InMemoryPlatformRepository>();
        services.AddScoped<IIntegrationQueries, InMemoryIntegrationRepository>();
        services.AddScoped<IPostQueries, InMemoryPostRepository>();

        services.AddScoped<IUserRepository, InMemoryUserRepository>();
        services.AddScoped<IIntegrationRepository, InMemoryIntegrationRepository>();
        services.AddScoped<IPostRepository, InMemoryPostRepository>();

        services.AddSingleton<IEncryptionService>(sp =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            string key = config["Encryption:Key"]
                ?? throw new ArgumentNullException("EncryptionKey");
            return new AesGcmEncryptionService(key);
        });

        return services;
    }
}
