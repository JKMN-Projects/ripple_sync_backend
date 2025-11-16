# RippleSync Backend

[![Build & Test](https://github.com/JKMN-Projects/ripple_sync_backend/actions/workflows/build-docker-image.yml/badge.svg)](https://github.com/JKMN-Projects/ripple_sync_backend/actions/workflows/build-docker-image.yml)
[![Tests](https://github.com/JKMN-Projects/ripple_sync_backend/actions/workflows/main.yml/badge.svg)](https://github.com/JKMN-Projects/ripple_sync_backend/actions/workflows/main.yml)

A robust social media management platform backend built with .NET 9 and Clean Architecture principles, enabling seamless cross-platform content publishing and analytics.

## 🚀 Features

- **Multi-Platform Publishing**: Schedule and publish content across multiple social media platforms simultaneously
- **Real-time Post Processing**: Automated post scheduling with PostgreSQL notifications
- **Secure Authentication**: JWT-based authentication with refresh token support
- **Analytics Dashboard**: Track engagement metrics across all connected platforms
- **Clean Architecture**: Domain-driven design with clear separation of concerns
- **Platform Integration**: Support for X (Twitter), LinkedIn, Facebook, Instagram, and Threads

## 📋 Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [Docker](https://www.docker.com/get-started) (for containerized deployment)
- PostgreSQL 16+ with pg_cron extension
- Visual Studio 2022 or VS Code (recommended)

## 🏗️ Architecture

The solution follows Clean Architecture principles with the following project structure:
```
src/
├── Domain/              # Enterprise business rules
├── Application/         # Application business rules
├── Infrastructure/      # External concerns (DB, external APIs)
├── Infrastructure.JukmanORM/  # Custom ORM implementation
├── Infrastructure.FakePlatform/  # Mock platform for testing
└── API/                # REST API presentation layer

tests/
├── Application.Tests/   # Application layer unit tests
├── Infrastructure.Tests/# Infrastructure layer tests
└── Tests.Common/       # Shared test utilities
```

### Key Design Patterns

- **Repository Pattern**: Abstract data access layer
- **Factory Pattern**: Platform-specific implementation creation
- **Domain-Driven Design**: Rich domain models with business logic
- **CQRS**: Separate read and write operations for optimized performance

## 🚦 Getting Started

### Local Development Setup

1. **Clone the repository**
```bash
   git clone https://github.com/jkmn-projects/ripple_sync_backend.git
   cd ripple_sync_backend
```

2. **Start PostgreSQL with Docker**
```bash
   docker-compose -f tools/Docker/compose.yaml up -d
```

3. **Run database migrations**
```bash
   dotnet run --project tools/DbMigrator "Host=localhost;Port=50003;Database=ripple_sync;Username=JukmanDev;Password=Juk20Man25"
```

4. **Configure application settings**
   Create an `appsettings.Development.json` in the API project with your configuration.

5. **Run the application**
```bash
   dotnet run --project src/API
```

   The API will be available at `https://localhost:7275`

### Docker Deployment
```bash
# Build the Docker image
docker build -f tools/Docker/Dockerfile -t ripplesync-backend .

# Run the container
docker run -d -p 8080:8080 --env-file .env ripplesync-backend
```

## 📡 API Documentation

### Authentication Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/authentication/login` | User login |
| POST | `/api/authentication/register` | User registration |
| POST | `/api/authentication/refresh` | Refresh access token |

### Post Management

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/post` | Get user's posts |
| POST | `/api/post` | Create new post |
| PUT | `/api/post/{postId}` | Update post |
| DELETE | `/api/post/{postId}` | Delete post |

### Integration Management

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/integration` | Get available integrations |
| POST | `/api/integration` | Connect platform integration |
| DELETE | `/api/integration/{platformId}` | Disconnect integration |

### Dashboard

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/dashboard/total` | Get engagement analytics |

## 🔄 CI/CD Pipeline

The project uses GitHub Actions for continuous integration and deployment:

- **Build & Test**: Runs on every push and pull request to main
- **Docker Image Build**: Automatically builds and publishes Docker images to GitHub Container Registry
- **Database Migration Verification**: Validates migration scripts before deployment

## 🧪 Testing

Run all tests:
```bash
dotnet test
```

Run specific test project:
```bash
dotnet test tests/Application.Tests
```

## 📊 Database Schema

The application uses PostgreSQL with the following key entities:
- User accounts with secure authentication
- Platform integrations for social media connections
- Posts with scheduling and media attachments
- Post events tracking publishing status
- Analytics data for engagement metrics

See [ER Diagram](documentation/diagrams/er_diagram.md) for detailed schema information.

## 🛠️ Development Tools

- **Database Migrator**: Custom tool for managing database schema changes
- **Docker Compose**: Local development environment setup
- **EditorConfig**: Consistent code formatting across the team

## 📝 License

This project is proprietary software. All rights reserved.

## 👥 Team

- **Jukman Projects** - Initial work and maintenance

## 🔗 Related Projects

- [RippleSync Frontend](https://github.com/jkmn-projects/ripple_sync_frontend) - Angular-based web client

## 📧 Contact

For questions or support, please reach out to the development team.

---

Built with ❤️ using .NET 9 and Clean Architecture
