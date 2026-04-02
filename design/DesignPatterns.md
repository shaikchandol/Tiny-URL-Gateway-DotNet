# Design Patterns Applied — TinyURL Gateway

This document describes the 12 design patterns applied in the TinyURL Gateway project, where each pattern is applied, and why it was chosen.

---

## 1. CQRS — Command Query Responsibility Segregation

**Category:** Architectural Pattern  
**Location:** `TinyUrl.Application/Commands/` and `TinyUrl.Application/Queries/`

### What it is
CQRS separates the model for reading data (Queries) from the model for writing data (Commands). Each operation has a dedicated class and handler.

### How it's applied
```
Commands (Write):                    Queries (Read):
├── CreateShortUrlCommand            ├── ListUrlsQuery
├── UpdateUrlCommand                 ├── GetUrlQuery
└── DeleteUrlCommand                 ├── ResolveShortCodeQuery
                                     ├── GetUrlClicksQuery
                                     ├── GetStatsSummaryQuery
                                     └── GetTopUrlsQuery
```

### Why
- Read and write workloads have different performance characteristics
- Queries can be independently optimized (caching, read replicas)
- Prevents bloated "God" service classes
- Scales read and write paths independently

---

## 2. Mediator Pattern

**Category:** Behavioral Pattern  
**Library:** MediatR 12  
**Location:** All Controllers → MediatR → Handlers

### What it is
Objects communicate through a central mediator rather than directly referencing each other. Reduces direct coupling between components.

### How it's applied
```csharp
// Controller dispatches — no direct dependency on handler
var result = await _mediator.Send(new CreateShortUrlCommand(...));

// Handler is resolved automatically by MediatR
public class CreateShortUrlHandler : IRequestHandler<CreateShortUrlCommand, ShortUrlDto>
```

### Why
- Controllers are thin — zero business logic
- Handlers are independently testable
- New commands/queries added without changing controllers
- Cross-cutting concerns (logging, validation) applied as pipeline behaviors

---

## 3. Repository Pattern

**Category:** Structural / Data Access Pattern  
**Location:** `IUrlRepository` (Application) ↔ `UrlRepository` (Infrastructure)

### What it is
Encapsulates data access logic behind an interface. The application layer only knows about the interface, not the ORM or database.

### How it's applied
```csharp
// Interface in Application layer (no EF reference)
public interface IUrlRepository {
    Task<ShortUrl?> GetByShortCodeAsync(string shortCode, CancellationToken ct);
    Task<ShortUrl> AddAsync(ShortUrl url, CancellationToken ct);
    Task UpdateAsync(ShortUrl url, CancellationToken ct);
    // ...9 more methods
}

// EF Core implementation in Infrastructure layer
public class UrlRepository : IUrlRepository {
    private readonly AppDbContext _db;
    // Full EF Core + LINQ implementation
}
```

### Why
- Swap databases without touching Application or Domain layers
- Business logic is not polluted with ORM specifics
- Testable — mock `IUrlRepository` in unit tests

---

## 4. Cache-Aside Pattern

**Category:** Cloud / Performance Pattern  
**Location:** `ResolveShortCodeHandler`, `CreateShortUrlHandler`, `UpdateUrlHandler`, `DeleteUrlHandler`

### What it is
The application manages the cache explicitly. On read: check cache first, fallback to DB on miss. On write: populate or invalidate cache.

### How it's applied
```
RESOLVE (Read):
  Redis.Get("url:abc1234")
      ├── HIT  → Return instantly (sub-millisecond)
      └── MISS → PostgreSQL query
                → Redis.Set("url:abc1234", longUrl, TTL=24h)
                → Return result

CREATE (Write):
  → PostgreSQL.Insert(url)
  → Redis.Set("url:{code}", longUrl, TTL)    ← Pre-warm cache

UPDATE / DELETE (Invalidate):
  → PostgreSQL.Update(url)
  → Redis.Remove("url:{code}")               ← Purge stale entry
```

### Why
- Short code resolution is the highest-traffic operation
- Redis reduces DB load by 90%+ for popular URLs
- Cache is always coherent — explicit invalidation on mutation

---

## 5. Pipeline Behavior Pattern (Decorator for MediatR)

**Category:** Behavioral Pattern  
**Location:** `TinyUrl.Application/Behaviors/`

### What it is
Intercepts every MediatR request before it reaches the handler. Similar to middleware but scoped to the application layer.

### How it's applied
```csharp
// Registered as open generics — applies to ALL commands and queries
services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

// Execution order:
// LoggingBehavior → ValidationBehavior → ActualHandler
public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
{
    public async Task<TResponse> Handle(TRequest request,
        RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        // Run validators
        var failures = _validators.Select(v => v.Validate(request))...
        if (failures.Any()) throw new ValidationException(failures);

        return await next(ct); // Pass to next behavior / handler
    }
}
```

### Why
- Validation and logging applied consistently across ALL operations
- Handlers stay focused on business logic only
- Adding new cross-cutting concerns requires zero handler changes

---

## 6. Factory Method Pattern

**Category:** Creational Pattern  
**Location:** `ShortUrl.Create()`, `ClickEvent.Create()` in Domain entities

### What it is
A static factory method controls object creation, enforcing invariants before the object exists.

### How it's applied
```csharp
// Private constructor — cannot create with `new ShortUrl()`
private ShortUrl() { }

// Only valid creation path
public static ShortUrl Create(string shortCode, string longUrl,
    string? customAlias = null, DateTimeOffset? expiresAt = null)
{
    return new ShortUrl
    {
        Id = Guid.NewGuid(),         // Always a new ID
        ShortCode = shortCode,
        LongUrl = longUrl,
        ClickCount = 0,              // Always starts at 0
        IsDeleted = false,           // Always starts active
        CreatedAt = DateTimeOffset.UtcNow,
        UpdatedAt = DateTimeOffset.UtcNow
    };
}
```

### Why
- Guarantees all entities are always in a valid initial state
- Business rules baked into creation (e.g., ClickCount always starts at 0)
- Prevents invalid object construction from external code

---

## 7. Strategy Pattern

**Category:** Behavioral Pattern  
**Location:** `IShortCodeGenerator` ↔ `Base62ShortCodeGenerator`

### What it is
Defines a family of algorithms, encapsulates each one, and makes them interchangeable.

### How it's applied
```csharp
// Strategy interface
public interface IShortCodeGenerator {
    string Generate(int length = 7);
}

// Concrete strategy — Base62
public class Base62ShortCodeGenerator : IShortCodeGenerator {
    private const string Alphabet = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
    public string Generate(int length = 7) { /* Base62 sampling */ }
}

// Registered as singleton — swappable via DI
services.AddSingleton<IShortCodeGenerator, Base62ShortCodeGenerator>();
```

### Why
- Swap to NanoID, UUID-based, or sequential generation without touching business logic
- Different strategies for different use cases (e.g., shorter codes for premium users)
- Easily unit-tested in isolation

---

## 8. Soft Delete Pattern

**Category:** Data Management Pattern  
**Location:** `ShortUrl.SoftDelete()`, EF Core Global Query Filter

### What it is
Records are never physically deleted. A boolean flag marks them as deleted and a global filter hides them from all queries.

### How it's applied
```csharp
// Domain entity
public void SoftDelete() {
    IsDeleted = true;
    UpdatedAt = DateTimeOffset.UtcNow;
}

// EF Core global filter — applied to ALL queries automatically
builder.Entity<ShortUrl>(e => {
    e.HasQueryFilter(x => !x.IsDeleted);  // Always filtered
});

// Override filter when needed (e.g., alias uniqueness check)
await _db.Urls.IgnoreQueryFilters()
              .AnyAsync(u => u.ShortCode == shortCode);
```

### Why
- Audit trail preserved — deleted data still queryable by admins
- Accidental deletion is recoverable
- Global filter ensures deleted items never leak into API responses

---

## 9. Dependency Injection / Inversion of Control

**Category:** Structural / Architectural Pattern  
**Location:** `DependencyInjection.cs` in each project layer

### What it is
High-level modules depend on abstractions, not concrete implementations. The DI container wires everything at startup.

### How it's applied
```csharp
// Application layer registers itself
services.AddApplication();  // MediatR + Validators + Behaviors

// Infrastructure layer registers itself
services.AddInfrastructure(config);
    → services.AddDbContext<AppDbContext>(...)
    → services.AddStackExchangeRedisCache(...)
    → services.AddScoped<IUrlRepository, UrlRepository>()
    → services.AddScoped<ICacheService, RedisCacheService>()
    → services.AddSingleton<IShortCodeGenerator, Base62ShortCodeGenerator>()
```

### Why
- Clean Architecture enforced — inner layers never reference outer layers
- Swap implementations (e.g., in-memory cache for tests) via single DI registration change
- Testability — all dependencies mockable through interfaces

---

## 10. Decorator Pattern (via Middleware)

**Category:** Structural Pattern  
**Location:** `ExceptionHandlingMiddleware`, Serilog request logging

### What it is
Wraps an object with additional behavior without modifying the original.

### How it's applied
```csharp
// Each middleware wraps the entire request pipeline
app.UseMiddleware<ExceptionHandlingMiddleware>();  // Outer: catches all exceptions
app.UseSerilogRequestLogging();                   // Middle: logs all requests
app.UseCors();                                    // Inner: applies CORS headers
app.MapControllers();                             // Core: actual controller logic

// ExceptionHandlingMiddleware wraps every request
public async Task InvokeAsync(HttpContext context) {
    try {
        await _next(context);  // Delegate to inner pipeline
    }
    catch (KeyNotFoundException ex) { /* → 404 */ }
    catch (ValidationException ex)  { /* → 400 */ }
    catch (Exception ex)            { /* → 500 */ }
}
```

### Why
- All exceptions handled in one place — no try/catch in controllers
- Consistent error response format across all endpoints
- New exception types handled by extending one class

---

## 11. Observer Pattern (via EF Core Change Tracking)

**Category:** Behavioral Pattern  
**Location:** `AppDbContext` + `ShortUrl` entity domain mutations

### What it is
When an object's state changes, all dependents are notified automatically.

### How it's applied
```csharp
// Domain entity mutates state and tracks changes internally
public void IncrementClickCount() {
    ClickCount++;
    UpdatedAt = DateTimeOffset.UtcNow;   // Timestamp updated automatically
}

// EF Core change tracker observes entity state
// UrlRepository.UpdateAsync() calls SaveChangesAsync()
// EF Core detects all property changes and generates minimal SQL UPDATE
_db.Urls.Update(url);
await _db.SaveChangesAsync(ct);
// → UPDATE urls SET click_count=43, updated_at='...' WHERE id='...'
```

### Why
- `UpdatedAt` always reflects actual last modification time
- No risk of forgetting to update audit timestamps
- EF Core only updates changed columns — minimal SQL overhead

---

## 12. Circuit Breaker Pattern

**Category:** Resilience Pattern  
**Library:** Polly 8  
**Location:** `TinyUrl.Infrastructure` (Polly referenced for resilience policies)

### What it is
Detects repeated failures and "opens" the circuit to prevent cascading failures. After a timeout period, allows a test request through (half-open state).

### States
```
CLOSED → Normal operation, requests flow through
   │
   │ (Failure threshold reached)
   ▼
OPEN   → All requests fail immediately (fast fail, no DB hit)
   │
   │ (After reset timeout)
   ▼
HALF-OPEN → One test request allowed
   │
   ├── Success → CLOSED (back to normal)
   └── Failure → OPEN  (remain open)
```

### How it's applied
```csharp
// Polly policy (configurable in DI)
var retryPolicy = Policy
    .Handle<Exception>()
    .WaitAndRetryAsync(3, attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)));

var circuitBreakerPolicy = Policy
    .Handle<Exception>()
    .CircuitBreakerAsync(
        exceptionsAllowedBeforeBreaking: 5,
        durationOfBreak: TimeSpan.FromSeconds(30)
    );

// Wrap both for infrastructure calls
var resilientPolicy = Policy.WrapAsync(retryPolicy, circuitBreakerPolicy);
```

### Why
- Prevents Redis/PostgreSQL outages from taking down the entire API
- Fail fast → better user experience than waiting for timeouts
- Self-healing — system automatically recovers when dependencies come back

---

## Summary Table

| # | Pattern | Category | Layer | Purpose |
|---|---|---|---|---|
| 1 | CQRS | Architectural | Application | Separate read/write models |
| 2 | Mediator | Behavioral | Application ↔ API | Decouple controllers from handlers |
| 3 | Repository | Structural | Application ↔ Infrastructure | Abstract data access |
| 4 | Cache-Aside | Cloud/Performance | Infrastructure | Reduce DB load on reads |
| 5 | Pipeline Behavior | Behavioral | Application | Cross-cutting concerns |
| 6 | Factory Method | Creational | Domain | Enforce valid entity creation |
| 7 | Strategy | Behavioral | Infrastructure | Swappable short code algorithms |
| 8 | Soft Delete | Data Management | Domain + Infrastructure | Safe non-destructive deletion |
| 9 | Dependency Injection | Structural | All layers | Loose coupling + testability |
| 10 | Decorator | Structural | API | Middleware pipeline composition |
| 11 | Observer | Behavioral | Domain + Infrastructure | Automatic state change tracking |
| 12 | Circuit Breaker | Resilience | Infrastructure | Prevent cascading failures |
