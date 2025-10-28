## Get integrations

GET /api/integration/user

``` mermaid
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

    GetIntegrationsResponse <|--|> UserIntegrationDto
    GetIntegrationsRequest --> GetIntegrationsResponse : "200"
    GetIntegrationsRequest --> ProblemDetails : "400"
    GetIntegrationsRequest --> ProblemDetails : "401"
```