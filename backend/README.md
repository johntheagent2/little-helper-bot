# Backend

The .NET backend that `bot/` forwards every inbound message to (see the root `CLAUDE.md`). Foundation is scaffolded: `LittleHelper.sln` with three hexagonal projects under `src/`.

**Persistence: SQLite.** **Hosting: Google Cloud Compute Engine (`e2-micro`, Always Free tier), a US region only (`us-west1`/`us-central1`/`us-east1`) — not Railway, not Cloud Run.** `bot/` hosts on this same VM too (Railway is dropped entirely). See `docs/analysis-phase.md`'s "Revision — 2026-09-22: backend persistence + hosting decided" for the reasoning (Cloud Run's ephemeral filesystem doesn't suit SQLite; a Compute Engine VM's persistent disk does, for $0/mo). Open gap: the VM needs its own HTTPS termination (e.g. Caddy/nginx + Let's Encrypt) in front of both processes — Railway/Cloud Run gave that for free, a bare VM doesn't.

## Layout

```
backend/
  LittleHelper.sln
  openapi/
    messages.yaml          hand-written contract for POST /messages + GET /health
                            (contract-first, per docs/analysis-phase.md decision #6)
  src/
    LittleHelper.Domain/          no I/O, no framework deps
      Users/                      User entity, PlatformType, IUserRepository port
      Messaging/                  IncomingMessage, ICommandHandler port, MessageHandler
                                   (resolve-or-create user, dispatch to whichever
                                   ICommandHandler claims the leading command token)
      CycleTracking/               CycleLog, CyclePrediction, CycleMutationResult,
                                   ICycleLogRepository port, CyclePredictor (pure
                                   calculation), CycleTrackingService,
                                   CycleTrackingCommandHandler (owns all cycle commands)
    LittleHelper.Infrastructure/  EF Core + SQLite
      Persistence/                LittleHelperDbContext, UserRepository, CycleLogRepository,
                                   Migrations/
      DependencyInjection/        AddInfrastructure(IServiceCollection, connectionString)
    LittleHelper.Api/             ASP.NET Core (Kestrel), controller-based
      Program.cs                  wires DI, runs migrations on startup, maps controllers
      Controllers/                HealthController (GET /health), MessagesController
                                   (POST /messages) — request/response DTOs live at the
                                   bottom of MessagesController.cs
```

## What's built vs. what's next

- **Built**: solution + project wiring, EF Core/SQLite persistence (auto-migrates on startup, creates its own `data/` dir), `POST /messages` (resolves-or-creates the calling user, returns a placeholder reply), `GET /health`. Verified end to end against the exact request/response shapes `bot/`'s `HttpBackendGateway.ts` sends. Cycle tracking is wired up behind seven commands (dates as `yyyy-MM-dd` throughout) — `/logcycle [start [end]]` logs a `CycleLog` and returns a prediction (start date defaults to today, server-UTC, if omitted), `/cycle` returns the current prediction without logging, `/cyclehistory [count]` lists the last N logged cycles most-recent-first (1–12, default 3, clamped rather than rejected if a larger count is requested or more than exist), `/editcycle <wrong> <correct>` fixes a mistyped start date in place, `/deletecycle <start>` removes a specific entry, `/undo` removes whichever entry was *logged* most recently (by `LoggedAt`, so undoing after an edit removes the edited entry even if its `StartDate` isn't the newest), `/help` lists all of them. `CyclePredictor` derives next period date, fertile window, and average cycle length from up to the 6 most recent logs (implausible gaps outside 15–45 days are excluded from the average; a 28-day default is used until there's at least one gap to average). Note: SQLite's EF Core provider can't translate `ORDER BY` on `DateTimeOffset` (`LoggedAt`), so the two lookups that need most-recently-logged ordering sort client-side after fetching — fine at this app's scale, would need revisiting under real load. No reminders/notifications wired to any of this yet.
- **Not built yet**: reminders (scheduling/recurrence/due-detection) and its outbound HTTP client to call bot's `POST /internal/notify` for push delivery, and API-key/auth enforcement on `/messages` (bot sends `Authorization: Bearer <BACKEND_API_KEY>` only if that env var is set — not yet validated here).
- **Not designed yet**: TLS termination on the shared Compute Engine VM (see `docs/analysis-phase.md`'s hosting revision — Caddy/nginx + Let's Encrypt needed in front of both this and `bot/`'s Kestrel/Fastify ports).

## Local dev

```
dotnet build backend/LittleHelper.sln
dotnet run --project backend/src/LittleHelper.Api
```

SQLite file lands at `src/LittleHelper.Api/data/littlehelper.db` (git-ignored, auto-created). Override the path via `ConnectionStrings:Default` (`appsettings.json`) or the `ConnectionStrings__Default` env var.

New migration after a model change:
```
dotnet ef migrations add <Name> --project backend/src/LittleHelper.Infrastructure --startup-project backend/src/LittleHelper.Api --output-dir Persistence/Migrations
```
