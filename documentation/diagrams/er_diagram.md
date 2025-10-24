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
    user ||--|| user_token : "has"
    user_token }o--|| token_type : "uses"


    user {
        uuid id PK
        text email UK
        varchar(100) password_hash
        varchar(100) salt
        timestamptz created_at 
    }

    user_token {
        uuid id PK
        uuid user_id PK, FK
        int token_type_id FK
        varchar(100) token
        timestamptz created_at
        timestamptz expires_at
    }

    token_type {
        int id PK
        text type
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
        timestamptz submitted_at
        timestamptz updated_at
        timestamptz scheduled_for "NULL"
    }

    post_media {
        uuid id PK
        uuid post_id FK, PK
        text image_url 
    }

    post_event {
        uuid post_id FK
        uuid integration_id FK
        int post_status_id FK
        text platform_post_identifier 
        jsonb platform_response "errors, etc"
    }

    post_status {
        int id PK
        varchar(50) status "scheduled, processing, posted, failed"
    }
```
