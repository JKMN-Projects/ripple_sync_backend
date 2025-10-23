## Login
GET /api/dashboard/total
``` mermaid

classDiagram
    class GetTotalEngagementParameters {
        string period = "Last week"
    }

    class GetTotalEngagementResponse {
        int totalPosts
        int totalReach
        int totalLikes
        int scheduledPosts
        StatsForPlatformIntegration[] statsForPlatforms
        PaginationInfo page
    }

    class StatsForPlatform {
        string PlatformId
        int reach
        int engagements
        int averageEngagements
        int followers
    }

    class ProblemDetails {
        int status
        string title
        string type
        string instance
        string detail
    }

    GetTotalEngagementParameters ..> StatsForPlatform : "Has"

    GetTotalEngagementParameters --> GetTotalEngagementResponse : "200"
    GetTotalEngagementParameters --> ProblemDetails : "400"
    GetTotalEngagementParameters --> ProblemDetails : "401"
```