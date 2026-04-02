# High-Level Design (HLD) — TinyURL Gateway

## 1. System Overview

TinyURL Gateway is a production-ready URL shortening microservice that accepts long URLs, generates short codes, stores them, and redirects users to the original URLs. It also provides click analytics and statistics dashboards.

---

## 2. Architecture Diagram

```
┌─────────────────────────────────────────────────────────────────────────┐
│                          CLIENT LAYER                                    │
│                                                                          │
│   ┌─────────────┐    ┌──────────────┐    ┌───────────────────────────┐  │
│   │  React Web  │    │  Mobile App  │    │  Third-Party Integrations │  │
│   │   (Vite)    │    │  (Future)    │    │  (CLI / REST Clients)     │  │
│   └──────┬──────┘    └──────┬───────┘    └────────────┬──────────────┘  │
└──────────┼────────────────── ┼──────────────────────── ┼─────────────────┘
           │                   │                          │
           ▼                   ▼                          ▼
┌─────────────────────────────────────────────────────────────────────────┐
│                          API GATEWAY / LOAD BALANCER                    │
│               (Reverse Proxy — Nginx / Azure API Management)            │
│    Rate Limiting │ TLS Termination │ Authentication │ Request Routing   │
└────────────────────────────────┬────────────────────────────────────────┘
                                 │
                                 ▼
┌─────────────────────────────────────────────────────────────────────────┐
│                       ASP.NET CORE 9 WEB API                            │
│                                                                          │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────────────────────┐   │
│  │  /api/urls   │  │ /api/stats   │  │   /{shortCode}  (Redirect)   │   │
│  │  CRUD Layer  │  │  Analytics   │  │   (302 / 301 Response)       │   │
│  └──────┬───────┘  └──────┬───────┘  └──────────────┬───────────────┘   │
│         │                 │                           │                  │
│         └─────────────────┼───────────────────────────┘                  │
│                           ▼                                              │
│              ┌────────────────────────┐                                  │
│              │    MediatR Pipeline    │                                  │
│              │  LoggingBehavior       │                                  │
│              │  ValidationBehavior    │                                  │
│              │  Command / Query       │                                  │
│              └────────────┬───────────┘                                  │
└───────────────────────────┼──────────────────────────────────────────────┘
                            │
              ┌─────────────┴──────────────┐
              │                            │
              ▼                            ▼
┌─────────────────────────┐   ┌────────────────────────────┐
│     CACHE LAYER         │   │      PERSISTENCE LAYER     │
│                         │   │                            │
│   ┌─────────────────┐   │   │  ┌─────────────────────┐  │
│   │   Redis 7       │   │   │  │   PostgreSQL 16      │  │
│   │                 │   │   │  │                      │  │
│   │  url:{code}     │◄──┼───┼──│  urls table          │  │
│   │  TTL: 24h       │   │   │  │  click_events table  │  │
│   │  Cache-Aside    │   │   │  │  EF Core 9 ORM       │  │
│   └─────────────────┘   │   │  └─────────────────────┘  │
└─────────────────────────┘   └────────────────────────────┘
```

---

## 3. Core Components

### 3.1 API Layer (`TinyUrl.Api`)
- **ASP.NET Core 9** Web API with controller-based routing
- **Swagger / OpenAPI** documentation
- **Serilog** structured logging with request logging middleware
- **Global Exception Middleware** — maps domain exceptions to HTTP status codes
- **CORS** — configured for cross-origin access
- **Health Checks** — `/health` endpoint monitoring DB and Redis

### 3.2 Application Layer (`TinyUrl.Application`)
- **CQRS** — Commands (write) and Queries (read) are strictly separated
- **MediatR 12** — all requests dispatched through the mediator pipeline
- **FluentValidation 11** — validation logic decoupled from controllers
- **Pipeline Behaviors** — cross-cutting concerns (logging, validation) injected transparently

### 3.3 Domain Layer (`TinyUrl.Domain`)
- **Rich Domain Entities** — `ShortUrl` and `ClickEvent` encapsulate business rules
- **No external dependencies** — pure C# with no framework coupling
- **Factory Methods** — entities created through static `Create()` factories

### 3.4 Infrastructure Layer (`TinyUrl.Infrastructure`)
- **Entity Framework Core 9** — ORM with PostgreSQL (Npgsql provider)
- **Redis** — distributed cache via `StackExchange.Redis`
- **Repository Pattern** — data access abstracted behind `IUrlRepository`
- **Base62 Short Code Generator** — collision-resistant alphanumeric code generation
- **Polly** — resilience policies (retry, circuit breaker) for external dependencies

---

## 4. Data Flow Diagrams

### 4.1 Create Short URL Flow
```
Client → POST /api/urls
       → ValidationBehavior (FluentValidation)
       → CreateShortUrlHandler
            → Generate unique Base62 short code
            → ShortUrl.Create() [Domain Factory]
            → IUrlRepository.AddAsync() [EF Core → PostgreSQL]
            → ICacheService.SetAsync() [Redis — Cache-Aside Write]
       → 201 Created + ShortUrlDto
```

### 4.2 Redirect (Resolve) Flow
```
Client → GET /{shortCode}
       → ResolveShortCodeHandler
            → ICacheService.GetAsync("url:{code}") [Redis]
                 ├─ Cache HIT  → Return LongUrl immediately (< 1ms)
                 └─ Cache MISS → IUrlRepository.GetByShortCodeAsync() [PostgreSQL]
                                → Check expiry [Domain logic]
                                → IncrementClickCount()
                                → AddClickEvent()
                                → ICacheService.SetAsync() [Populate cache]
       → 302 Redirect → LongUrl
```

### 4.3 Analytics Flow
```
Client → GET /api/stats/summary
       → GetStatsSummaryHandler
            → IUrlRepository.GetTotalUrlsAsync()
            → IUrlRepository.GetTotalClicksAsync()
            → IUrlRepository.GetUrlsCreatedTodayAsync()
            → IUrlRepository.GetClicksTodayAsync()
       → StatsSummaryDto (totalUrls, totalClicks, urlsToday, clicksToday, avgClicksPerUrl)
```

---

## 5. Technology Stack

| Layer | Technology | Version |
|---|---|---|
| Runtime | .NET / C# | 9.0 / 13 |
| Framework | ASP.NET Core | 9.0 |
| ORM | Entity Framework Core | 9.0 |
| Database | PostgreSQL | 16 |
| Cache | Redis (StackExchange.Redis) | 7.x / 2.8 |
| Mediator | MediatR | 12.4 |
| Validation | FluentValidation | 11.11 |
| Resilience | Polly | 8.5 |
| Logging | Serilog | 9.0 |
| API Docs | Swagger (Swashbuckle) | 7.3 |
| Containerization | Docker + Docker Compose | — |

---

## 6. Scalability Considerations

| Concern | Strategy |
|---|---|
| High read traffic | Redis cache absorbs redirect traffic (Cache-Aside) |
| Write scalability | CQRS — read/write paths independently scalable |
| DB connection pooling | EF Core + Npgsql built-in pooling |
| Resilience | Polly retry + circuit breaker on infrastructure calls |
| Horizontal scaling | Stateless API — multiple instances behind load balancer |
| Cache invalidation | Explicit cache removal on update/delete |

---

## 7. Security Considerations

- Short code collision handled with retry loop (5 attempts)
- Soft delete — data never physically removed
- Input validation on all endpoints (FluentValidation)
- Custom alias sanitized (`^[a-zA-Z0-9_-]+$` regex)
- Expired URLs return `410 Gone` — not accessible
- Environment-based configuration (no secrets in code)
