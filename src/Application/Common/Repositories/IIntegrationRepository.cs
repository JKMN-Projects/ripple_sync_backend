using RippleSync.Application.Integrations;

namespace RippleSync.Application.Common.Repositories;
public interface IIntegrationRepository
{
    Task<IEnumerable<IntegrationResponse>> GetIntegrations(Guid userId);
    Task<IEnumerable<UserIntegrationResponse>> GetUserIntegrations(Guid userId);
    Task CreateUserIntegration(Guid userId, int platformId, string accessToken);
    Task DeleteUserIntegration(Guid userId, int platformId);
}