---
title: Threat Model — /outages/by-region and adjacent endpoints
target: OutageTracker
author: Najib Zarrari
date: 2026-08-17
scope: Public unauthenticated read/write endpoints exposed by OutageTracker.Api. Deployed to Azure App Service Linux B1 at outage-tracker-<suffix>.azurewebsites.net.
methodology: STRIDE
---

# Threat Model — /outages/by-region and adjacent endpoints

## Trust boundaries and assumptions

- **Boundary 1:** public internet → Azure App Service ingress. No auth, no allow-list.
- **Boundary 2:** Api process → SQLite file at `../outages.db`. Same-container, filesystem-mediated. Shared with the Web app when running locally; App Service deployment carries its own instance of the file per deploy slot.
- **Assumption:** field operators are trusted **once inside the network**. This app has no authentication or authorization surface today — every caller is anonymous and equivalent.
- **Assumption:** Application Insights connection string is populated (M3); Kusto retention is default (30 days).

## Endpoint inventory as of `main@c57160e4`

| Method | Path | Auth | Body / query | Backing method |
|---|---|---|---|---|
| GET | `/outages?severity=<int?>` | none | severity query param | `OutageService.GetBySeverityAsync` |
| GET | `/outages/{id:guid}` | none | route param | `OutageService.GetByIdAsync` |
| POST | `/outages` | none | full `Outage` JSON body | `OutageService.CreateAsync` |
| GET | `/outages/by-region?region=<string>` | none | region query param | `OutageService.FindByRegionRawAsync` |

Every endpoint is anonymous. Every endpoint reads or writes the same SQLite database.

---

## S — Spoofing

| # | Threat | Asset | Status | Mitigation |
|---|---|---|---|---|
| S1 | Any anonymous caller can pose as any field operator when posting outages (`POST /outages`), since there is no auth. Attacker can inject arbitrary outages and pollute the incident record. | Outage records; incident-response accuracy | **Exposed — HIGH** | Add Microsoft Entra ID JWT bearer auth on `POST /outages`; enforce `[Authorize]` (or minimal-API `.RequireAuthorization()`) on the endpoint. `OutageTracker.Api/Program.cs`. |
| S2 | Client-supplied `Id` on `POST /outages` could try to overwrite a specific existing outage row. | Existing outage records | **Mitigated** — `OutageService.CreateAsync` reassigns `outage.Id = Guid.NewGuid()` server-side (`OutageTracker.Core/OutageService.cs`, `CreateAsync`). |

## T — Tampering

| # | Threat | Asset | Status | Mitigation |
|---|---|---|---|---|
| T1 | SQL injection via the `region` query param on `/outages/by-region`. | Database, credentials leakage via subquery patterns | **Mitigated — VERIFIED IN M4** — Parameterized query using `command.Parameters.Add(...)` in `OutageService.FindByRegionRawAsync`. CodeQL rule `cs/sql-injection` monitors this file on every PR (M4 setup). |
| T2 | Client-supplied `ReportedAt`, `RestoredAt`, `EstimatedRestorationAt` on `POST /outages` can be back-dated or future-dated to mask a real outage timeline. | Outage timeline accuracy | **Exposed — MEDIUM** | Reject `ReportedAt` more than 1 hour in the future or older than 90 days. Only accept `RestoredAt >= ReportedAt`. Validate in `CreateAsync` or a `record` DTO with validation attributes. |
| T3 | `Region` string on `POST /outages` and `/outages/by-region` accepts arbitrarily long / unicode-heavy strings which can pollute reports and waste storage. | Storage; report readability | **Exposed — LOW** | Add length (`MaxLength = 32`) and character-class validation (`[A-Za-z0-9-]+`). |
| T4 | Duplicate route registration for `/outages/by-region` in `Program.cs` (dead code after `app.Run()`). | Availability of the endpoint | **Mitigated — VERIFIED IN M5** — Deduped in PR #7. Route registered exactly once before `app.Run()`. |

## R — Repudiation

| # | Threat | Asset | Status | Mitigation |
|---|---|---|---|---|
| R1 | No structured request logging on any endpoint. If an attacker floods `POST /outages` with fake outages, there is no per-request audit trail beyond App Insights' default request telemetry (which doesn't capture body content). | Audit trail; incident forensics | **Exposed — HIGH** | Inject `ILogger<OutageService>` and log a structured event on every `CreateAsync` (`incident_id`, `region`, `severity`, `caller_ip`). Log on `FindByRegionRawAsync` with just `region` + `caller_ip` (do not log outage descriptions). |
| R2 | No caller identity available even if logging were added — every request is anonymous. Without S1 fixed, R1 logs are non-attributable. | Attribution | **Exposed — depends on S1** | Chain: fix S1 first, then include `sub` claim from Entra JWT in R1 log fields. |

## I — Information disclosure

| # | Threat | Asset | Status | Mitigation |
|---|---|---|---|---|
| I1 | Full outage `Description` is returned unfiltered by all read endpoints. Descriptions may contain customer premises addresses, worker names, or other operational details a public reader shouldn't see. | Customer/personnel privacy | **Exposed — MEDIUM** | Return a redacted DTO (`record OutageSummary`) from public endpoints that omits `Description`; keep a separate authenticated endpoint (behind S1's auth) for full description access. |
| I2 | Verbose ASP.NET Core exception page in Development mode returns stack traces including file paths and package versions. | Info leak that aids further attacks | **Mitigated for Web** — `app.UseExceptionHandler("/Error", …)` in `OutageTracker.Web/Program.cs` when not Development. **Not verified for Api** — Api project has no equivalent. | Add `app.UseExceptionHandler(…)` in `OutageTracker.Api/Program.cs`, especially since Api runs on the same Azure App Service in Production. |
| I3 | Guid `Id` is returned in every response. Predictable enough that once an attacker sees one Id, they can iterate through others via `/outages/{id:guid}`. | Enumeration of specific incidents | **Exposed — LOW** | Guids are random enough that brute-force enumeration is infeasible. Accepting the risk. |

## D — Denial of service

| # | Threat | Asset | Status | Mitigation |
|---|---|---|---|---|
| **D1** | **No rate limiting on any endpoint. A single client can hammer `/outages/by-region` (or any endpoint) and pin the B1 App Service Plan CPU, starving legitimate operators.** | **Availability** | **Mitigated — PR [#10](https://github.com/nzarrari/outage-tracker-validation-20260815164221/pull/10)** | **`/outages/by-region` uses ASP.NET Core rate limiting (`Microsoft.AspNetCore.RateLimiting`): fixed window, 10 requests/minute per client IP, 429 with `Retry-After: 60`. `OutageTracker.Api/Program.cs`.** |
| D2 | Unbounded result size — `GetBySeverityAsync` and `FindByRegionRawAsync` return every matching row. A caller can query `?severity=null` (all rows) and force the app to serialize an unbounded list. | Availability; memory | **Exposed — MEDIUM** | Add `.Take(500)` (or a page-size query param) to both methods in `OutageService.cs`. |
| D3 | `POST /outages` accepts an unbounded request body. Attackers can send a 100MB `Description` string and consume memory. | Memory; storage | **Exposed — MEDIUM** | Enforce `RequestSizeLimitAttribute(1024 * 32)` or per-endpoint `[RequestFormLimits]`. Add `MaxLength = 4096` on `Description` in `Outage.cs` too. |
| D4 | SQLite is a single-writer database. Concurrent writes serialize; a burst of `POST /outages` after S1 is fixed would still throttle. | Write throughput | **Accepted risk** for workshop scale. In production, move to Azure SQL. |

## E — Elevation of privilege

| # | Threat | Asset | Status | Mitigation |
|---|---|---|---|---|
| E1 | No privilege model exists today — every caller is equivalent to every other. **N/A** at the current state. Once S1 (auth) is added, any admin endpoints introduced later must be gated. | Future concern | **N/A now** | Design principle for future work: keep read anonymous, require auth for write, require role for admin. Document in `.github/copilot-instructions.md`. |

---

## Prioritized fix list

The following order minimizes residual risk per unit of implementation work:

1. **D1 — rate limiting on `/outages/by-region`** (this is what M6 Part B implements). Low effort, blocks the most likely attack (script kiddie).
2. **S1 — auth on `POST /outages`**. Requires Entra ID app registration; use OIDC pattern from M3 as reference. Blocks the most damaging attack (data pollution).
3. **T2 + T3 — input validation**. Once auth is in, tighten body validation to reject nonsense.
4. **R1 — structured logging**. Only useful after S1; drop it in as part of the auth PR.
5. **I1 — DTO for public read endpoints**. Lower priority; do after auth is deployed.
6. **D2 + D3 — result size + request size limits**. Bundle with T3.

## Follow-up: workshop convergence

- **M4** already mitigated **T1**.
- **M5** already mitigated **T4**.
- **M6 Part B** mitigates **D1** (rate limiting).
- **Everything else (S1, T2, T3, R1, I1, D2, D3)** is out of scope for this workshop and lives here as a residual-risk register for follow-up work.
