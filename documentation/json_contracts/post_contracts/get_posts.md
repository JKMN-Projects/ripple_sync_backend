## Get posts
GET /api/post
``` mermaid
classDiagram
    class GetPostsRequest {
        string? status = null
    }

    class GetPostResponse {
        GetPostDto[] posts
    }

    class GetPostDto {
        string messageContent
        string statusName
        string[] mediaAttachment
        int64 timestamp
        string[] platforms
    }

    class ProblemDetails {
        int status
        string title
        string type
        string instance
        string detail
    }

    GetPostResponse <|--|> GetPostDto
    GetPostsRequest --> GetPostResponse : "200"
    GetPostsRequest --> ProblemDetails : "400"
    GetPostsRequest --> ProblemDetails : "401"
```