# Sequence Diagram

## Register User

``` mermaid
    sequenceDiagram
    Client->>+AuthenticationController: POST /api/authentication/register
    AuthenticationController->>+UserService: CreateUser 
    UserService ->>+ UserRepository: Does user exists?
    alt User already exists
        UserRepository-->>UserService: yes
        UserService -->> AuthenticationController: UserAlreadyExists
        AuthenticationController -->> Client: Return HTTP 409 Conflict
    end
    UserRepository-->>-UserService: no
    UserService ->>+ UserRepository: Insert User
    UserRepository -->>- UserService: 
    UserService -->>- AuthenticationController: 
    AuthenticationController -->- Client: 
```

## Login

``` mermaid
    sequenceDiagram
    Client->>+AuthenticationController: POST /api/authentication/login
    AuthenticationController->>+UserService: Login 
    UserService ->>+ UserRepository: Get user by email
    alt Invalid Login 
        UserService -->> AuthenticationController: 
        AuthenticationController -->> Client: Return HTTP 400 Bad Request
    end
    UserRepository-->>-UserService: Return user
    UserService -->>- AuthenticationController: return JWT token
    AuthenticationController -->- Client: Return 200 OK JWT token
```

## Get Statistics On Post

Used to get statistics on post for each integration

``` mermaid
    sequenceDiagram
    Client->>+PostController: Get /api/post/statistics
    PostController->>+PostService: GetPostStatistics
    PostService ->>+ PostRepository: GetPostById
    PostRepository -->>- PostService: Posts With events
    loop Foreach Integration
        PostService ->>+ IIntegretionRepository:  get statistics on post
        IIntegretionRepository -->>- PostService: Return statistics
    end
    PostService -->>- PostController: Stats on post
    PostController -->>- Client : Return stats on post
```

## Schedule post

Used to schedule post

``` mermaid
    sequenceDiagram
    Client->>+PostController: Post /api/post/schedule
    PostController->>+PostService: Schedule post
    PostService ->>+ PostRepository: Schedule post
    PostRepository -->>- PostService: 
    PostService -->>- PostController: 
    PostController -->>- Client : 
    loop Foreach Scheduled Post
        PostPublisher -->>+ PostRepository: GetScheduledPosts
        PostRepository -->>+ PostPublisher: Scheduled Post With Datetime < Now
        loop Foreach Post Event
            PostPublisher -->>+ IIntegretionRepository: Publish Post
            IIntegretionRepository -->>- PostPublisher: Return Status
        end
        PostPublisher -->>- PostRepository: SetEventStatus
        PostRepository -->>- PostPublisher: 
    end


```
