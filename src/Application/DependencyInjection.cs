using Microsoft.Extensions.DependencyInjection;
using RippleSync.Application.Users;

namespace RippleSync.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<UserManager>();

        return services;
    }
}
