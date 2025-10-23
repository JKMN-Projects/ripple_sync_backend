``` mermaid
classDiagram
    class CreateUserRequest {
        +string email
        +string password
    }

    class StatusCodes {
        200: OK
        
    }

    CreateUserRequest --> StatusCodes : returns
```