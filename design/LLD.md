# Low-Level Design (LLD) — TinyURL Gateway

## 1. Project Structure

```
TinyUrl.sln
├── src/
│   ├── TinyUrl.Domain/                         # Enterprise Business Rules
│   │   └── Entities/
│   │       ├── ShortUrl.cs                     # Core domain entity
│   │       └── ClickEvent.cs                   # Click tracking entity
│   │
│   ├── TinyUrl.Application/                    # Application Business Rules
│   │   ├── Commands/
│   │   │   ├── CreateShortUrl/
│   │   │   │   ├── CreateShortUrlCommand.cs    # MediatR IRequest<ShortUrlDto>
│   │   │   │   ├── CreateShortUrlHandler.cs    # Business logic handler
│   │   │   │   └── CreateShortUrlValidator.cs  # FluentValidation rules
│   │   │   ├── DeleteUrl/
│   │   │   │   ├── DeleteUrlCommand.cs
│   │   │   │   └── DeleteUrlHandler.cs
│   │   │   └── UpdateUrl/
│   │   │       ├── UpdateUrlCommand.cs
│   │   │       └── UpdateUrlHandler.cs
│   │   ├── Queries/
│   │   │   ├── GetUrl/
│   │   │   ├── ListUrls/
│   │   │   ├── GetUrlClicks/
│   │   │   ├── GetStatsSummary/
│   │   │   ├── GetTopUrls/
│   │   │   └── ResolveShortCode/
│   │   ├── Behaviors/
│   │   │   ├── LoggingBehavior.cs              # IPipelineBehavior — cross-cutting log
│   │   │   └── ValidationBehavior.cs           # IPipelineBehavior — auto-validation
│   │   ├── DTOs/
│   │   │   └── ShortUrlDto.cs                  # Data Transfer Objects (records)
│   │   ├── Interfaces/
│   │   │   ├── IUrlRepository.cs               # Repository abstraction
│   │   │   ├── ICacheService.cs                # Cache abstraction
│   │   │   └── IShortCodeGenerator.cs          # Generator abstraction
│   │   └── DependencyInjection.cs              # Extension method for DI registration
│   │
│   ├── TinyUrl.Infrastructure/                 # Frameworks & Drivers
│   │   ├── Data/
│   │   │   └── AppDbContext.cs                 # EF Core DbContext
│   │   ├── Repositories/
│   │   │   └── UrlRepository.cs                # EF Core implementation
│   │   ├── Services/
│   │   │   ├── RedisCacheService.cs            # StackExchange.Redis implementation
│   │   │   └── Base62ShortCodeGenerator.cs     # Short code generator
│   │   └── DependencyInjection.cs
│   │
│   └── TinyUrl.Api/                            # Interface Adapters
│       ├── Controllers/
│       │   ├── UrlsController.cs               # /api/urls CRUD
│       │   ├── StatsController.cs              # /api/stats
│       │   └── RedirectController.cs           # /{shortCode} redirect
│       ├── Middleware/
│       │   └── ExceptionHandlingMiddleware.cs  # Global error handler
│       ├── Program.cs                          # App bootstrap + DI
│       ├── appsettings.json
│       └── Dockerfile
```

---

## 2. Domain Entity Design

### 2.1 ShortUrl Entity

```csharp
public class ShortUrl
{
    // Private setters — full encapsulation
    public Guid Id { get; private set; }
    public string ShortCode { get; private set; }       // Unique identifier (Base62)
    public string LongUrl { get; private set; }         // The original URL
    public string? CustomAlias { get; private set; }    // Optional user-defined alias
    public int ClickCount { get; private set; }         // Denormalized counter
    public DateTimeOffset? ExpiresAt { get; private set; }
    public bool IsDeleted { get; private set; }         // Soft delete flag
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    // Navigation property
    public ICollection<ClickEvent> ClickEvents { get; private set; }

    // Factory Method — only way to create a ShortUrl
    public static ShortUrl Create(string shortCode, string longUrl,
        string? customAlias = null, DateTimeOffset? expiresAt = null)

    // Domain behaviors
    public void IncrementClickCount()     // Updates ClickCount + UpdatedAt
    public void UpdateLongUrl(string url) // Validates and updates LongUrl
    public void UpdateExpiry(DateTimeOffset? expiry)
    public void SoftDelete()              // Sets IsDeleted = true
    public bool IsExpired()               // ExpiresAt < UtcNow
}
```

### 2.2 ClickEvent Entity

```csharp
public class ClickEvent
{
    public Guid Id { get; private set; }
    public Guid UrlId { get; private set; }        // FK → ShortUrl.Id
    public string ShortCode { get; private set; }  // Denormalized for fast queries
    public DateTimeOffset ClickedAt { get; private set; }

    public ShortUrl? ShortUrl { get; private set; }

    public static ClickEvent Create(Guid urlId, string shortCode)
}
```

---

## 3. Database Schema

### 3.1 `urls` Table

| Column | Type | Constraints |
|---|---|---|
| `id` | `uuid` | PRIMARY KEY |
| `short_code` | `varchar(50)` | UNIQUE, NOT NULL |
| `long_url` | `text` | NOT NULL |
| `custom_alias` | `varchar(50)` | NULLABLE |
| `click_count` | `integer` | DEFAULT 0 |
| `expires_at` | `timestamptz` | NULLABLE |
| `is_deleted` | `boolean` | DEFAULT false |
| `created_at` | `timestamptz` | NOT NULL |
| `updated_at` | `timestamptz` | NOT NULL |

**Indexes:**
- Primary Key on `id`
- Unique Index on `short_code`
- Global Query Filter: `WHERE is_deleted = false` (applied automatically by EF Core)

### 3.2 `click_events` Table

| Column | Type | Constraints |
|---|---|---|
| `id` | `uuid` | PRIMARY KEY |
| `url_id` | `uuid` | FK → urls.id |
| `short_code` | `varchar(50)` | NOT NULL |
| `clicked_at` | `timestamptz` | NOT NULL |

**Indexes:**
- Primary Key on `id`
- Foreign Key on `url_id`

### 3.3 Entity Relationship Diagram

```
┌─────────────────────────────┐         ┌──────────────────────────┐
│           urls              │         │       click_events       │
├─────────────────────────────┤         ├──────────────────────────┤
│ id (uuid) PK                │◄────────│ id (uuid) PK             │
│ short_code (varchar) UNIQUE │  1 : N  │ url_id (uuid) FK         │
│ long_url (text)             │         │ short_code (varchar)     │
│ custom_alias (varchar)      │         │ clicked_at (timestamptz) │
│ click_count (int)           │         └──────────────────────────┘
│ expires_at (timestamptz)    │
│ is_deleted (bool)           │
│ created_at (timestamptz)    │
│ updated_at (timestamptz)    │
└─────────────────────────────┘
```

---

## 4. API Contract

### Base URL: `http://localhost:8080`

#### `GET /api/urls`
**Query params:** `page` (default: 1), `limit` (default: 20), `search` (optional)
```json
{
  "urls": [
    {
      "id": "uuid",
      "shortCode": "abc1234",
      "longUrl": "https://example.com/very/long/url",
      "customAlias": null,
      "clickCount": 42,
      "expiresAt": null,
      "createdAt": "2025-01-01T00:00:00Z",
      "updatedAt": "2025-01-01T00:00:00Z"
    }
  ],
  "total": 100,
  "page": 1,
  "limit": 20
}
```

#### `POST /api/urls`
```json
// Request
{ "longUrl": "https://example.com", "customAlias": "mylink", "expiresAt": null }

// Response 201
{ "id": "uuid", "shortCode": "mylink", "longUrl": "...", "clickCount": 0, ... }
```

#### `GET /api/urls/{id}`
```json
{ "id": "uuid", "shortCode": "abc1234", "longUrl": "...", "clickCount": 5, ... }
```

#### `PATCH /api/urls/{id}`
```json
// Request
{ "longUrl": "https://new-url.com", "expiresAt": "2025-12-31T00:00:00Z" }
```

#### `DELETE /api/urls/{id}` → `204 No Content`

#### `GET /api/urls/{id}/clicks?days=30`
```json
{
  "shortCode": "abc1234",
  "totalClicks": 150,
  "data": [
    { "date": "2025-01-01", "clicks": 12 },
    { "date": "2025-01-02", "clicks": 8 }
  ]
}
```

#### `GET /api/stats/summary`
```json
{
  "totalUrls": 500,
  "totalClicks": 25000,
  "urlsCreatedToday": 12,
  "clicksToday": 340,
  "avgClicksPerUrl": 50.0
}
```

#### `GET /api/stats/top-urls?limit=10`
```json
[
  { "shortCode": "abc1234", "longUrl": "...", "clickCount": 9500, ... }
]
```

#### `GET /{shortCode}` → `302 Redirect`
- Resolves short code, records click event, redirects to `longUrl`
- Returns `404` if not found
- Returns `410 Gone` if expired

---

## 5. CQRS Command / Query Map

| Operation | Type | Handler | Validator |
|---|---|---|---|
| Create URL | Command | `CreateShortUrlHandler` | `CreateShortUrlValidator` |
| Update URL | Command | `UpdateUrlHandler` | — |
| Delete URL | Command | `DeleteUrlHandler` | — |
| List URLs | Query | `ListUrlsHandler` | — |
| Get URL by ID | Query | `GetUrlHandler` | — |
| Resolve short code | Query | `ResolveShortCodeHandler` | — |
| Get click time-series | Query | `GetUrlClicksHandler` | — |
| Get stats summary | Query | `GetStatsSummaryHandler` | — |
| Get top URLs | Query | `GetTopUrlsHandler` | — |

---

## 6. MediatR Pipeline

```
Request
   │
   ▼
LoggingBehavior          ← Logs request name + timing
   │
   ▼
ValidationBehavior       ← Runs FluentValidation; throws on failure
   │
   ▼
CommandHandler           ← Business logic
   │
   ▼
Response
```

---

## 7. Cache Strategy (Cache-Aside)

```
READ (Resolve):
  1. Check Redis key: "url:{shortCode}"
  2. HIT  → Return cached LongUrl (skip DB)
  3. MISS → Query PostgreSQL
           → Update Redis (TTL: 24h or until expiry)
           → Return LongUrl

WRITE (Create):
  1. Insert into PostgreSQL
  2. Pre-warm Redis cache with new entry

INVALIDATE (Update / Delete):
  1. Update / soft-delete in PostgreSQL
  2. Remove Redis key: "url:{shortCode}"
```

---

## 8. Short Code Generation Algorithm

```
Base62 Alphabet: 0-9, A-Z, a-z  (62 characters)
Default Length:  7 characters
Total Combos:    62^7 = ~3.5 trillion unique codes

Algorithm:
  1. Randomly sample 7 characters from alphabet
  2. Check uniqueness in DB (ShortCodeExistsAsync)
  3. Retry up to 5 times if collision
  4. Custom aliases bypass generation — direct uniqueness check

Collision Probability (1M URLs): < 0.00003%
```

---

## 9. Error Handling

| Exception | HTTP Status | Error Code |
|---|---|---|
| `KeyNotFoundException` | 404 Not Found | `NOT_FOUND` |
| `InvalidOperationException` (expired) | 410 Gone | `CONFLICT` |
| `InvalidOperationException` (alias taken) | 409 Conflict | `CONFLICT` |
| `ValidationException` | 400 Bad Request | `VALIDATION_ERROR` |
| Unhandled `Exception` | 500 Internal Server Error | `INTERNAL_ERROR` |

**Error Response Format:**
```json
{ "error": "NOT_FOUND", "message": "URL with id '...' not found." }
```
