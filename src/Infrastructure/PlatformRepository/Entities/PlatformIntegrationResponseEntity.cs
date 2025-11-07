using RippleSync.Infrastructure.JukmanORM.ClassAttributes;

namespace RippleSync.Infrastructure.PlatformRepository.Entities;

[method: SqlConstructor()]
internal class PlatformIntegrationResponseEntity(int id, string platformName, string platformDescription, bool connected, string imageData)
{
    public int Id { get; set; } = id;
    public string Name { get; set; } = platformName;
    public string Description { get; set; } = platformDescription;
    public bool Connected { get; set; } = connected;
    public string ImageData { get; set; } = imageData;
}
