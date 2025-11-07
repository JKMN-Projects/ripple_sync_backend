using RippleSync.Infrastructure.JukmanORM.ClassAttributes;

namespace RippleSync.Infrastructure.PlatformRepository.Entities;

[method: SqlConstructor()]
internal class PlatformResponseEntity(string platformName)
{
    public string Name { get; set; } = platformName;
}
