using RippleSync.Application.Common.Repositories;
using RippleSync.Application.Integrations;

namespace RippleSync.Infrastructure.IntegrationRepository;
public class InMemoryIntegrationRepository : IIntegrationRepository
{
    //private List<Platform> _platforms = new List<Platform>()
    //{
    //    new() { Id = 1, Name = "Twitter", Description = "Share updates on Twitter"},
    //    new() { Id = 2, Name = "Facebook", Description = "Create posts on Facebook"},
    //    new() { Id = 3, Name = "LinkedIn", Description = "Share professional updates on LinkedIn"},
    //    new() { Id = 4, Name = "Instagram", Description = "Post photos and stories on Instagram"}
    //};

    private static readonly List<IntegrationResponse> _integrations =
    [
        new (1, "Twitter", "Share updates on Twitter", false, ""),
        new (2, "Facebook", "Create posts on Facebook", false, ""),
        new (3, "LinkedIn", "Share professional updates on LinkedIn", true, ""),
        new (4, "Instagram", "Post photos and stories on Instagram", false, "")
    ];

    public async Task<IEnumerable<IntegrationResponse>> GetIntegrations(Guid userId, CancellationToken cancellationToken = default)
        => _integrations;

    public async Task<IEnumerable<UserIntegrationResponse>> GetUserIntegrations(Guid userId, CancellationToken cancellationToken = default)
        => _integrations.Select(i => new UserIntegrationResponse(i.PlatformId, i.Name));

    public async Task CreateUserIntegration(Guid userId, int platformId, string accessToken, CancellationToken cancellationToken = default)
    {
        var toEdit = _integrations.FirstOrDefault(i => i.PlatformId == platformId);

        if (toEdit == null) return;

        var toEditIndex = _integrations.IndexOf(toEdit);
        _integrations[toEditIndex] = toEdit with { Connected = true };
    }

    public async Task DeleteUserIntegration(Guid userId, int platformId, CancellationToken cancellationToken = default)
    {
        var toEdit = _integrations.FirstOrDefault(i => i.PlatformId == platformId);

        if (toEdit == null) return;

        var toEditIndex = _integrations.IndexOf(toEdit);
        _integrations[toEditIndex] = toEdit with { Connected = false };
    }

    //class Platform
    //{
    //    public int Id { get; set; }
    //    public string Name { get; set; }
    //    public string Description { get; set; }
    //    public string ImageUrl { get; set; }
    //}
}
