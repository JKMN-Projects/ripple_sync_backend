## Update post
PUT /api/post/{postId}
``` mermaid
---

config:
    class:
        hideEmptyMembersBox: true
---
classDiagram
    class UpdatePostRequest {
        CreatePostDto post
    }

    class UpdatePostResponse {
    }

    class UpdatePostDto {
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

    UpdatePostRequest <|--|> UpdatePostDto

    UpdatePostRequest --> UpdatePostResponse : "204"
    UpdatePostRequest --> ProblemDetails : "400"
    UpdatePostRequest --> ProblemDetails : "401"
```