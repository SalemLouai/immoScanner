# Brief — Feature to build

> Pipeline input file. Read by the orchestrator as the starting point.

## Feature name
ImmoScorer — Real-estate listing aggregator and scorer

## Business goal
Spot good real-estate deals. The app scrapes listings from LeBonCoin.fr and
SeLoger.com based on user-defined criteria, compares each price against actual
neighborhood/city sales (DVF open data from data.gouv.fr), computes an opportunity
score, and exposes everything in a React web UI that is filterable and sortable by score.

## POC scope (IMPORTANT)
This is a **single-user Proof of Concept, local / lightweight Azure**.
DO NOT propose heavy infrastructure. Use the cheap Azure services:
- Queue: **Azure Storage Queue** (NOT Service Bus)
- Persistence: **Azure Table Storage** (NOT Cosmos DB) + local SQLite accepted
- Secrets / config: **appsettings.json** + User Secrets in dev (NOT Key Vault)
- Compute hosting: **Azure Container Apps** scale-to-zero, or local `dotnet run` —
  no AKS, no cluster.
- No microservices: a modular monolith backend is enough.

## Functional requirements

### 1. Search criteria input
- The user defines: location (city / postal code), property type (apartment / house),
  price range, min/max area, number of rooms.
- Criteria are persisted and reusable (a "saved search").

### 2. Smart scraping (anti-ban)
- Sources: LeBonCoin.fr and SeLoger.com.
- Scraping must be **respectful and stealthy** to avoid bans:
  - Realistic User-Agent rotation.
  - Randomized delays between requests (human-like throttling, e.g. 3-8 s).
  - Respect robots.txt and per-domain rate limiting.
  - Headless browser (Playwright) with a credible browser fingerprint.
  - Exponential backoff on 429 / 403 errors.
  - Captcha detection -> pause the source rather than forcing through.
  - A scraping job queue (Azure Storage Queue) to spread the load over time.
- Extensible architecture: adding a 3rd source must only require a new
  implementation of an `IListingScraper` interface.

### 3. Reference-data enrichment (DVF, data.gouv.fr)
- Fetch the open **DVF (Demandes de Valeurs Foncières)** data from data.gouv.fr for
  the listing's geographic area.
- Compute a reference price per m2 for the neighborhood / municipality (median,
  optionally per property type).
- Local cache of DVF data (it changes rarely) to limit calls.

### 4. Opportunity scoring
- For each listing, compute a score (e.g. 0-100) based on:
  - Gap between the listing's price per m2 and the DVF reference price for the area.
  - Bonus/malus on secondary criteria (area, floor, EPC/DPE if available).
- The score and its breakdown (sub-scores) are stored with the listing.
- Scoring logic must be isolated and testable (an `IScoringEngine` service).

### 5. Backend API
- Endpoints to: run a search, list scored listings, filter/sort, view a listing's detail.
- Result pagination.

### 6. React frontend
- A React (Vite) app showing listings as cards/list.
- **Filters**: price, area, type, city, minimum score.
- **Sorting**: by score (default), price, price per m2, date.
- Visual score indicator (color/badge) and gap-to-reference display.
- Link to the original listing.

## Technical constraints
- Backend: **.NET 9**, Clean Architecture, CQRS via MediatR, EF Core (SQLite in dev,
  Azure Table Storage for lightweight cloud persistence).
- Scraping: **Playwright for .NET**.
- Frontend: **React 18 + Vite + TypeScript**, fetch via the backend API.
- Config via **appsettings.json** + User Secrets (no secret hardcoded in code).
- Logging: structured Serilog (console + file).
- Tests: xUnit + NSubstitute + FluentAssertions on the backend.

## Out of scope
- No user authentication (single-user POC).
- No payment, no push notifications.
- No continuous real-time scraping — on-demand trigger or a simple cron.
- No multi-region deployment nor high availability.

## Legal / ethical considerations (document, do not ignore)
- Scraping must respect each site's Terms of Service and robots.txt.
- Personal data (advertiser contact info): do not store beyond what the POC needs.
  Document this in the architecture (security section).
- Note that SeLoger/LeBonCoin may forbid scraping in their ToS and that this POC is
  for private/experimental use. Also note these sites use aggressive anti-bot
  protection (e.g. DataDome); live scraping may fail and the `IListingScraper`
  abstraction must allow switching to fixtures/mocks.

## Acceptance criteria
- The backend solution compiles with no warning (warnings as errors).
- At least one scraping source works end to end (a mock is acceptable if the site
  blocks, but the `IListingScraper` abstraction must be real).
- The scoring engine is covered by unit tests (cases: under market, at market, above).
- DVF enrichment is isolated behind an interface and tested with a fake dataset.
- The React frontend lists, filters and sorts listings by score.
- The architecture explicitly documents the anti-ban strategy.
