using Microsoft.Extensions.Logging;
using RippleSync.Application.Common.Repositories;
using RippleSync.Application.Common.Responses;

namespace RippleSync.Application.Integrations;
public sealed class IntegrationManager
{
    private readonly ILogger<IntegrationManager> _logger;
    private readonly IIntegrationRepository _integrationRepo;

    public IntegrationManager(ILogger<IntegrationManager> logger, IIntegrationRepository integrationRepo)
    {
        _logger = logger;
        _integrationRepo = integrationRepo;
    }

    public async Task<ListResponse<IntegrationResponse>> GetIntegrations(Guid userId)
        => new ListResponse<IntegrationResponse>(await _integrationRepo.GetIntegrations(userId));

    public async Task<ListResponse<UserIntegrationResponse>> GetUserIntegrations(Guid userId)
        => new ListResponse<UserIntegrationResponse>(await _integrationRepo.GetUserIntegrations(userId));

    public async Task CreateIntegration(Guid userId, int platformId, string accessToken)
    {
        if (string.IsNullOrWhiteSpace(accessToken)) throw new ArgumentNullException(nameof(accessToken));

        await _integrationRepo.CreateUserIntegration(userId, platformId, accessToken);
    }

    public async Task DeleteIntegration(Guid userId, int platformId)
        => await _integrationRepo.DeleteUserIntegration(userId, platformId);
}
