using RippleSync.Domain.Platforms;

namespace RippleSync.Application.Platforms;

public interface IPlatformFactory
{
    IPlatform Create(Platform platform);
}
