``` mermaid
classDiagram
    class CreateUserRequest {
        string email
        string password
    }

    class CreateUserResponse {
    }

    class ProblemDetails {
        int Status
        string Title
        string Type
        string Instance
        string Detail
    }

    CreateUserRequest --> CreateUserResponse : "201"
    CreateUserRequest --> ProblemDetails : "400"
    CreateUserRequest --> ProblemDetails : "401"
```