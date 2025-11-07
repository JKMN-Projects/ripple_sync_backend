using RippleSync.Infrastructure.JukmanORM.ClassAttributes;

namespace RippleSync.Infrastructure.IntegrationRepository.Entities;

[method: SqlConstructor()]
internal class ConnectedIntegrationEntity(Guid id, string platform_name)
{
    public Guid Id { get; set; } = id;
    public string Name { get; set; } = platform_name;
}
