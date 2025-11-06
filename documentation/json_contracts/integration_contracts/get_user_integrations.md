## Get integrations

GET /api/integration/user

``` mermaid
---

config:
    class:
        hideEmptyMembersBox: true
---
classDiagram
    class GetUserIntegrationsRequest {
    }

    class GetUserIntegrationsResponse {
        UserIntegrationDto[] integrations
    }

    class UserIntegrationDto {
        int32 platformId
        string name
    }

    class ProblemDetails {
        int status
        string title
        string type
        string instance
        string detail
    }

    GetUserIntegrationsResponse <|--|> UserIntegrationDto
    GetUserIntegrationsRequest --> GetUserIntegrationsResponse : "200"
    GetUserIntegrationsRequest --> ProblemDetails : "400"
    GetUserIntegrationsRequest --> ProblemDetails : "401"
```