using RippleSync.Application.Platforms;
using RippleSync.Domain.Platforms;

namespace RippleSync.API.Platforms;

public class DependencyInjectionPlatformFactory(
    IServiceProvider serviceProvider) : IPlatformFactory
{
    public ISoMePlatform Create(Platform platform)
    {
        ISoMePlatform? searchedPlatform = serviceProvider.GetKeyedService<ISoMePlatform>(platform);

        return searchedPlatform
            ?? throw new ArgumentException($"No platform found for {platform}", nameof(platform));
    }

}
