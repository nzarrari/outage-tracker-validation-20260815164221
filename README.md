# OutageTracker

Power-utility field-ops app. Regions report outages; severity 1 is the worst; `RestoredAt=null` means still out.

Stack: **.NET 8** minimal API + Blazor Server + EF Core (SQLite) + xUnit.

## Structure

| Project | Purpose |
|---|---|
| `OutageTracker.Api` | Minimal-API endpoints for outage read/write |
| `OutageTracker.Core` | Domain model, `OutageDbContext`, `OutageService` |
| `OutageTracker.Web` | Blazor Server UI at `/outages` |
| `OutageTracker.Tests` | xUnit tests (InMemory EF Core provider) |

## Getting started

```powershell
dotnet restore
dotnet build --configuration Release
dotnet test  --configuration Release --no-build

# Run the API (port varies)
dotnet run --project OutageTracker.Api
# Run the Blazor Web app (port varies)
dotnet run --project OutageTracker.Web
```

Both apps use the same `outages.db` at the solution root.

## Security

- **Threat model** — see [`docs/threat-model.md`](docs/threat-model.md). Grounded STRIDE analysis of the current public endpoints.
- **CodeQL** — enabled with extended query suite; SQL-injection detection validated in workshop Module 4.
- **Coverage gate** — 60/80% floor reported on every PR; see `.github/workflows/build-test-deploy.yml`.
- **Copilot code review rules** — see `.github/copilot-instructions.md` (review guidelines section).

## Workshop context

This repo was built as the validation vehicle for the **Secure Agentic SDLC Workshop** (see `C:\Users\nzarrari\OneDrive - Microsoft\Documents\Microsoft Scout\Secure-Agentic-SDLC-Workshop\` on the workshop author's machine). Each PR corresponds to a workshop module milestone:

- **PR #1** — Severity filter (M2: Copilot chat + inline)
- **PR #3** — `EstimatedRestorationAt` end-to-end (M2: Copilot Coding Agent)
- **PR #4** — Build-test-deploy workflow with OIDC (M3)
- **PR #6** — `/outages/by-region` endpoint + SQL-injection fix (M4)
- **PR #7** — Quality gates + coverage + review rules (M5)
- **This PR + follow-ups** — Threat model + mitigations (M6)
