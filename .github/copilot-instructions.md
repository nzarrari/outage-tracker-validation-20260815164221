# Copilot instructions for OutageTracker

## Domain
OutageTracker is a power-utility field-ops app. Regions report outages;
severity 1 is the worst; RestoredAt=null means still out.

## Code style
- Nullable reference types are enabled. Do not disable them.
- Prefer `async`/`await` for I/O; suffix async methods with `Async`.
- Use records for read-only DTOs; classes for EF entities.
- Blazor pages live under `OutageTracker.Web/Components/Pages/`.
- API endpoints live in `OutageTracker.Api/Program.cs` and use minimal API syntax.

## Testing
- Every new public method on `OutageService` needs an xUnit test.
- Use `Microsoft.EntityFrameworkCore.InMemory` with a per-test unique database name.
- Prefer explicit `Assert.Equal` over generic `Assert.True`.

## Security
- Never use raw SQL via `FromSqlRaw` with string concatenation. Use parameterized queries
  or LINQ expressions.
- No hardcoded connection strings, secrets, or PII.

## Review guidelines (for Copilot code review)
- Reject PRs with public methods lacking XML docs.
- Reject `Console.WriteLine`; use `ILogger<T>` instead.
- Reject `.Result` / `.Wait()` on tasks in async paths — deadlock risk.
- Reject `catch (Exception ex)` with no logging or rethrow.
- Reject any new endpoint without a corresponding xUnit test.
- Reject any DbContext or HttpClient created with `new` outside a factory or DI.
- Prefer `IReadOnlyList<T>` over `List<T>` on return types when the caller shouldn't mutate.
- Any new NuGet package must have a comment in the PR body explaining why it was added.
