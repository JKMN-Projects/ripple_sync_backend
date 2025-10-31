using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using RippleSync.Application.Common.Queries;
using RippleSync.Application.Common.Repositories;
using RippleSync.Application.Common.Security;
using RippleSync.Infrastructure.IntegrationRepository;
using RippleSync.Infrastructure.PlatformRepository;
using RippleSync.Infrastructure.PostRepository;
using RippleSync.Infrastructure.Security;
using RippleSync.Infrastructure.UserRepository;

namespace RippleSync.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString)
    {
        services.AddSingleton<IPasswordHasher, Rfc2898PasswordHasher>();
        services.AddSingleton<IAuthenticationTokenProvider, JwtTokenProvider>();

        services.AddScoped<NpgsqlConnection>(sp => new NpgsqlConnection(connectionString));

        services.AddScoped<IPlatformQueries, InMemoryPlatformRepository>();
        services.AddScoped<IIntegrationQueries, InMemoryIntegrationRepository>();
        services.AddScoped<IPostQueries, InMemoryPostRepository>();

        services.AddScoped<IUserRepository, InMemoryUserRepository>();
        services.AddScoped<IIntegrationRepository, InMemoryIntegrationRepository>();
        services.AddScoped<IPostRepository, InMemoryPostRepository>();
        //services.AddScoped<IPlatformRepository, InMemoryPlatformRepository>();

        return services;
    }
}
