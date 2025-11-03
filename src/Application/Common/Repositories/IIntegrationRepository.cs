using RippleSync.Domain.Integrations;
using RippleSync.Domain.Platforms;

namespace RippleSync.Application.Common.Repositories;
public interface IIntegrationRepository
{
    Task CreateAsync(Integration integration, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid userId, Platform platform, CancellationToken cancellationToken = default);
}
