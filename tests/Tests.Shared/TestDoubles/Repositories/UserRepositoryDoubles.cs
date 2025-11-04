
using RippleSync.Application.Common.Repositories;
using RippleSync.Domain.Users;

namespace RippleSync.Tests.Shared.TestDoubles.Repositories;
public static partial class UserRepositoryDoubles
{
    public static IUserRepository ComposeMany(params IUserRepository[] repositories)
    {
        if (repositories.Length > 2)
        {
            return repositories.Aggregate((first, second) => new Composite(first, second));
        }
        else if (repositories.Length == 2)
        {
            return new Composite(repositories[0], repositories[1]);
        }
        throw new ArgumentException("At least two repositories must be provided for composition.", nameof(repositories));
    }

    public class Dummy : IUserRepository
    {
        public virtual Task<User?> GetUserByEmailAsync(string email, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public virtual Task<User?> GetUserByIdAsync(Guid userId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public virtual Task<Guid> InsertAsync(User user, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public virtual Task<User> UpdateUserAsync(User user, CancellationToken cancellation = default) => throw new NotImplementedException();
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

        public Task<User?> GetUserByEmailAsync(string email, CancellationToken cancellationToken = default)
        {
            try
            {
                return _first.GetUserByEmailAsync(email, cancellationToken);
            }
            catch (NotImplementedException)
            {
                return _second.GetUserByEmailAsync(email, cancellationToken);
            }
        }

        public Task<User?> GetUserByIdAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            try
            {
                return _first.GetUserByIdAsync(userId, cancellationToken);
            }
            catch (NotImplementedException)
            {

                return _second.GetUserByIdAsync(userId, cancellationToken);
            }
        }

        public Task<Guid> InsertAsync(User user, CancellationToken cancellationToken = default)
        {
            try
            {
                return _first.InsertAsync(user, cancellationToken);
            }
            catch (NotImplementedException)
            {
                return _second.InsertAsync(user, cancellationToken);
            }
        }

        public Task<User> UpdateUserAsync(User user, CancellationToken cancellation = default)
        {
            try
            {
                return _first.UpdateUserAsync(user, cancellation);
            }
            catch (NotImplementedException)
            {
                return _second.UpdateUserAsync(user, cancellation);
            }
        }
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

        public static class InsertUser
        {
            public class AlwaysReturnsNewGuid : Dummy
            {
                public override Task<Guid> InsertAsync(User user, CancellationToken cancellationToken = default) 
                    => Task.FromResult(Guid.NewGuid());
            }
        }

        public static class UpdateUser
        {
            public class ReturnsReceivedUser : Dummy
            {
                public override Task<User> UpdateUserAsync(User user, CancellationToken cancellation = default) 
                    => Task.FromResult(user);
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

        public class InsertUser : Dummy
        {
            public User? LastReceivedUser { get; private set; }
            public int InvokationCount { get; private set; }
            private readonly IUserRepository spiedRepository;
            public InsertUser(IUserRepository spiedRepository)
            {
                this.spiedRepository = spiedRepository;
            }
            public override Task<Guid> InsertAsync(User user, CancellationToken cancellationToken = default)
            {
                LastReceivedUser = user;
                InvokationCount++;
                return spiedRepository.InsertAsync(user, cancellationToken);
            }
        }

        public class UpdateUser : Dummy
        {
            public User? LastReceivedUser { get; private set; }
            public int InvokationCount { get; private set; }
            private readonly IUserRepository spiedRepository;
            public UpdateUser(IUserRepository spiedRepository)
            {
                this.spiedRepository = spiedRepository;
            }
            public override Task<User> UpdateUserAsync(User user, CancellationToken cancellation = default)
            {
                LastReceivedUser = user;
                InvokationCount++;
                return spiedRepository.UpdateUserAsync(user, cancellation);
            }
        }
    }
}