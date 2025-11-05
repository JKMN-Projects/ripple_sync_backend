using RippleSync.Infrastructure.JukmanORM.ClassAttributes;

namespace RippleSync.Infrastructure.IntegrationRepository.Entities;
internal class ConnectedIntegrationEntity
{
    public Guid Id { get; set; }

    [SqlProperty(propName: "platform_name")]
    public string Name { get; set; }


    [SqlConstructor("ripple_sync")]
    internal ConnectedIntegrationEntity(Guid id, string platform_name)
    {
        Id = id;
        Name = platform_name;
    }
}
