using RippleSync.Application.Platforms;
using RippleSync.Domain.Integrations;
using RippleSync.Domain.Platforms;

namespace RippleSync.API.Platforms;

public class DependencyInjectionPlatformFactory(
    IServiceProvider serviceProvider) : IPlatformFactory
{
    public IPlatform Create(Platform platform)
    {
        IPlatform? searchedPlatform = serviceProvider.GetKeyedService<IPlatform>(platform);
        if (searchedPlatform == null)
        {
            throw new ArgumentException($"No platform found for {platform}", nameof(platform));
        }
        return searchedPlatform;
    }

}
