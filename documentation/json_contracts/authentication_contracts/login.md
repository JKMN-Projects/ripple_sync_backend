## Login
POST /api/authentication/login
``` mermaid
classDiagram
    class LoginRequest {
        string email
        string password
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

    LoginRequest --> AuthenticationTokenResponse : "200"
    LoginRequest --> ProblemDetails : "401"
```