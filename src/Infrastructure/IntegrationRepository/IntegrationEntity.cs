using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RippleSync.Infrastructure.UserPlatformIntegrationRepository;
internal class IntegrationEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public bool Connected { get; set; }
    public string ImageUrl { get; set; }

    private IntegrationEntity(Guid id, string name, string description, bool connected, string imageUrl)
    {
        Id = id;
        Name = name;
        Description = description;
        Connected = connected;
        ImageUrl = imageUrl;
    }
}
