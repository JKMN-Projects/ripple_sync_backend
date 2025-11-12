# Sequence Diagram

## Register User

```mermaid
    sequenceDiagram
    Client->>+AuthenticationController: POST /api/authentication/register
    AuthenticationController->>+UserManager: CreateUser 
    UserManager ->>+ UserRepository: Does user exists?
    alt User already exists
        UserRepository-->>UserManager: yes
        UserManager -->> AuthenticationController: UserAlreadyExists
        AuthenticationController -->> Client: Return HTTP 409 Conflict
    end
    UserRepository-->>-UserManager: no
    UserManager ->>+ UserRepository: Insert User
    UserRepository -->>- UserManager: 
    UserManager -->>- AuthenticationController: 
    AuthenticationController -->- Client: 
```

## Login

```mermaid
    sequenceDiagram
    Client->>+AuthenticationController: POST /api/authentication/login
    AuthenticationController->>+UserManager: Login 
    UserManager ->>+ UserRepository: Get user by email
    alt Invalid Login 
        UserManager -->> AuthenticationController: 
        AuthenticationController -->> Client: Return HTTP 400 Bad Request
    end
    UserRepository-->>-UserManager: Return user
    UserManager -->>- AuthenticationController: return JWT token
    AuthenticationController -->- Client: Return 200 OK JWT token
```

## Get Statistics On Post
Used to get statistics on post for each integration

```mermaid
sequenceDiagram
    Client->>+PostController: Get /api/post/statistics
    PostController->>+PostManager: GetPostStatistics
    PostManager ->>+ PostRepository: Get published posts by user
    PostRepository -->>- PostManager: Published posts with events
    PostManager ->>+ IIntegrationRepository: Get users integrations
    IIntegrationRepository -->>- PostManager: Return users integrations
    loop Foreach Integration
        PostManager ->>+ IPlatformFactory: Create platform
        IPlatformFactory -->>- PostManager: ISoMePlatform
        PostManager ->>+ ISoMePlatform:  Get statistics for posts
        ISoMePlatform -->>- PostManager: Return statistics
    end
    PostManager -->>- PostController: Stats on post
    PostController -->>- Client : Return stats on post
```

## Publish post

Used to publish post

```mermaid
sequenceDiagram
    database Client 
```
