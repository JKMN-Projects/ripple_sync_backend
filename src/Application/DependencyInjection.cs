using Microsoft.Extensions.DependencyInjection;
using RippleSync.Application.Integrations;
using RippleSync.Application.Posts;
using RippleSync.Application.Users;

namespace RippleSync.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<UserManager>();
        services.AddScoped<IntegrationManager>();
        services.AddScoped<PostManager>();

        return services;
    }
}
