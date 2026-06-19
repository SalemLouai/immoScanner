# Security Review — ImmoScorer (Real-estate aggregator & scorer)
Date: 2026-06-19 · Reviewer: Senior AppSec (.NET / Azure)
Scope: output/src/ (4 .NET projects + React frontend), output/tests/
Reference contract: output/architecture.md section 5 · Brief: single-user POC, no auth, appsettings/User Secrets, lightweight Azure.

## Verdict: ⚠️ Conditional GO

## Executive summary
The codebase is clean for a POC: no hardcoded secrets, EF Core queries are fully parameterized (no raw SQL), input is validated via FluentValidation, the global error handler does not leak stack traces, and React renders all scraped content through auto-escaping JSX (no dangerouslySetInnerHTML). The architecture's documented POC substitutions (no Key Vault, no auth) are honored and acceptable within the declared scope. Two real issues warrant conditions before publishing: a DOM-based XSS vector where untrusted scraped originalUrl values are placed directly into an anchor href without scheme validation (a javascript:/data: URL would execute on click), and a configuration risk where the interactive API explorer (Scalar/OpenAPI) is gated only on ASPNETCORE_ENVIRONMENT=Development — which docker-compose.yml sets explicitly, meaning the container ships with the dev surface exposed. No Critical findings.

## Findings

| ID | OWASP category | Severity | File:line | Description | Remediation |
|----|----------------|----------|-----------|-------------|-------------|
| F1 | A03 Injection (XSS) | 🟠 High | src/frontend/src/pages/ListingDetailPage.tsx:146 | Anchor href={listing.originalUrl} renders an untrusted, scraped URL with no scheme allow-list. A scraped listing whose link is javascript:... or data:text/html,... would execute script when the user clicks the original-listing link. React escapes text nodes but does NOT sanitize href attribute values. | Validate the scheme before rendering: accept only https:/http: (e.g. parse with new URL(url).protocol) and disable the link otherwise. Optionally sanitize on the backend before persisting OriginalUrl. |
| F2 | A05 Security Misconfiguration | 🟠 High | src/ImmoScorer.Api/Program.cs:77-82 + src/docker-compose.yml:11 | OpenAPI doc and the interactive Scalar UI are mapped only if (app.Environment.IsDevelopment()). The provided docker-compose.yml sets ASPNETCORE_ENVIRONMENT=Development, so the shipped container exposes the live API explorer and full schema. If reused on any reachable host, the API surface and Scalar console are public. | For any non-local run set ASPNETCORE_ENVIRONMENT=Production (code gating is already correct). Document the compose file as local-only. |
| F3 | A05 Security Misconfiguration | 🟡 Medium | src/ImmoScorer.Api/appsettings.json:41 | AllowedHosts is "*", accepting any Host header. Combined with no HTTPS redirection (UseHttpsRedirection absent from Program.cs), a non-local deployment is exposed to Host-header spoofing and plaintext transport. | Restrict AllowedHosts to the deployed hostname and enforce HTTPS at ingress for any cloud deployment. Acceptable as-is for localhost POC. |
| F4 | A10 SSRF | 🟡 Medium | src/ImmoScorer.Infrastructure/AntiBot/AntiBotService.cs:123-135 | IsAllowedByRobotsTxtAsync builds {scheme}://{host}/robots.txt from the scrape URL and issues an outbound GetStringAsync with no domain allow-list. Scrape URLs are currently code-constructed (leboncoin/seloger), so exposure is indirect, but no whitelist guards outbound fetches. | Maintain an allow-list of permitted scrape domains and reject others before any outbound request. Low real-world risk in current POC. |
| F5 | A10 SSRF / Injection | 🟡 Medium | src/ImmoScorer.Infrastructure/ReferenceData/DvfDataGouvrClient.cs:119 | The DVF query URL interpolates postalCode directly (code_postal={postalCode}) without Uri.EscapeDataString (only type_local is escaped). On the scrape path postalCode comes from scraped.PostalCode, not the validated 5-digit command field, so a malformed value could distort the outbound request (query-param injection). Base URL is fixed config, so no host pivot. | Escape postalCode via Uri.EscapeDataString and/or re-validate (^\d{5}$) before building the request. |
| F6 | A09 Logging Failures | 🟡 Medium | src/ImmoScorer.Infrastructure/Scraping/LeBonCoinScraper.cs:51, DvfDataGouvrClient.cs:121-123 | Full scrape/DVF URLs (including search-criteria query params) are logged at Information; file sink retains 7 days. Not secrets, but user-behavioral data. Description (possible incidental advertiser PII) is correctly NOT logged. | Keep criteria/PII out of Information-level logs or scrub query strings. Acceptable for single-user POC. |
| F7 | A04 Insecure Design | 🟡 Medium | src/ImmoScorer.Infrastructure/AntiBot/AntiBotService.cs:119,143 | robots.txt check fails open: on malformed URL or fetch error it returns true (allowed). A transient network error silently bypasses the robots.txt compliance the brief mandates (ethical/ToS rather than classic security). | For ToS-sensitive scraping, fail closed (skip) or retry before allowing. Document the fail-open decision explicitly. |
| F8 | A05 Security Misconfiguration | 🔵 Low | output/ (repo root) | No backend/root .gitignore (only src/frontend/.gitignore). appsettings.Development.json is committed (currently no secrets). bin/obj artifacts (incl. Playwright payload) are present under src/.../bin/. Future risk: a dev could add a secret to a committed dev file. | Add a root .gitignore excluding appsettings.*.local.json, *.db, logs/, dvf-cache/, bin/, obj/. |
| F9 | A04 Insecure Design (DoS) | 🔵 Low | src/ImmoScorer.Api/Program.cs (no rate limiting) | No rate limiting; POST /searches/{id}/scrape enqueues background scraping. Fine for single-user localhost (architecture section 5 defers it), but an exposed deployment could be abused to trigger unbounded scraping/outbound traffic. | Add ASP.NET rate limiting before any multi-user/cloud exposure. |
| F10 | A08 Data Integrity | ℹ️ Info | AzureStorageQueueJobQueue.cs:68 | Queue messages deserialize with System.Text.Json into the fixed ScrapingJob type (no polymorphic handling); poison messages are deleted safely. No insecure deserialization. Noted compliant. | None. |

Severities: 🔴 Critical · 🟠 High · 🟡 Medium · 🔵 Low · ℹ️ Info

## OWASP Top 10 (2021) — per-category analysis

- **A01 Broken Access Control** — NA (accepted POC scope). Brief excludes auth (single-user, localhost). Endpoints (/searches, /listings, /searches/{id}/scrape) are open by design; single user means no ownership concept. No sensitive personal data exposed (advertiser PII not modeled). Flag: any multi-user / internet exposure requires [Authorize] + ownership checks. See F2/F9.
- **A02 Cryptographic Failures** — Compliant for POC. No hardcoded keys/connection strings (QueueOptions.ConnectionString empty default, documented for User Secrets/env var; appsettings.json only a local SQLite path). Secret-pattern grep hit only Playwright SDK type docs + the empty option — no leaks. No sensitive data persisted. App-level TLS not enforced (F3) — fine on localhost, enforce via ingress.
- **A03 Injection** — Data access compliant: all EF Core is LINQ/parameterized (GetListingsQueryHandler, ProcessScrapingJobCommandHandler); no FromSqlRaw/ExecuteSqlRaw/concatenated SQL (grep confirmed). Input validated (RunSearchCommandValidator: postal ^\d{5}$, positive ranges, min<=max). XSS sub-class: F1. Query-param injection: F5. React text auto-escaped; no dangerouslySetInnerHTML/innerHTML/eval (grep confirmed).
- **A04 Insecure Design** — Result pattern used consistently (no business exceptions). Idempotence via (Source, ExternalId) dedup correct. Concerns: robots fail-open (F7), no rate limiting (F9). Validation is server-side in the MediatR pipeline (not client-only) — good.
- **A05 Security Misconfiguration** — TreatWarningsAsErrors=true + Nullable on all .csproj. Error handler returns generic message, no stack trace. Issues: dev API explorer via compose env (F2), AllowedHosts "*" + no HTTPS redirect (F3), missing root .gitignore and committed bin/ (F8).
- **A06 Vulnerable & Outdated Components** — Compliant. Current pinned versions from nuget.org (EF Core 9.0.5, MediatR 12.4.1, Playwright 1.52.0, Azure.Storage.Queues 12.22.0, Polly 8.5.2, Serilog 9.0.0). No custom feeds, no known-vulnerable versions. (QA_REPORT cites older numbers for a few packages than the .csproj — cosmetic only.)
- **A07 Identification & Authentication Failures** — NA (no auth in POC). No tokens issued or logged; no credential handling.
- **A08 Software & Data Integrity Failures** — Compliant. Fixed-type JSON deserialization, no polymorphic handling, safe poison-message deletion (F10). Trusted package source. No insecure deserialization.
- **A09 Security Logging & Monitoring Failures** — Mostly compliant. Structured Serilog (console + rolling file, 7-day retention), request logging, exceptions logged in the global handler. No secrets/tokens logged. Minor: full URLs with criteria at Information (F6); scraped Description correctly never logged.
- **A10 SSRF** — Outbound calls: robots.txt (F4) and DVF API (F5). Both use code-constructed or fixed-config hosts (not directly user-supplied), so practical risk is Low, but neither enforces a domain allow-list.

## Azure-specific review

- **Secrets management** — Compliant with POC: Storage connection string via QueueOptions (User Secrets in dev, env var / Container Apps secret in prod), never hardcoded. Default queue provider InMemory; Azure path activates only on config.
- **Managed Identity / least privilege** — Not implemented (POC uses connection-string auth per the brief's lightweight scope). Prod recommendation: DefaultAzureCredential + Managed Identity with Storage Queue Data Contributor scoped to the single queue. No "*" RBAC roles in code. Accepted as POC risk.
- **Container Apps** — docker-compose.yml runs Development (F2). Ensure the Container Apps revision sets ASPNETCORE_ENVIRONMENT=Production and HTTPS-only ingress.

## Contract compliance (architecture.md section 5)

| Requirement (section 5) | Implemented | Evidence |
|------------------|-------------|----------|
| No authentication (single-user POC) | Yes (by design) | No auth middleware in Program.cs; endpoints open |
| Secrets via appsettings + User Secrets, no Key Vault | Yes | QueueOptions.ConnectionString empty default + XML doc; appsettings.json no credentials |
| No hardcoded secrets | Yes | grep across src/ found none (Playwright docs + empty option only) |
| robots.txt checked before scrape | Partial | called in scrapers; but fails open (F7) |
| Advertiser PII not modeled | Yes | Listing/ScrapedListing have no name/phone/email; only Description (incidental) |
| Data-minimization redaction (phone/email) | Not implemented | section 5 states documented but not implemented in POC — consistent |
| CORS restricted to React origin | Yes | Program.cs:44-56 reads Cors:AllowedOrigins, default http://localhost:5173 |
| Polly retry / circuit breaker for HTTP | Declared | Polly packages referenced; resilience config not asserted in tests |
| Dedup via (Source, ExternalId) unique index | Yes | ProcessScrapingJobCommandHandler.cs:63-74 + migration index |

## GO conditions (address before publishing / before any non-local deployment)

1. **F1 (XSS)** — Add a URL-scheme allow-list (http:/https: only) before rendering listing.originalUrl in ListingDetailPage.tsx. This is the only finding that can execute attacker-controlled script in the user's browser; fix before the PR is considered done. (Relay to Dev.)
2. **F2 (dev surface exposed)** — Document docker-compose.yml as local-only, or set ASPNETCORE_ENVIRONMENT=Production for any reachable deployment. Do not ship the Development env to a reachable host.
3. **F3 / F5** — Before any cloud deployment: tighten AllowedHosts, enforce HTTPS, and Uri.EscapeDataString the DVF postalCode.

## Accepted POC risks (tolerated under the declared POC scope)

- No authentication / authorization (A01) — explicitly out of scope per brief.
- No Key Vault; secrets in env var / User Secrets — explicitly mandated by brief.
- No Managed Identity for Storage Queue — lightweight POC; documented upgrade path.
- No rate limiting (F9) — single-user localhost; architecture defers it.
- robots.txt fail-open (F7) and no scrape-domain allow-list (F4) — acceptable while URLs are code-constructed and usage is private/experimental.
- Committed appsettings.Development.json + bin/ artifacts (F8) — no secrets present today; flagged to prevent future leakage.

## Residual risks summary
- DOM XSS via scraped URL until F1 is fixed (High).
- Misconfiguration exposure if the Development compose/env is reused on a reachable host (F2/F3) — High in that scenario, none on localhost.
- Indirect SSRF / query-param tampering on outbound robots.txt and DVF calls (F4/F5) — Low while scrape URLs are not user-supplied.
- Test suite at 49/81 passing (test-harness EF config gap per QA_REPORT, not production code) — no security impact, but lowers regression confidence.

## Legal / ethical considerations
- **ToS / robots.txt**: Architecture section 5 and the brief acknowledge LeBonCoin/SeLoger ToS forbid automated scraping; the IListingScraper abstraction + FixtureScraper (default Scraping:Mode=Fixture) let the POC run without live scraping. robots.txt is checked but fails open (F7) — recommend fail-closed for compliance.
- **Personal data (GDPR)**: Domain model deliberately omits advertiser name/phone/email (data minimization). Description may carry incidental PII; section 5 documents a redaction pass as future work (not implemented). No PII written to logs today.
- **Data retention**: No automated purge in POC (documented as future work); only a single-user local SQLite store.

## Sign-off
**Decision: ⚠️ Conditional GO.** No Critical vulnerabilities. Publishing the PR is acceptable provided the three GO conditions are tracked (F1 fixed or filed as a blocking follow-up before any real use; F2 deployment-env documented). All remaining items are Medium/Low or explicitly accepted POC risks.

## Good practices observed
- Fully parameterized EF Core data access; zero raw SQL.
- Global error handler returns a generic message — no stack-trace/PII leak to clients.
- Server-side input validation (FluentValidation) via the MediatR pipeline.
- No hardcoded secrets; secret config externalized per the brief.
- React renders untrusted content through auto-escaping JSX; no dangerouslySetInnerHTML.
- External links use rel="noopener noreferrer" with target="_blank".
- TreatWarningsAsErrors=true and nullable enabled across projects.
- Safe poison-message handling and non-polymorphic JSON deserialization on the queue.
- Advertiser PII deliberately excluded from the domain model (data minimization by design).
