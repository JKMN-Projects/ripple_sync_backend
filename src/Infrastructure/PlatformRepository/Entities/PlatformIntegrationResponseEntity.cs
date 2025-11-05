using RippleSync.Infrastructure.JukmanORM.ClassAttributes;

namespace RippleSync.Infrastructure.PlatformRepository.Entities;
internal class PlatformIntegrationResponseEntity
{
    public int Id { get; set; }

    [SqlProperty(propName: "platform_name")]
    public string Name { get; set; }

    [SqlProperty(propName: "platform_description")]
    public string Description { get; set; }
    public bool Connected { get; set; }

    [SqlProperty(propName: "image_data")]
    public string ImageData { get; set; }


    [SqlConstructor()]
    public PlatformIntegrationResponseEntity(int id, string platform_name, string platform_description, bool connected, string image_data)
    {
        Id = id;
        Name = platform_name;
        Description = platform_description;
        Connected = connected;
        ImageData = image_data;
    }
}
