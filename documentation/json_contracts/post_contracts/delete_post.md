## Delete post
DELETE /api/post/{id}
``` mermaid
classDiagram
    class DeletePostRequest {
    }

    class DeletePostResponse {
    }

    class ProblemDetails {
        int status
        string title
        string type
        string instance
        string detail
    }

    DeletePostRequest --> DeletePostResponse : "200"
    DeletePostRequest --> ProblemDetails : "400"
    DeletePostRequest --> ProblemDetails : "401"
```