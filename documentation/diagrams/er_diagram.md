# ER Diagram

``` mermaid
 
    erDiagram
    user ||--o{ user_platform_integration : "has"
    user ||--o{ post : "creates"
    platform ||--o{ user_platform_integration : "defines"
    post ||--o{ post_event : "has"
    user_platform_integration ||--o{ post_event : "posts_to"
    post ||--o{ post_media : "contains"
    post_event }o--|| post_status : "has"


    user {
        uuid id PK
        text email UK
        varchar(100) password_hash
        timestamp created_at 
    }

    platform {
        int id PK
        text platform_name UK "twitter, youtube, facebook, etc"
    }

    user_platform_integration {
        uuid user_id FK, PK
        int platform_id FK, PK
        text access_token "encrypted"
    }

    post {
        uuid id PK
        uuid user_id FK, PK
        text message_content
        timestamp submitted_at
        timestamp updated_at
        timestamp scheduled_for
    }

    post_media {
        uuid id PK
        uuid post_id FK, PK
        text image_url 
    }

    post_event {
        uuid post_id FK
        uuid integration_id FK
        uuid post_status_id FK
        text platform_post_identifier 
        jsonb platform_response "errors, etc"
    }

    post_status {
        uuid id PK
        varchar(50) status "Scheduled, Processing, Posted, Failed"
    }
```
