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
using RippleSync.Infrastructure.UnitOfWork;
using RippleSync.Infrastructure.UserRepository;

namespace RippleSync.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString)
    {
        services.AddSingleton<IPasswordHasher, Rfc2898PasswordHasher>();
        services.AddSingleton<IAuthenticationTokenProvider, JwtTokenProvider>();

        services.AddSingleton<IOAuthSecurer, OAuthSecurer>();

        services.AddSingleton<IEncryptionService>(sp =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            string key = config["Encryption:Key"]
                ?? throw new ArgumentException("EncryptionKey empty");
            return new AesGcmEncryptionService(key);
        });

        services.AddKeyedSingleton<ISoMePlatform, SoMePlatformLinkedIn>(Platform.LinkedIn);
        services.AddKeyedSingleton<ISoMePlatform, SoMePlatformX>(Platform.X);
        services.AddKeyedSingleton<ISoMePlatform, SoMePlatformFacebook>(Platform.Facebook);
        services.AddKeyedSingleton<ISoMePlatform, SoMePlatformInstagram>(Platform.Instagram);
        services.AddKeyedSingleton<ISoMePlatform, SoMePlatformThreads>(Platform.Threads);
        services.AddKeyedSingleton<ISoMePlatform, SoMePlatformFake>(Platform.FakePlatform);

        bool inMemory = false;
        if (inMemory)
        {
            services.AddScoped<IPlatformQueries, InMemoryPlatformRepository>();
            services.AddScoped<IIntegrationQueries, InMemoryIntegrationRepository>();
            services.AddScoped<IPostQueries, InMemoryPostRepository>();

            services.AddScoped<IUserRepository, InMemoryUserRepository>();
            services.AddScoped<IIntegrationRepository, InMemoryIntegrationRepository>();
            services.AddScoped<IPostRepository, InMemoryPostRepository>();
        }
        else
        {
            services.AddScoped<IUnitOfWork>(sp => new NpgsqlUnitOfWork(connectionString));

            services.AddScoped<IPlatformQueries, NpgsqlPlatformRepository>();
            services.AddScoped<IIntegrationQueries, NpgsqlIntegrationRepository>();
            services.AddScoped<IPostQueries, NpgsqlPostRepository>();

            services.AddScoped<IUserRepository, NpgsqlUserRepository>();
            services.AddScoped<IIntegrationRepository, NpgsqlIntegrationRepository>();
            services.AddScoped<IPostRepository, NpgsqlPostRepository>();
        }

        return services;
    }
}
