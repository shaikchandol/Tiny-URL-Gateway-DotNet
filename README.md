# TinyURL Gateway — ASP.NET Core 9 / .NET 9 / C#

A production-ready URL shortener microservice built with Clean Architecture, CQRS, MediatR, EF Core 9, and Redis.

## Architecture

```
TinyUrl.Api            → ASP.NET Core Web API (Controllers, Middleware, Swagger)
TinyUrl.Application    → CQRS Commands & Queries (MediatR), DTOs, Interfaces, Validators
TinyUrl.Domain         → Domain Entities (ShortUrl, ClickEvent)
TinyUrl.Infrastructure → EF Core + PostgreSQL, Redis Cache, Repositories
```

## Design Patterns

| Pattern | Where |
|---|---|
| CQRS | Commands vs Queries split in Application layer |
| Mediator | MediatR pipeline for all requests |
| Repository | `IUrlRepository` / `UrlRepository` |
| Cache-Aside | Redis cache checked before DB on resolve |
| Pipeline Behavior | `ValidationBehavior`, `LoggingBehavior` |
| Soft Delete | `IsDeleted` flag with EF global query filter |
| Domain Model | Rich domain entities with encapsulated logic |
| Dependency Injection | All services registered via extension methods |

## API Endpoints

| Method | Path | Description |
|---|---|---|
| GET | `/api/urls` | List all URLs (paginated, searchable) |
| POST | `/api/urls` | Create a short URL |
| GET | `/api/urls/{id}` | Get URL by ID |
| PATCH | `/api/urls/{id}` | Update URL |
| DELETE | `/api/urls/{id}` | Soft-delete URL |
| GET | `/api/urls/{id}/clicks` | Click time-series analytics |
| GET | `/api/stats/summary` | Global stats dashboard |
| GET | `/api/stats/top-urls` | Top URLs by click count |
| GET | `/{shortCode}` | Redirect to original URL |

## Running Locally

### Prerequisites
- .NET 9 SDK
- Docker & Docker Compose

### With Docker Compose

```bash
docker compose up --build
```

API will be available at `http://localhost:8080`  
Swagger UI at `http://localhost:8080/swagger`

### Without Docker

1. Start PostgreSQL and Redis locally
2. Update `appsettings.Development.json` with your connection strings
3. Run:

```bash
cd src/TinyUrl.Api
dotnet run
```

## Running Migrations

```bash
cd src/TinyUrl.Infrastructure
dotnet ef migrations add InitialCreate --startup-project ../TinyUrl.Api
dotnet ef database update --startup-project ../TinyUrl.Api
```

## Tech Stack

- **Runtime**: .NET 9 / C# 13
- **Framework**: ASP.NET Core 9
- **ORM**: Entity Framework Core 9 + Npgsql
- **Database**: PostgreSQL 16
- **Cache**: Redis 7 via StackExchange.Redis
- **Mediator**: MediatR 12
- **Validation**: FluentValidation 11
- **Resilience**: Polly 8
- **Logging**: Serilog
- **Docs**: Swagger / OpenAPI
