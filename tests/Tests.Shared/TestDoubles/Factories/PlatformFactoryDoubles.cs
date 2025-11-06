using RippleSync.Application.Platforms;
using RippleSync.Domain.Platforms;

namespace RippleSync.Tests.Shared.TestDoubles.Factories;

public static class PlatformFactoryDoubles
{
    public class Dummy : IPlatformFactory
    {
        public virtual ISoMePlatform Create(Platform platform) => throw new NotImplementedException();
    }
}
