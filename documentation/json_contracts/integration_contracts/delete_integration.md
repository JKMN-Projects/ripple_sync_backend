## Delete integration

DELETE /api/integration/{platformId}

``` mermaid
---

config:
    class:
        hideEmptyMembersBox: true
---

classDiagram
    class DeleteIntegrationQueryParameters {
    }

    class DeleteIntegrationResponse {
    }

    class ProblemDetails {
        int status
        string title
        string type
        string instance
        string detail
    }

    DeleteIntegrationQueryParameters --> DeleteIntegrationResponse : "204"
    DeleteIntegrationQueryParameters --> ProblemDetails : "400"
    DeleteIntegrationQueryParameters --> ProblemDetails : "401"
    DeleteIntegrationQueryParameters --> ProblemDetails : "404"
```
