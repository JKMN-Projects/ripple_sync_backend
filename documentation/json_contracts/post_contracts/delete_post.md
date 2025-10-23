## Delete post
DELETE /api/post/{postId}
``` mermaid
---

config:
    class:
        hideEmptyMembersBox: true
---
classDiagram
    class DeletePostQueryParameters {
        
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
    
    DeletePostQueryParameters --> DeletePostResponse : "204"
    DeletePostQueryParameters --> ProblemDetails : "400"
    DeletePostQueryParameters --> ProblemDetails : "401"
```