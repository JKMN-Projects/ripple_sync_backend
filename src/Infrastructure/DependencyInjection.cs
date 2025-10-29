using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using RippleSync.Application.Common.Repositories;
using RippleSync.Application.Common.Security;
using RippleSync.Infrastructure.Security;
using RippleSync.Infrastructure.UserPlatformIntegrationRepository;
using RippleSync.Infrastructure.UserRepository;
using System.Data;

namespace RippleSync.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString)
    {
        services.AddSingleton<IPasswordHasher, Rfc2898PasswordHasher>();
        services.AddSingleton<IAuthenticationTokenProvider, JwtTokenProvider>();

        services.AddScoped<NpgsqlConnection>(sp =>
            new NpgsqlConnection(connectionString));

        services.AddScoped<IUserRepository, InMemoryUserRepository>();
        services.AddScoped<IIntegrationRepository, InMemoryIntegrationRepository>();

        return services;
    }
}
