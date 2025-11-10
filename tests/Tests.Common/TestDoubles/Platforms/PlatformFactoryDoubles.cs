using RippleSync.Application.Platforms;
using RippleSync.Domain.Platforms;

namespace RippleSync.Tests.Common.TestDoubles.Platforms;

public static class PlatformFactoryDoubles
{
    public class Dummy : IPlatformFactory
    {
        public virtual ISoMePlatform Create(Platform platform) => throw new NotImplementedException();
    }

    public static class Stubs
    {
        public static class Create
        {
            public class ReturnsSpecifiedSoMePlatform : Dummy
            {
                private readonly ISoMePlatform _platform;
                public ReturnsSpecifiedSoMePlatform(ISoMePlatform platform)
                {
                    _platform = platform;
                }

                public override ISoMePlatform Create(Platform platform)
                    => _platform;
            }

            public class ReturnsDifferentSoMePlatformsBasedOnInput : Dummy
            {
                private readonly Dictionary<Platform, ISoMePlatform> _platformsMap;
                private readonly ISoMePlatform? _defaultPlatform;
                public ReturnsDifferentSoMePlatformsBasedOnInput(Dictionary<Platform, ISoMePlatform> platformsMap, ISoMePlatform? defaultPlatform = null)
                {
                    _platformsMap = platformsMap;
                    _defaultPlatform = defaultPlatform;
                }

                public override ISoMePlatform Create(Platform platform)
                {
                    return _platformsMap.TryGetValue(platform, out var soMePlatform)
                        ? soMePlatform
                        : _defaultPlatform
                            ?? throw new ArgumentException($"No SoMePlatform stub defined for platform: {platform}");
                }
            }
        }
    }
}
