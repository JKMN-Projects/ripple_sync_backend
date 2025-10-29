using RippleSync.Infrastructure.MicroORM.ClassAttributes;

namespace RippleSync.Infrastructure.IntegrationRepository.Entities;
internal class UserIntegrationEntity
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public bool Connected { get; set; }
    public string ImageUrl { get; set; }


    [SqlConstructor("ripple_sync")]
    internal UserIntegrationEntity(int id, string name, string description, bool connected, string imageUrl)
    {
        Id = id;
        Name = name;
        Description = description;
        Connected = connected;
        ImageUrl = imageUrl;
    }
}
