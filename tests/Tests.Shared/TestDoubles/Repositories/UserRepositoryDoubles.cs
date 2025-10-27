
using RippleSync.Application.Common.Repositories;
using RippleSync.Domain.Users;

namespace RippleSync.Tests.Shared.TestDoubles.Repositories;
public static partial class UserRepositoryDoubles
{
    public class Dummy : IUserRepository
    {
        public virtual Task<User?> GetUserByEmailAsync(string email, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }

    public static partial class Stubs
    {
        public static class GetUserByEmail
        {
            public class AlwaysReturnsNull : Dummy
            {
                public override Task<User?> GetUserByEmailAsync(string email, CancellationToken cancellationToken = default) 
                    => Task.FromResult<User?>(null);
            }

            public class ReturnsSpecificUser : Dummy
            {
                private readonly User _userToReturn;
                public ReturnsSpecificUser(User userToReturn)
                {
                    _userToReturn = userToReturn;
                }
                public override Task<User?> GetUserByEmailAsync(string email, CancellationToken cancellationToken = default) 
                    => Task.FromResult<User?>(_userToReturn);
            }
        }
    }

    public static partial class Spies
    {
        public class GetUserByEmail : Dummy
        {
            public string? LastReceivedEmail { get; private set; }
            public int InvokationCount { get; private set; }

            private readonly IUserRepository spiedRepository;

            public GetUserByEmail(IUserRepository spiedRepository)
            {
                this.spiedRepository = spiedRepository;
            }

            public override Task<User?> GetUserByEmailAsync(string email, CancellationToken cancellationToken = default)
            {
                LastReceivedEmail = email;
                InvokationCount++;
                return spiedRepository.GetUserByEmailAsync(email, cancellationToken);
            }
        }
    }
}