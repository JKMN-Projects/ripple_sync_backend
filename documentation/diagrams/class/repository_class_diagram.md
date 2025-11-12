``` mermaid
---
config:
    class:
        hideEmptyMembersBox: true
---
    classDiagram
    namespace Application {
        class IUserRepository {
            <<interface>>
            CreateAsync(User)
            UpdateAsync(User)
            GetByEmailAsync(string email) User?
            GetByIdAsync(Guid userId) User?
            GetByRefreshTokenAsync(string refreshTokenValue) User?
        }
        
        class IPostRepository{
            <<interface>>
            GetByIdAsync(Guid postId) Post?
            RemoveScheduledOnAllPostsWithoutEventAsync(Guid userId)
            CreateAsync(Post)
            UpdateAsync(Post)
            DeleteAsync(Post)
            GetAllByUserIdAsync(Guid userId) IEnumenrable~Post~
            GetPostsReadyToPublishAsync(Guid userId) IEnumenrable~Post~
        }

        class IIntegrationRepository{
            <<interface>>
            CreateAsync(Integration)
            UpdateAsync(Integration)
            DeleteAsync(Guid userId, Platform)
            GetByUserIdAsync(Guid userId) IEnumerable~Integration~
            GetByIdsAsync(IEnumerable~Guid~ integrationIds) IEnumerable~Integration~
        }

        class IFeedbackRepository {
            PostConversationAsync(List<object> messages) HttpResponseMessage?
        }

        class IPostQueries {
            GetPostByUserAsync(Guid userId, string? status) IEnumerable~GetPostsByUserResponse~
            GetImageByIdAsync(Guid imageId) string?
        }

        class IPlatformQueries {
            GetAllPlatformsAsync() IEnumerable~PlatformResponse~
            GetPlatformWithUserIntegrationsAsync(Guid userId) IEnumerable~PlatformWithUserIntegrationsResponse~
        }

        class IIntergrationQueries {
            GetConnectedIntegrationsAsync(Guid userId) IEnumerable~ConnectedIntegrationsResponse~ 
        }
    }
    
    namespace Infrastructure {
        class InMemoryUserRepository
        class NpgsqlUserRepository
        
        class InMemoryPostRepository
        class NpgsqlPostRepository
        
        class InMemoryIntegrationRepository
        class NpgsqlIntegrationRepository
        
        class InMemoryFeedbackRepository
        class NpgsqlFeedbackRepository
    }

    IUserRepository <|-- InMemoryUserRepository
    IUserRepository <|-- NpgsqlUserRepository
    
    IPostRepository <|-- InMemoryPostRepository
    IPostRepository <|-- NpgsqlPostRepository
    IPostQueries <|-- InMemoryPostRepository
    IPostQueries <|-- NpgsqlPostRepository
    
    IIntegrationRepository <|-- InMemoryIntegrationRepository
    IIntegrationRepository <|-- NpgsqlIntegrationRepository
    IIntegrationQueries <|-- InMemoryIntegrationRepository
    IIntegrationQueries <|-- NpgsqlIntegrationRepository
    
    IFeedbackRepository <|-- InMemoryFeedbackRepository
    IFeedbackRepository <|-- NpgsqlFeedbackRepository

    IPlatformQueries <|-- NpgsqlPlatformRepository
```