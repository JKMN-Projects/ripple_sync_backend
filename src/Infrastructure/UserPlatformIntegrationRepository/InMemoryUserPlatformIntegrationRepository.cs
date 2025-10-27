using RippleSync.Domain.Integrations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace RippleSync.Infrastructure.UserPlatformIntegrationRepository;
internal class InMemoryUserPlatformIntegrationRepository
{
    private List<Platform> _platforms = new List<Platform>()
    {
        new() { Id = 1, Name = "Twitter", Description = "Share updates on Twitter"},
        new() { Id = 2, Name = "Facebook", Description = "Create posts on Facebook"},
        new() { Id = 3, Name = "LinkedIn", Description = "Share professional updates on LinkedIn"},
        new() { Id = 4, Name = "Instagram", Description = "Post photos and stories on Instagram"}
    };

    private List<UserPlatformIntegration> _integrations = new List<UserPlatformIntegration>()
    {
        UserPlatformIntegration.Reconstitute(Guid.NewGuid(), "Twitter", "Share updates on Twitter", false, ""),
        UserPlatformIntegration.Reconstitute(Guid.NewGuid(), "Facebook", "Create posts on Facebook", false, ""),
        UserPlatformIntegration.Reconstitute(Guid.NewGuid(), "LinkedIn", "Share professional updates on LinkedIn", true, ""),
        UserPlatformIntegration.Reconstitute(Guid.NewGuid(), "Instagram", "Post photos and stories on Instagram", false, "")
    };

    public async Task<IEnumerable<UserPlatformIntegration>> GetUserIntegrations(Guid userId)
    {
        return _integrations;
    }

    class Platform
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string ImageUrl { get; set; }
    }
}
