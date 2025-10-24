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
            FindAllAsync()
            FindAsync(Guid id)
            InsertAsync(User user)
            UpdateAsync(User user)
            RemoveAsync(Guid id)
        }
    }

    namespace Domain {
        class User
    }

    namespace Infrastructure {
        class InMemoryUserRepository
        class PostgresUserRepository
    }

    IUserRepository --> User
    IUserRepository <|-- InMemoryUserRepository
    IUserRepository <|-- PostgresUserRepository

```