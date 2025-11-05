using RippleSync.Application.Platforms;
using RippleSync.Domain.Platforms;

namespace RippleSync.API.Platforms;

public class DependencyInjectionPlatformFactory(
    IServiceProvider serviceProvider) : IPlatformFactory
{
    public ISoMePlatform Create(Platform platform)
    {
        ISoMePlatform? searchedPlatform = serviceProvider.GetKeyedService<ISoMePlatform>(platform);
        if (searchedPlatform == null)
        {
            throw new ArgumentException($"No platform found for {platform}", nameof(platform));
        }
        return searchedPlatform;
    }

}
