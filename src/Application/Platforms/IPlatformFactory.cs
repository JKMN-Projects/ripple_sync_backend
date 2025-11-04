using RippleSync.Domain.Platforms;

namespace RippleSync.Application.Platforms;

public interface IPlatformFactory
{
    ISoMePlatform Create(Platform platform);
}
