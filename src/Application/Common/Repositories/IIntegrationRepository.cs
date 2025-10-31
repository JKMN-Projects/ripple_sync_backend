using RippleSync.Domain.Integrations;

namespace RippleSync.Application.Common.Repositories;
public interface IIntegrationRepository
{
    Task CreateAsync(Integration integration, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid userId, int platformId, CancellationToken cancellationToken = default);
}
