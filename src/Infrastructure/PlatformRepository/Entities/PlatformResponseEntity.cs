using RippleSync.Infrastructure.JukmanORM.ClassAttributes;

namespace RippleSync.Infrastructure.PlatformRepository.Entities;
internal class PlatformResponseEntity
{
    [SqlProperty(propName: "platform_name")]
    public string Name { get; set; }


    [SqlConstructor()]
    public PlatformResponseEntity(string platform_name)
    {
        Name = platform_name;
    }
}
