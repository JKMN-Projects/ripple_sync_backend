## Create integration

POST /api/integration

``` mermaid
---

config:
    class:
        hideEmptyMembersBox: true
---

classDiagram
    class CreateIntegrationRequest {
        int platformId
        string accesstoken
    }

    class CreateIntegrationResponse {
    }

    class ProblemDetails {
        int status
        string title
        string type
        string instance
        string detail
    }

    CreateIntegrationRequest --> CreateIntegrationResponse : "201"
    CreateIntegrationRequest --> ProblemDetails : "400"
    CreateIntegrationRequest --> ProblemDetails : "401"
```
