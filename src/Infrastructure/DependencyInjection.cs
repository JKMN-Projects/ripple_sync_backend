using Microsoft.Extensions.DependencyInjection;

namespace RippleSync.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        // Register application services here
        return services;
    }
}
