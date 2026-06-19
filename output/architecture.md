# Architecture -- ImmoScorer

## 1. Overview

```
                          +---------------------+
                          |   React 18 (Vite)   |
                          |  SPA / TypeScript    |
                          +----------+----------+
                                     |  HTTP / JSON
                                     v
+====================================================================+
|                       .NET 9 Modular Monolith                      |
|                                                                    |
|  +--------------------------------------------------------------+  |
|  |                      Api Layer                                |  |
|  |  Minimal API endpoints (POST /searches, GET /listings, ...)   |  |
|  |  Serilog request logging, global error handler                |  |
|  +------+------------------------------------------+------------+  |
|         |  IRequest<Result<T>>                      |              |
|         v                                           v              |
|  +--------------------------------------------------------------+  |
|  |                  Application Layer                            |  |
|  |  Commands: RunSearchCommand, TriggerScrapingCommand           |  |
|  |  Queries:  GetListingsQuery, GetListingDetailQuery            |  |
|  |  Handlers: orchestrate domain logic, call infra interfaces    |  |
|  |  Pipeline behaviors: ValidationBehavior, LoggingBehavior      |  |
|  +------+------------------+------------------+-----------------+  |
|         |                  |                  |                    |
|         v                  v                  v                    |
|  +--------------------------------------------------------------+  |
|  |                    Domain Layer                               |  |
|  |  Entities: Search, Listing, ScoreBreakdown                    |  |
|  |  Value Objects: SearchCriteria, Address, Price, Score          |  |
|  |  Interfaces: IListingScraper, IScoringEngine,                 |  |
|  |              IDvfReferenceDataProvider, IScrapingJobQueue      |  |
|  +--------------------------------------------------------------+  |
|         ^                  ^                  ^                    |
|         |                  |                  |                    |
|  +--------------------------------------------------------------+  |
|  |                 Infrastructure Layer                          |  |
|  |                                                               |  |
|  |  +-----------------+  +------------------+  +---------------+ |  |
|  |  | LeBonCoinScraper|  | SeLogerScraper   |  | DvfDataClient | |  |
|  |  | (Playwright)    |  | (Playwright)     |  | (HttpClient)  | |  |
|  |  +-----------------+  +------------------+  +-------+-------+ |  |
|  |                                                     |         |  |
|  |  +-----------------+  +------------------+          |         |  |
|  |  | ScoringEngine   |  | AzureStorageQueue|          |         |  |
|  |  | (impl)          |  | JobQueue         |          |         |  |
|  |  +-----------------+  +------------------+          |         |  |
|  |                                                     |         |  |
|  |  +-----------------+  +------------------+          |         |  |
|  |  | EF Core 9       |  | AntiBot Service  |          |         |  |
|  |  | (SQLite dev)     |  | (UA rotation,    |          |         |  |
|  |  |                  |  |  delays, backoff) |          |         |  |
|  |  +-----------------+  +------------------+          |         |  |
|  +--------------------------------------------------------------+  |
+====================================================================+
         |                      |                        |
         v                      v                        v
  +-------------+    +-------------------+     +------------------+
  | SQLite (dev)|    | Azure Storage     |     | data.gouv.fr     |
  | Table       |    | Queue             |     | DVF API          |
  | Storage     |    | (scraping jobs)   |     | (open data)      |
  | (cloud)     |    |                   |     |                  |
  +-------------+    +-------------------+     +------------------+


Data flow — happy path:
1. User creates SearchCriteria via POST /searches
2. Api dispatches RunSearchCommand -> Handler persists Search, enqueues scraping jobs
3. Queue consumer dequeues jobs, invokes IListingScraper (LeBonCoin / SeLoger)
4. Each scraped listing is enriched via IDvfReferenceDataProvider (DVF median price/m2)
5. IScoringEngine computes Score (0-100) + breakdown per listing
6. Listings + scores are persisted (EF Core / SQLite or Table Storage)
7. React SPA fetches GET /listings?searchId=X&sort=score&minScore=60 etc.
```

## 2. Stack & technical choices

| Need | Solution (.NET / Azure) | Justification |
|---|---|---|
| Runtime | .NET 9, C# 13, Nullable enabled, TreatWarningsAsErrors | Reference stack mandate. Ensures compile-time safety and zero-warning builds. |
| Architecture pattern | Clean Architecture (Domain / Application / Infrastructure / Api) | Reference stack mandate. Dependencies point inward toward Domain. Enables testability and substitution of infrastructure. |
| CQRS / Mediation | MediatR 12+ with pipeline behaviors | Reference stack mandate. Decouples handlers from endpoints. Allows cross-cutting concerns (validation, logging) as behaviors. |
| Error handling | Result<T> pattern (no business exceptions) | Reference stack mandate. Explicit success/failure flow, composable, no exception-driven control flow. |
| Web scraping | Microsoft.Playwright for .NET (headless Chromium) | Brief requirement. Handles JavaScript-heavy sites (LeBonCoin, SeLoger). Supports realistic browser fingerprints and stealth mode. |
| Anti-ban orchestration | Custom AntiBotService (UA rotation, random delays, exponential backoff, robots.txt parser, captcha detection) | Brief requirement. Each strategy is a composable concern injected into scrapers. Keeps scraping respectful and reduces ban risk. |
| DVF reference data | HttpClient + local file cache (JSON/CSV on disk) | Brief requirement. DVF data changes quarterly at most. Local cache avoids repeated large downloads. Simple HttpClient call to data.gouv.fr API. |
| Scoring | IScoringEngine in-process service | Brief requirement. Isolated, testable, no external dependency. Weighted formula comparing listing price/m2 to DVF reference with bonuses/maluses. |
| Persistence (dev) | EF Core 9 + SQLite | Brief explicitly accepts SQLite for dev. Zero-cost local database, full EF Core feature set, easy migration to cloud. |
| Persistence (cloud) | Azure Table Storage | Brief mandates lightweight cloud persistence. Cost-effective for a single-user POC. Partition key = SearchId, Row key = ListingId. |
| Job queue | Azure Storage Queue | Brief mandates this over Service Bus. Sufficient for POC throughput. Cheap, serverless, simple SDK. |
| Configuration / Secrets | appsettings.json + User Secrets (dev) | Brief explicitly forbids Key Vault for POC scope. User Secrets keeps sensitive config out of source control. No secrets hardcoded. |
| Logging / Observability | Serilog (console + file sinks) | Reference stack mandate. Structured logging with correlation IDs for request tracing. File sink for local debugging. |
| Frontend | React 18 + Vite + TypeScript | Brief requirement. Fast HMR dev experience, type safety, modern SPA tooling. |
| HTTP client (frontend) | fetch API (native) or Axios | Minimal dependency. Communicates with backend Minimal API endpoints. |
| Hosting (cloud) | Azure Container Apps (scale-to-zero) | Brief requirement. Pay-per-use, no cluster management, Docker-based deployment. Scale-to-zero eliminates idle cost. |
| Input validation | FluentValidation via MediatR pipeline behavior | Validates commands/queries before reaching handlers. Clean separation of validation logic from business logic. |
| Tests | xUnit + NSubstitute + FluentAssertions + WebApplicationFactory | Reference stack mandate. NSubstitute for mocking interfaces, FluentAssertions for readable assertions, WebApplicationFactory for integration tests. |

## 3. Contracts & interfaces

### Domain layer -- Core abstractions

```csharp
namespace ImmoScorer.Domain.Scraping;

/// <summary>
/// Abstraction for scraping listings from a real-estate source.
/// One implementation per source (LeBonCoin, SeLoger, mock/fixture).
/// </summary>
public interface IListingScraper
{
    string SourceName { get; }
    Task<Result<IReadOnlyList<ScrapedListing>>> ScrapeAsync(
        SearchCriteria criteria,
        CancellationToken cancellationToken = default);
}
```

```csharp
namespace ImmoScorer.Domain.Scoring;

/// <summary>
/// Computes an opportunity score for a listing given reference data.
/// </summary>
public interface IScoringEngine
{
    Score ComputeScore(Listing listing, DvfReferenceData referenceData);
}
```

```csharp
namespace ImmoScorer.Domain.ReferenceData;

/// <summary>
/// Provides DVF reference price per m2 for a geographic area.
/// </summary>
public interface IDvfReferenceDataProvider
{
    Task<Result<DvfReferenceData>> GetReferenceDataAsync(
        string postalCode,
        PropertyType propertyType,
        CancellationToken cancellationToken = default);
}
```

```csharp
namespace ImmoScorer.Domain.Scraping;

/// <summary>
/// Abstraction over the job queue for distributing scraping work.
/// </summary>
public interface IScrapingJobQueue
{
    Task EnqueueAsync(ScrapingJob job, CancellationToken cancellationToken = default);
    Task<ScrapingJob?> DequeueAsync(CancellationToken cancellationToken = default);
}
```

```csharp
namespace ImmoScorer.Domain.Scraping;

/// <summary>
/// Anti-bot protection service: delays, UA rotation, backoff, robots.txt.
/// </summary>
public interface IAntiBotService
{
    Task<string> GetRandomUserAgentAsync();
    Task DelayBeforeRequestAsync(string domain, CancellationToken cancellationToken = default);
    Task HandleResponseErrorAsync(string domain, int statusCode, CancellationToken cancellationToken = default);
    Task<bool> IsAllowedByRobotsTxtAsync(string url, CancellationToken cancellationToken = default);
    Task<bool> IsCaptchaDetectedAsync(string pageContent);
}
```

### Application layer -- Commands and Queries

```csharp
namespace ImmoScorer.Application.Searches.Commands;

public sealed record RunSearchCommand(SearchCriteria Criteria)
    : IRequest<Result<SearchId>>;
```

```csharp
namespace ImmoScorer.Application.Searches.Commands;

public sealed class RunSearchCommandHandler
    : IRequestHandler<RunSearchCommand, Result<SearchId>>;
```

```csharp
namespace ImmoScorer.Application.Scraping.Commands;

public sealed record TriggerScrapingCommand(SearchId SearchId)
    : IRequest<Result<Unit>>;
```

```csharp
namespace ImmoScorer.Application.Scraping.Commands;

public sealed class TriggerScrapingCommandHandler
    : IRequestHandler<TriggerScrapingCommand, Result<Unit>>;
```

```csharp
namespace ImmoScorer.Application.Scraping.Commands;

public sealed record ProcessScrapingJobCommand(ScrapingJob Job)
    : IRequest<Result<int>>; // returns count of listings scraped
```

```csharp
namespace ImmoScorer.Application.Scraping.Commands;

public sealed class ProcessScrapingJobCommandHandler
    : IRequestHandler<ProcessScrapingJobCommand, Result<int>>;
```

```csharp
namespace ImmoScorer.Application.Listings.Queries;

public sealed record GetListingsQuery(
    SearchId SearchId,
    ListingFilter? Filter,
    ListingSortOrder SortOrder,
    int Page,
    int PageSize)
    : IRequest<Result<PaginatedList<ListingDto>>>;
```

```csharp
namespace ImmoScorer.Application.Listings.Queries;

public sealed class GetListingsQueryHandler
    : IRequestHandler<GetListingsQuery, Result<PaginatedList<ListingDto>>>;
```

```csharp
namespace ImmoScorer.Application.Listings.Queries;

public sealed record GetListingDetailQuery(ListingId ListingId)
    : IRequest<Result<ListingDetailDto>>;
```

```csharp
namespace ImmoScorer.Application.Listings.Queries;

public sealed class GetListingDetailQueryHandler
    : IRequestHandler<GetListingDetailQuery, Result<ListingDetailDto>>;
```

```csharp
namespace ImmoScorer.Application.Searches.Queries;

public sealed record GetSavedSearchesQuery()
    : IRequest<Result<IReadOnlyList<SavedSearchDto>>>;
```

### Application layer -- DTOs

```csharp
namespace ImmoScorer.Application.Listings.Dtos;

public sealed record ListingDto(
    Guid Id,
    string Title,
    string Source,
    string City,
    string PostalCode,
    decimal Price,
    decimal Area,
    decimal PricePerM2,
    decimal ReferencePricePerM2,
    int Score,
    string OriginalUrl,
    DateTime ScrapedAt);
```

```csharp
namespace ImmoScorer.Application.Listings.Dtos;

public sealed record ListingDetailDto(
    Guid Id,
    string Title,
    string Description,
    string Source,
    string City,
    string PostalCode,
    decimal Price,
    decimal Area,
    int? Rooms,
    int? Floor,
    string? EnergyRating,
    decimal PricePerM2,
    decimal ReferencePricePerM2,
    int Score,
    ScoreBreakdownDto ScoreBreakdown,
    string OriginalUrl,
    DateTime ScrapedAt);
```

```csharp
namespace ImmoScorer.Application.Listings.Dtos;

public sealed record ScoreBreakdownDto(
    int PriceGapScore,
    int AreaScore,
    int FloorScore,
    int EnergyScore,
    int TotalScore);
```

### Application layer -- Pipeline behaviors

```csharp
namespace ImmoScorer.Application.Common.Behaviors;

public sealed class ValidationBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>;
```

```csharp
namespace ImmoScorer.Application.Common.Behaviors;

public sealed class LoggingBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>;
```

### Application layer -- Persistence abstractions

```csharp
namespace ImmoScorer.Application.Common.Persistence;

public interface IImmoScorerDbContext
{
    DbSet<Search> Searches { get; }
    DbSet<Listing> Listings { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
```

### Infrastructure layer -- Key implementations (signatures only)

```csharp
namespace ImmoScorer.Infrastructure.Scraping;

public sealed class LeBonCoinScraper : IListingScraper;
```

```csharp
namespace ImmoScorer.Infrastructure.Scraping;

public sealed class SeLogerScraper : IListingScraper;
```

```csharp
namespace ImmoScorer.Infrastructure.Scraping;

public sealed class FixtureScraper : IListingScraper;
// Fallback mock scraper returning static test data when live sites block.
```

```csharp
namespace ImmoScorer.Infrastructure.Scoring;

public sealed class WeightedScoringEngine : IScoringEngine;
```

```csharp
namespace ImmoScorer.Infrastructure.ReferenceData;

public sealed class DvfDataGouvrClient : IDvfReferenceDataProvider;
// Fetches DVF CSV from data.gouv.fr, parses and caches locally.
```

```csharp
namespace ImmoScorer.Infrastructure.Queue;

public sealed class AzureStorageQueueJobQueue : IScrapingJobQueue;
// Azure Storage Queue implementation. Falls back to in-memory queue in dev.
```

```csharp
namespace ImmoScorer.Infrastructure.Queue;

public sealed class InMemoryJobQueue : IScrapingJobQueue;
// In-memory queue for local development without Azure.
```

```csharp
namespace ImmoScorer.Infrastructure.AntiBot;

public sealed class AntiBotService : IAntiBotService;
```

```csharp
namespace ImmoScorer.Infrastructure.Persistence;

public sealed class ImmoScorerDbContext : DbContext, IImmoScorerDbContext;
```

## 4. Data model

### Entities

**Search** (aggregate root)
- `SearchId Id` (strongly-typed ID, Guid wrapper)
- `SearchCriteria Criteria` (owned value object)
- `SearchStatus Status` (enum: Created, InProgress, Completed, Failed)
- `DateTime CreatedAt`
- `DateTime? CompletedAt`
- Navigation: `ICollection<Listing> Listings`

**Listing** (entity, belongs to Search)
- `ListingId Id` (strongly-typed ID, Guid wrapper)
- `SearchId SearchId` (FK)
- `string Title`
- `string? Description`
- `string Source` (e.g. "LeBonCoin", "SeLoger")
- `Address Address` (owned value object)
- `Price Price` (owned value object)
- `decimal Area` (m2)
- `int? Rooms`
- `int? Floor`
- `string? EnergyRating` (DPE: A-G)
- `decimal PricePerM2` (computed: Price.Amount / Area)
- `decimal ReferencePricePerM2` (from DVF)
- `Score Score` (owned value object)
- `ScoreBreakdown ScoreBreakdown` (owned value object)
- `string OriginalUrl`
- `string ExternalId` (deduplication key from source)
- `DateTime ScrapedAt`

### Value Objects

**SearchCriteria**
- `string City`
- `string PostalCode`
- `PropertyType PropertyType` (enum: Apartment, House, Any)
- `decimal? MinPrice`
- `decimal? MaxPrice`
- `decimal? MinArea`
- `decimal? MaxArea`
- `int? MinRooms`
- `int? MaxRooms`

**Address**
- `string City`
- `string PostalCode`
- `string? Neighborhood`

**Price**
- `decimal Amount`
- `string Currency` (default "EUR")

**Score**
- `int Value` (0-100, clamped)

**ScoreBreakdown**
- `int PriceGapScore` (0-60, dominant factor)
- `int AreaScore` (0-15)
- `int FloorScore` (0-10)
- `int EnergyScore` (0-15)

**DvfReferenceData** (not persisted as entity, used transiently)
- `string PostalCode`
- `PropertyType PropertyType`
- `decimal MedianPricePerM2`
- `int SampleCount`
- `DateTime DataAsOf`

**ScrapedListing** (transient, returned by IListingScraper before enrichment)
- `string Title`
- `string? Description`
- `string Source`
- `string City`
- `string PostalCode`
- `decimal Price`
- `decimal Area`
- `int? Rooms`
- `int? Floor`
- `string? EnergyRating`
- `string OriginalUrl`
- `string ExternalId`

**ScrapingJob** (queue message)
- `Guid JobId`
- `SearchId SearchId`
- `string SourceName` (which scraper to use)
- `SearchCriteria Criteria`

### Enums

```csharp
public enum PropertyType { Any, Apartment, House }
public enum SearchStatus { Created, InProgress, Completed, Failed }
public enum ListingSortOrder { ScoreDescending, PriceAscending, PriceDescending, PricePerM2Ascending, DateDescending }
```

### Strongly-typed IDs

```csharp
public readonly record struct SearchId(Guid Value);
public readonly record struct ListingId(Guid Value);
```

### EF Core persistence strategy

- **Provider**: SQLite for local development (`Data Source=immoscorer.db`). Azure Table Storage for cloud (via a separate repository adapter, not EF).
- **Owned types**: `SearchCriteria`, `Address`, `Price`, `Score`, `ScoreBreakdown` are configured as EF Core owned types, stored as columns in the parent entity table (no separate tables).
- **Value conversions**:
  - `SearchId` and `ListingId` use `.HasConversion(id => id.Value, g => new SearchId(g))` and similar.
  - `PropertyType`, `SearchStatus`, `ListingSortOrder` enums stored as strings via `.HasConversion<string>()`.
- **Indexes**:
  - `Listing`: index on `(SearchId, Score.Value DESC)` for the default sorted query.
  - `Listing`: unique index on `(Source, ExternalId)` for deduplication.
  - `Search`: index on `CreatedAt DESC`.
- **Relationships**: `Search` 1-->* `Listing` with cascade delete.

### Initial migration (prose)

The initial EF Core migration ("InitialCreate") creates two tables:

1. **Searches** table: columns for `Id` (PK, Guid), `Status` (nvarchar), `CreatedAt`, `CompletedAt`, plus flattened `SearchCriteria` columns (`Criteria_City`, `Criteria_PostalCode`, `Criteria_PropertyType`, `Criteria_MinPrice`, `Criteria_MaxPrice`, `Criteria_MinArea`, `Criteria_MaxArea`, `Criteria_MinRooms`, `Criteria_MaxRooms`).

2. **Listings** table: columns for `Id` (PK, Guid), `SearchId` (FK to Searches), `Title`, `Description`, `Source`, `Area`, `Rooms`, `Floor`, `EnergyRating`, `PricePerM2`, `ReferencePricePerM2`, `OriginalUrl`, `ExternalId`, `ScrapedAt`, plus flattened owned-type columns for `Address_City`, `Address_PostalCode`, `Address_Neighborhood`, `Price_Amount`, `Price_Currency`, `Score_Value`, `ScoreBreakdown_PriceGapScore`, `ScoreBreakdown_AreaScore`, `ScoreBreakdown_FloorScore`, `ScoreBreakdown_EnergyScore`.

Foreign key from `Listings.SearchId` to `Searches.Id` with cascade delete. The two indexes listed above are also created in this migration.

## 5. Security & scalability

### Authentication

**None.** The brief explicitly defines this as a single-user POC with no auth requirement. All endpoints are open. If auth is needed later, the Minimal API pipeline can be extended with JWT bearer or Azure AD B2C without changing application/domain layers.

> Assumption: "No auth" means no middleware, no [Authorize], no identity provider. The API is expected to run on localhost or a private network. If deployed to Azure Container Apps, network isolation (VNet or IP restriction) should be considered but is out of POC scope.

### Secret management

- **Development**: `appsettings.json` for non-sensitive config (connection strings to local SQLite, logging levels). `dotnet user-secrets` for any sensitive value (Azure Storage connection strings, any future API keys).
- **Production / Azure**: Environment variables injected via Azure Container Apps configuration (secrets section). No Key Vault per brief's POC constraint.
- **Hardcoded secrets**: Strictly forbidden. The `.gitignore` must exclude `appsettings.Development.json` if it contains any secret. User Secrets are stored outside the project tree by design.

### Scale points and strategy

| Component | Scale concern | Strategy |
|---|---|---|
| Scraping throughput | Rate-limited by anti-ban delays (3-8s per request). Single-user, so ~2 concurrent source scrapers max. | Azure Storage Queue decouples request from execution. Queue consumer processes one job at a time per source. If needed later: multiple consumers with source-level partitioning. |
| DVF data fetch | Large CSV files (~500 MB for full France). Network-bound. | Local file cache with TTL (refresh quarterly). On first request per postal code, download the relevant department file, parse and index in-memory. Cache to disk as JSON. |
| Database | SQLite handles single-user easily. Table Storage for cloud has no connection pool limit. | For POC, SQLite is sufficient. No connection pooling concern. If multi-user later, migrate to PostgreSQL or Cosmos DB. |
| API requests | Single user, low RPS. | No rate limiting needed. Azure Container Apps scale-to-zero eliminates idle cost. If needed later, add ASP.NET rate limiting middleware. |
| React frontend | Static files, single user. | Served from the same Container App or a static web app. No CDN needed for POC. |

### Idempotence, retry, and resilience

- **Scraping jobs**: The `ExternalId` unique index ensures that re-processing a scraping job does not create duplicate listings (upsert on `Source + ExternalId`).
- **Queue message processing**: Azure Storage Queue provides at-least-once delivery. The handler is idempotent via the deduplication mechanism above.
- **HTTP calls (DVF API)**: Wrapped in a Polly retry policy with exponential backoff (3 retries, base 2s). Circuit breaker after 5 consecutive failures (30s open window).
- **Scraper resilience**: Polly retry with exponential backoff for transient HTTP errors (429, 503). On 403, switch to next User-Agent and retry once. On captcha detection, pause the source for a configurable duration (default: 5 minutes) rather than hammering.
- **Playwright browser lifecycle**: Browser instance is reused across scraping jobs for a given source but recycled after N pages (configurable, default 20) to avoid fingerprint accumulation.

### Legal and ethical constraints

- **robots.txt compliance**: `IAntiBotService.IsAllowedByRobotsTxtAsync()` is called before every scrape URL. If disallowed, the URL is skipped and logged. The robots.txt file is cached per domain with a 24-hour TTL.
- **Terms of Service**: LeBonCoin and SeLoger both prohibit automated scraping in their ToS. This POC is for private/experimental use only. The architecture supports the `FixtureScraper` as a drop-in replacement when live scraping is blocked or ethically questionable.
- **Personal data (GDPR consideration)**: Scraping may capture advertiser names or phone numbers embedded in listing descriptions. The domain model intentionally does NOT have fields for advertiser personal data. If personal data appears in `Description`, it is incidental. For production use, a data-minimization pass (regex redaction of phone numbers, emails) should be added as a post-processing step in the scraper pipeline. This is documented but not implemented in the POC.
- **Data retention**: No automated purge in POC. For production, listings older than a configurable retention period should be purged. The `Search.CreatedAt` timestamp supports this.

### CORS

The API configures CORS to allow requests from the React dev server origin (`http://localhost:5173`) in development. In production, the CORS origin is set via configuration.

## 6. Implementation checklist

```
1.  [ ] Solution scaffold: ImmoScorer.sln with 4 projects (Domain, Application, Infrastructure, Api) + test projects
2.  [ ] SearchCriteria value object + SearchId/ListingId strongly-typed IDs (Domain)
3.  [ ] Address, Price, Score, ScoreBreakdown value objects (Domain)
4.  [ ] PropertyType, SearchStatus, ListingSortOrder enums (Domain)
5.  [ ] Search entity (aggregate root) with status management (Domain)
6.  [ ] Listing entity with all properties and ScrapedListing transient record (Domain)
7.  [ ] ScrapingJob record for queue messages (Domain)
8.  [ ] DvfReferenceData value object (Domain)
9.  [ ] IListingScraper interface (Domain)
10. [ ] IScoringEngine interface (Domain)
11. [ ] IDvfReferenceDataProvider interface (Domain)
12. [ ] IScrapingJobQueue interface (Domain)
13. [ ] IAntiBotService interface (Domain)
14. [ ] Result<T> pattern type (Domain or Application.Common)
15. [ ] RunSearchCommand + RunSearchCommandHandler with FluentValidation (Application)
16. [ ] TriggerScrapingCommand + Handler: enqueues jobs per source (Application)
17. [ ] ProcessScrapingJobCommand + Handler: invokes scraper, enriches with DVF, scores, persists (Application)
18. [ ] GetListingsQuery + Handler with pagination, filtering, sorting (Application)
19. [ ] GetListingDetailQuery + Handler (Application)
20. [ ] GetSavedSearchesQuery + Handler (Application)
21. [ ] ListingDto, ListingDetailDto, ScoreBreakdownDto, SavedSearchDto DTOs (Application)
22. [ ] ListingFilter record for query filtering (Application)
23. [ ] PaginatedList<T> wrapper (Application.Common)
24. [ ] ValidationBehavior<TRequest, TResponse> MediatR pipeline behavior (Application)
25. [ ] LoggingBehavior<TRequest, TResponse> MediatR pipeline behavior (Application)
26. [ ] IImmoScorerDbContext abstraction (Application.Common.Persistence)
27. [ ] ImmoScorerDbContext EF Core implementation with owned types, value conversions, indexes (Infrastructure)
28. [ ] InitialCreate EF Core migration (Infrastructure)
29. [ ] LeBonCoinScraper: Playwright-based IListingScraper implementation (Infrastructure)
30. [ ] SeLogerScraper: Playwright-based IListingScraper implementation (Infrastructure)
31. [ ] FixtureScraper: static test data IListingScraper fallback (Infrastructure)
32. [ ] AntiBotService: UA rotation, random delays, exponential backoff, robots.txt cache, captcha detection (Infrastructure)
33. [ ] WeightedScoringEngine: IScoringEngine implementation with price gap, area, floor, energy sub-scores (Infrastructure)
34. [ ] DvfDataGouvrClient: IDvfReferenceDataProvider with HTTP fetch, CSV parsing, local file cache (Infrastructure)
35. [ ] AzureStorageQueueJobQueue: IScrapingJobQueue using Azure.Storage.Queues SDK (Infrastructure)
36. [ ] InMemoryJobQueue: IScrapingJobQueue for local dev (Infrastructure)
37. [ ] Polly resilience policies: retry + circuit breaker for HTTP calls, scraper backoff (Infrastructure)
38. [ ] ScrapingBackgroundService: hosted service that dequeues and dispatches ProcessScrapingJobCommand (Infrastructure)
39. [ ] DI registration: AddInfrastructure() extension method wiring all implementations (Infrastructure)
40. [ ] POST /searches endpoint -> RunSearchCommand (Api)
41. [ ] POST /searches/{id}/scrape endpoint -> TriggerScrapingCommand (Api)
42. [ ] GET /searches endpoint -> GetSavedSearchesQuery (Api)
43. [ ] GET /listings?searchId=X&minScore=N&sort=... endpoint -> GetListingsQuery (Api)
44. [ ] GET /listings/{id} endpoint -> GetListingDetailQuery (Api)
45. [ ] Global error handler middleware mapping Result failures to HTTP status codes (Api)
46. [ ] CORS configuration for React dev server (Api)
47. [ ] Serilog configuration: console + file sinks, structured logging (Api)
48. [ ] Program.cs: service registration, middleware pipeline, Swagger/OpenAPI (Api)
49. [ ] appsettings.json + appsettings.Development.json with scraping config, delays, DVF cache path (Api)
50. [ ] React project scaffold: Vite + TypeScript + project structure (Frontend)
51. [ ] API client service: typed fetch wrapper for all backend endpoints (Frontend)
52. [ ] Listings list/card view component with score badge (color-coded) (Frontend)
53. [ ] Filter panel: price, area, type, city, minimum score (Frontend)
54. [ ] Sort controls: score, price, price/m2, date (Frontend)
55. [ ] Listing detail view with score breakdown and DVF reference comparison (Frontend)
56. [ ] Search creation form (city, postal code, type, price range, area range, rooms) (Frontend)
57. [ ] Saved searches list with re-run capability (Frontend)
58. [ ] Unit tests: WeightedScoringEngine (under-market, at-market, above-market, edge cases) (Tests)
59. [ ] Unit tests: RunSearchCommandHandler (valid criteria, invalid criteria) (Tests)
60. [ ] Unit tests: ProcessScrapingJobCommandHandler (scraper returns listings, scraper fails, deduplication) (Tests)
61. [ ] Unit tests: GetListingsQueryHandler (pagination, filtering, sorting) (Tests)
62. [ ] Unit tests: DvfDataGouvrClient with fake HTTP responses (Tests)
63. [ ] Unit tests: AntiBotService (delay ranges, backoff escalation, robots.txt parsing) (Tests)
64. [ ] Integration tests: API endpoints via WebApplicationFactory (Tests)
65. [ ] Dockerfile for the .NET backend (multi-stage build) (Deployment)
66. [ ] docker-compose.yml for local full-stack run (backend + frontend) (Deployment)
```
