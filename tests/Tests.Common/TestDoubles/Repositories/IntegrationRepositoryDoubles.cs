using RippleSync.Application.Common.Repositories;
using RippleSync.Domain.Integrations;
using RippleSync.Domain.Platforms;

namespace RippleSync.Tests.Common.TestDoubles.Repositories;
public static class IntegrationRepositoryDoubles
{
    public class Dummy : IIntegrationRepository
    {
        public virtual Task CreateAsync(Integration integration, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public virtual Task DeleteAsync(Guid userId, Platform platform, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public virtual Task<IEnumerable<Integration>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public virtual Task<IEnumerable<Integration>> GetIntegrationsByIdsAsync(IEnumerable<Guid> integrationIds, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public virtual Task UpdateAsync(Integration integration, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }

    public static class Stubs
    {
        public static class GetIntegrationsByIdsAsync
        {
            public class ReturnsSpecifiedIntegrations : Dummy
            {
                private readonly Integration[] _integrations;
                public ReturnsSpecifiedIntegrations(params Integration[] integrations)
                {
                    _integrations = integrations;
                }

                public override Task<IEnumerable<Integration>> GetIntegrationsByIdsAsync(IEnumerable<Guid> integrationIds, CancellationToken cancellationToken = default)
                    => Task.FromResult(_integrations.AsEnumerable());
            }
        }
    }
}
