using RippleSync.Application.Integrations;

namespace RippleSync.Application.Common.Queries;

public interface IIntegrationQueries
{
    /// <summary>
    /// Get a users connected integrations.
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns>A collection of the integrations a user has connected to</returns>
    Task<IEnumerable<ConnectedIntegrationsResponse>> GetConnectedIntegrationsAsync(Guid userId, CancellationToken cancellationToken = default);
}
