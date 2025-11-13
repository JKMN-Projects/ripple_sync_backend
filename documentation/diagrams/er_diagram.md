# ER Diagram

```mermaid
erDiagram
    user_account ||--o{ user_platform_integration : "has (CASCADE)"
    user_account ||--o{ post : "creates (CASCADE)"
    user_account ||--o{ user_token : "has (CASCADE)"
    user_token }o--|| token_type : "uses"
    post ||--o{ post_event : "has (CASCADE)"
    post_event }o--|| post_status : "has"
    post ||--o{ post_media : "contains (CASCADE)"
    platform ||--o{ user_platform_integration : "defines"
    user_platform_integration ||--o{ post_event : "posts_to (CASCADE)"

    user_account {
        uuid id PK
        text email UK
        varchar password_hash
        varchar salt
        timestamptz created_at 
    }

    user_token {
        uuid id PK
        uuid user_account_id FK
        int token_type_id FK
        varchar token_value
        timestamptz created_at
        timestamptz expires_at
    }

    token_type {
        int id PK
        text token_name UK
    }

    platform {
        int id PK
        text platform_name UK
        text platform_description
        text image_data
    }

    user_platform_integration {
        uuid id PK
        uuid user_account_id FK
        int platform_id FK
        text access_token
        text refresh_token
        timestamptz expiration
        text token_type
        text scope
        UK "user_account_id, platform_id"
    }

    post {
        uuid id PK
        uuid user_account_id FK
        text message_content
        timestamptz submitted_at
        timestamptz updated_at
        timestamptz scheduled_for
    }

    post_media {
        uuid id PK
        uuid post_id FK
        text image_data 
    }

    post_event {
        uuid post_id PK, FK
        uuid user_platform_integration_id PK, FK
        int post_status_id FK
        text platform_post_identifier 
        jsonb platform_response
    }

    post_status {
        int id PK
        varchar status UK
    }
```
