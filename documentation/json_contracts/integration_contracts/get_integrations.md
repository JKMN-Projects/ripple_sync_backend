## Get integrations

GET /api/integration

``` mermaid
---

config:
    class:
        hideEmptyMembersBox: true
---
classDiagram
    class GetIntegrationsRequest {
    }

    class GetIntegrationsResponse {
        IntegrationDto[] integrations
    }

    class IntegrationDto {
        int32 platformId
        string name
        string description
        bool connected
        string imageUrl
    }

    class ProblemDetails {
        int status
        string title
        string type
        string instance
        string detail
    }

    GetIntegrationsResponse <|--|> IntegrationDto
    GetIntegrationsRequest --> GetIntegrationsResponse : "200"
    GetIntegrationsRequest --> ProblemDetails : "400"
    GetIntegrationsRequest --> ProblemDetails : "401"
```