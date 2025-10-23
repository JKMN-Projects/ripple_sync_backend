## Create integrations

Post /api/integration

``` mermaid
---

config:
    class:
        hideEmptyMembersBox: true
---

classDiagram
    class PostIntegrationsRequest {
        int platformId
        string accesstoken
    }

    class PostIntegrationsResponse {
    }

    class ProblemDetails {
        int status
        string title
        string type
        string instance
        string detail
    }

    PostIntegrationsRequest --> PostIntegrationsResponse : "201"
    PostIntegrationsRequest --> ProblemDetails : "400"
    PostIntegrationsRequest --> ProblemDetails : "401"
```
