## Create post
POST /api/post
``` mermaid
classDiagram
    class CreatePostRequest {
        CreatePostDto post
    }

    class CreatePostResponse {
        string id
    }

    class CreatePostDto {
        string id 
        string messageContent
        int64 scheduled_timestamp
        string[] mediaAttachment
        int32[] integrationIds
    }

    class ProblemDetails {
        int status
        string title
        string type
        string instance
        string detail
    }

    CreatePostRequest <|--|> CreatePostDto
    CreatePostRequest --> CreatePostResponse : "201"
    CreatePostRequest --> ProblemDetails : "400"
    CreatePostRequest --> ProblemDetails : "401"
```