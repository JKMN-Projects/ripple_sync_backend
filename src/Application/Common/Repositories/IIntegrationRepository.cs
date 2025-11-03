using RippleSync.Domain.Integrations;
using RippleSync.Domain.Platforms;

namespace RippleSync.Application.Common.Repositories;
public interface IIntegrationRepository
{
    Task CreateAsync(Integration integration, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid userId, Platform platform, CancellationToken cancellationToken = default);
    Task<IEnumerable<Integration>> GetIntegrationsByUserId(Guid userId, CancellationToken cancellation = default);
    Task UpdateIntegrationAsync(Integration integration, CancellationToken cancellation = default);
}
