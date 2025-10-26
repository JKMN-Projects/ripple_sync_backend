using Microsoft.Extensions.DependencyInjection;
using RippleSync.Application.Common.Repositories;
using RippleSync.Application.Common.Security;
using RippleSync.Infrastructure.Security;
using RippleSync.Infrastructure.UserRepository;

namespace RippleSync.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<IPasswordHasher, Rfc2898PasswordHasher>();
        services.AddSingleton<IAuthenticationTokenProvider, JwtTokenProvider>();

        services.AddScoped<IUserRepository, InMemoryUserRepository>();

        return services;
    }
}
