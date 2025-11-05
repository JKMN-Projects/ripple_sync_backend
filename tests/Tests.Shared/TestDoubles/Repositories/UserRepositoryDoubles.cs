
using RippleSync.Application.Common.Repositories;
using RippleSync.Domain.Users;

namespace RippleSync.Tests.Shared.TestDoubles.Repositories;
public static partial class UserRepositoryDoubles
{
    public class Dummy : IUserRepository
    {
        public virtual Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public virtual Task<User?> GetByIdAsync(Guid userId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public virtual Task<User?> GetByRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public virtual Task CreateAsync(User user, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public virtual Task UpdateAsync(User user, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }

    public class Composite : IUserRepository
    {
        private readonly IUserRepository _first;
        private readonly IUserRepository _second;
        public Composite(IUserRepository first, IUserRepository second)
        {
            _first = first;
            _second = second;
        }

        public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
        {
            try
            {
                return _first.GetByEmailAsync(email, cancellationToken);
            }
            catch (NotImplementedException)
            {
                return _second.GetByEmailAsync(email, cancellationToken);
            }
        }

        public Task<User?> GetByIdAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            try
            {
                return _first.GetByIdAsync(userId, cancellationToken);
            }
            catch (NotImplementedException)
            {

                return _second.GetByIdAsync(userId, cancellationToken);
            }
        }

        public Task<User?> GetByRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
        {
            try
            {
                return _first.GetByRefreshTokenAsync(refreshToken, cancellationToken);
            }
            catch (NotImplementedException)
            {
                return _second.GetByRefreshTokenAsync(refreshToken, cancellationToken);
            }
        }

        public Task CreateAsync(User user, CancellationToken cancellationToken = default)
        {
            try
            {
                return _first.CreateAsync(user, cancellationToken);
            }
            catch (NotImplementedException)
            {
                return _second.CreateAsync(user, cancellationToken);
            }
        }

        public Task UpdateAsync(User user, CancellationToken cancellationToken = default)
        {
            try
            {
                return _first.UpdateAsync(user, cancellationToken);
            }
            catch (NotImplementedException)
            {
                return _second.UpdateAsync(user, cancellationToken);
            }
        }
    }

    public static partial class Stubs
    {
        public static class GetByEmail
        {
            public class AlwaysReturnsNull : Dummy
            {
                public override Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default) 
                    => Task.FromResult<User?>(null);
            }

            public class ReturnsSpecificUser : Dummy
            {
                private readonly User _userToReturn;
                public ReturnsSpecificUser(User userToReturn)
                {
                    _userToReturn = userToReturn;
                }
                public override Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default) 
                    => Task.FromResult<User?>(_userToReturn);
            }
        }

        public static class GetByRefreshToken
        {
            public class AlwaysReturnsNull : Dummy
            {
                public override Task<User?> GetByRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default) 
                    => Task.FromResult<User?>(null);
            }

            public class ReturnsSpecificUser : Dummy
            {
                private readonly User _userToReturn;
                public ReturnsSpecificUser(User userToReturn)
                {
                    _userToReturn = userToReturn;
                }
                public override Task<User?> GetByRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default) 
                    => Task.FromResult<User?>(_userToReturn);
            }
        }

        public static class Insert
        {
            public class AlwaysReturnsNewGuid : Dummy
            {
                public override Task<Guid> CreateAsync(User user, CancellationToken cancellationToken = default) 
                    => Task.FromResult(Guid.NewGuid());
            }
        }

        public static class Update
        {
            public class ReturnsReceivedUser : Dummy
            {
                public override Task<User> UpdateAsync(User user, CancellationToken cancellation = default) 
                    => Task.FromResult(user);
            }
        }
    }

    public static partial class Spies
    {
        public class GetByEmail : Dummy
        {
            public string? LastReceivedEmail { get; private set; }
            public int InvokationCount { get; private set; }

            private readonly IUserRepository spiedRepository;

            public GetByEmail(IUserRepository spiedRepository)
            {
                this.spiedRepository = spiedRepository;
            }

            public override Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
            {
                LastReceivedEmail = email;
                InvokationCount++;
                return spiedRepository.GetByEmailAsync(email, cancellationToken);
            }
        }

        public class Insert : Dummy
        {
            public User? LastReceivedUser { get; private set; }
            public int InvokationCount { get; private set; }
            private readonly IUserRepository spiedRepository;
            public Insert(IUserRepository spiedRepository)
            {
                this.spiedRepository = spiedRepository;
            }
            public override Task CreateAsync(User user, CancellationToken cancellationToken = default)
            {
                LastReceivedUser = user;
                InvokationCount++;
                spiedRepository.CreateAsync(user, cancellationToken);
                return Task.CompletedTask;
            }
        }

        public class Update : Dummy
        {
            public User? LastReceivedUser { get; private set; }
            public int InvokationCount { get; private set; }
            private readonly IUserRepository spiedRepository;
            public Update(IUserRepository spiedRepository)
            {
                this.spiedRepository = spiedRepository;
            }
            public override Task UpdateAsync(User user, CancellationToken cancellationToken = default)
            {
                LastReceivedUser = user;
                InvokationCount++;
                spiedRepository.UpdateAsync(user, cancellationToken);
                return Task.CompletedTask;
            }
        }
    }
}