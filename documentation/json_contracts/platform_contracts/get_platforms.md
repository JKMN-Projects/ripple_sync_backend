## Get platforms

GET /api/platform

``` mermaid
---

config:
    class:
        hideEmptyMembersBox: true
---
classDiagram
    class GetPlatformsRequest {
    }

    class GetPlatformsResponse {
        PlatformDto[] platforms
    }

    class PlatformDto {
        string name
    }

    class ProblemDetails {
        int status
        string title
        string type
        string instance
        string detail
    }

    GetPlatformsResponse <|--|> PlatformDto
    GetPlatformsRequest --> GetPlatformsResponse : "200"
    GetPlatformsRequest --> ProblemDetails : "400"
    GetPlatformsRequest --> ProblemDetails : "401"
```