using Infrastructure.FakePlatform;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RippleSync.Application.Common.Notifiers;
using RippleSync.Application.Common.Queries;
using RippleSync.Application.Common.Repositories;
using RippleSync.Application.Common.Security;
using RippleSync.Application.Common.UnitOfWork;
using RippleSync.Application.Platforms;
using RippleSync.Domain.Platforms;
using RippleSync.Infrastructure.FeedbackRepository;
using RippleSync.Infrastructure.IntegrationRepository;
using RippleSync.Infrastructure.JukmanORM.Extensions;
using RippleSync.Infrastructure.NotifierRepository;
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

        services.AddSingleton<IEncryptionService, AesGcmEncryptionService>();

        services.AddKeyedSingleton<ISoMePlatform, SoMePlatformLinkedIn>(Platform.LinkedIn);
        services.AddKeyedSingleton<ISoMePlatform, SoMePlatformX>(Platform.X);
        services.AddKeyedSingleton<ISoMePlatform, SoMePlatformFacebook>(Platform.Facebook);
        services.AddKeyedSingleton<ISoMePlatform, SoMePlatformInstagram>(Platform.Instagram);
        services.AddKeyedSingleton<ISoMePlatform, SoMePlatformThreads>(Platform.Threads);
        services.AddKeyedSingleton<ISoMePlatform, SoMePlatformFake>(Platform.FakePlatform);

        services.AddScoped<IFeedbackRepository, GptFeedbackRepository>();

        services.AddHttpClient<LinkedInHttpClient>(httpClient =>
        {
            httpClient.BaseAddress = new Uri("https://api.linkedin.com/");
            httpClient.DefaultRequestHeaders.Add("X-Restli-Protocol-Version", "2.0.0");
            httpClient.DefaultRequestHeaders.Add("Linkedin-Version", "202510");
        });

        bool inMemory = false;
        if (inMemory)
        {
            services.AddScoped<IUnitOfWork, InMemoryUnitOfWork>();
            services.AddScoped<IPlatformQueries, InMemoryPlatformRepository>();
            services.AddScoped<IIntegrationQueries, InMemoryIntegrationRepository>();
            services.AddScoped<IPostQueries, InMemoryPostRepository>();

            services.AddScoped<IUserRepository, InMemoryUserRepository>();
            services.AddScoped<IIntegrationRepository, InMemoryIntegrationRepository>();
            services.AddScoped<IPostRepository, InMemoryPostRepository>();


            services.AddSingleton<IPostNotificationListener, InMemoryNotificationListener>();
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


            services.AddSingleton<IPostNotificationListener, NpgsqlNotificationListener>();
        }

        return services;
    }
}
