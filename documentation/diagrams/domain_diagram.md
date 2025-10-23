## Domain diagram

``` mermaid

classDiagram
    User <.. Post : "Posted by" 
    User o--> UserIntegration
    UserIntegration --> Platform : "Integrates"
    Post *--> PostMedia : "has"
    Post *--> PostEvent : "has"
    PostEvent o--> PostEventStatus

    class Platform

    class User
    
    class UserIntegration

    class Post

    class PostMedia

    class PostEvent

    class PostEventStatus {
        <<enumeration>>
        Scheduled
        Processing
        Posted
        Failed
    }
```