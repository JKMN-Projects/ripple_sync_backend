## Update integrations

PUT /api/integration/{platformId}

``` mermaid
---

config:
    class:
        hideEmptyMembersBox: true
---

classDiagram
    class UpdateIntegrationRequest {
        string accesstoken
    }

    class UpdateIntegrationResponse {
    }

    class ProblemDetails {
        int status
        string title
        string type
        string instance
        string detail
    }

    UpdateIntegrationRequest --> UpdateIntegrationResponse : "204"
    UpdateIntegrationRequest --> ProblemDetails : "400"
    UpdateIntegrationRequest --> ProblemDetails : "401"
    UpdateIntegrationRequest --> ProblemDetails : "404"
```
