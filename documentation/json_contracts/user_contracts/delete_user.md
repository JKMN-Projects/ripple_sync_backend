## Delete user
DELETE /api/users/{userId}
``` mermaid
---

config:
    class:
        hideEmptyMembersBox: true
---
classDiagram
    class DeleteUserQueryParameters {
    }

    class DeleteUserResponse {
    }

    class ProblemDetails {
        int Status
        string Title
        string Type
        string Instance
        string Detail
    }

    DeleteUserQueryParameters --> DeleteUserResponse : "204"
    DeleteUserQueryParameters --> ProblemDetails : "400"
    DeleteUserQueryParameters --> ProblemDetails : "401"
```