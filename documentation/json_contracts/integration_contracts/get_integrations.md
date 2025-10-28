## Get integrations

GET /api/integration

``` mermaid
classDiagram
    class GetIntegrationsRequest {
    }

    class GetIntegrationsResponse {
        IntegrationDto[] integrations
    }

    class IntegrationDto {
        int32 id
        string name
        bool connected
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