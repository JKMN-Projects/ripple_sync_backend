using Microsoft.Extensions.DependencyInjection;

namespace RippleSync.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // Register application services here
        return services;
    }
}
