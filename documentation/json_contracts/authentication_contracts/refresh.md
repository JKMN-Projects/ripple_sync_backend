## Refresh
POST /api/authentication/refresh
``` mermaid
classDiagram
    class RefreshRequest {
        string refreshToken
    }

    class AuthenticationTokenResponse {
        string email
    }

    class ProblemDetails {
        int Status
        string Title
        string Type
        string Instance
        string Detail
    }

    RefreshRequest --> AuthenticationTokenResponse : "200"
    RefreshRequest --> ProblemDetails : "401"
```