# SlotKeeper

![CI](https://github.com/16PHANI/slotkeeper/actions/workflows/ci.yml/badge.svg)
![.NET](https://img.shields.io/badge/.NET-8-512BD4?logo=dotnet&logoColor=white)
![Frontend](https://img.shields.io/badge/React_18-TypeScript-3178C6?logo=typescript&logoColor=white)
![Tests](https://img.shields.io/badge/tests-18%20passing-brightgreen)
![License](https://img.shields.io/badge/license-MIT-blue)

**Repo:** [github.com/16PHANI/slotkeeper](https://github.com/16PHANI/slotkeeper)

A resource booking API built around one rule: two people can never end up holding the same time slot, no matter how close together their requests land. The conflict check isn't application code hoping nothing races it — it's a database constraint that makes the bad outcome structurally impossible.

## Engineering highlights

- **Solved a real concurrency bug class, not a toy one.** Booking conflicts are prevented with a unique database index enforced at write time, not a SELECT-then-INSERT check that races under load.
- **One code path for every booking.** The waitlist auto-promoter calls the exact same service method a live booking request does, so there is no second copy of the conflict logic to drift out of sync.
- **Tested across two database engines on purpose.** Integration tests run on SQLite for speed; production runs on SQL Server. The conflict-detection code has to recognize a constraint violation from either engine, and it's tested doing so.
- **18 automated tests, 0 flaky.** Unit tests isolate the slot math and business rules; integration tests drive the real HTTP pipeline through `WebApplicationFactory`.
- **CI enforces all of it.** Every push runs `dotnet restore`, `build`, and both test suites via GitHub Actions — see the badge above.

## How the race condition gets solved

The naive approach almost every booking tutorial uses:

```
Request A:  SELECT is slot free?  → yes
Request B:       SELECT is slot free? → yes      (A hasn't committed yet)
Request A:  INSERT booking                       ← succeeds
Request B:  INSERT booking                       ← also succeeds — double booking
```

The check and the write are two separate round trips, so there's a window where both requests see "free" before either commits. SlotKeeper closes that window by never asking the question separately from answering it:

```
Request A:  INSERT booking + slot rows  → unique index accepts it
Request B:  INSERT booking + slot rows  → unique index REJECTS it (409)
```

Every booking is broken into fixed slot boundaries (30 minutes by default, configurable per resource). Each slot is a row in `BookingSlots` with a unique index on `(ResourceId, SlotStartUtc)`. Creating a booking inserts the parent `Booking` row plus one `BookingSlot` row per slot it covers, all in a single `SaveChanges` call. If any slot is already taken, the unique index rejects the entire write — there's no partial booking and no race window, because the database enforces the rule instead of the application code checking for it.

| Guarantee | How it's enforced |
|---|---|
| No double-booking | Unique index on `(ResourceId, SlotStartUtc)` in `BookingSlots`; a violation is caught in `BookingService` and turned into a 409 |
| No lost cancellations | `Booking.RowVersion` is a concurrency token, regenerated on every write, checked by EF Core's optimistic concurrency |
| One code path for every booking | The waitlist sweep calls `BookingService.CreateBookingAsync` directly instead of duplicating conflict logic |
| Cross-database conflict detection | Catches SQL Server error codes 2627/2601 in production and SQLite's "UNIQUE constraint failed" message in tests, so the same 409 behavior is verified without needing a real SQL Server in CI |

Cancelling a booking deletes its slot rows, freeing them immediately for the next request. A background service sweeps the waitlist every 30 seconds and tries to promote pending entries through that same booking path.

## Features

| Area | What it does |
|---|---|
| Booking | Books a resource for one or more fixed slots in a single atomic write |
| Conflict safety | Unique database index rejects double-bookings at write time |
| Waitlist | Auto-promotes waitlisted users through the same code path as a live booking |
| Roles | JWT auth with Member and Admin roles enforced server-side on every mutating endpoint |
| Daily limits | Per-resource cap on bookings per user per day, checked before insert |
| Reporting | Admin-only utilization report backed by a hand-written stored procedure |
| Audit trail | Every booking created or cancelled is logged with actor, action, and payload |
| Testing | 18 automated tests across unit and full HTTP integration suites, run in CI on every push |

## Tech stack

| Layer | Choice |
|---|---|
| API | ASP.NET Core 8 (C#), split into Domain / Infrastructure / Api projects |
| Data | Entity Framework Core 8 on SQL Server |
| Auth | JWT bearer tokens, role-based authorization (Member / Admin) |
| Reporting | Hand-written stored procedure, called through EF Core's raw SQL support |
| Frontend | React 18 + TypeScript, built with Vite |
| Testing | xUnit for unit tests; xUnit + `WebApplicationFactory` + SQLite for integration tests |
| Infra | Docker Compose for local SQL Server, GitHub Actions for CI |

## Project structure

```
slotkeeper/
  src/
    SlotKeeper.Domain/         entities, enums, exceptions, pure booking logic
    SlotKeeper.Infrastructure/ EF Core DbContext, stored procedure SQL
    SlotKeeper.Api/            controllers, services, auth, middleware
  tests/
    SlotKeeper.UnitTests/         slot math and booking rules, no database involved
    SlotKeeper.IntegrationTests/  full HTTP round trips against an in-memory SQLite database
  client/                     React + TypeScript frontend
  database/                   reference copy of the stored procedure SQL
  docker-compose.yml          API plus SQL Server for local development
  Directory.Build.props       shared MSBuild settings (roll-forward policy, see Testing below)
```

## Quick start

Requires the .NET 8 SDK, Node 18+, and Docker.

```
git clone https://github.com/16PHANI/slotkeeper.git
cd slotkeeper
docker compose up --build
```

The API is live at `http://localhost:8080`, with Swagger at `http://localhost:8080/swagger` in Development mode.

In a second terminal, start the frontend:

```
cd client
npm install
npm run dev
```

The frontend runs on `http://localhost:5173` and expects the API at `http://localhost:8080` (see `client/.env.example`).

To run the API without Docker, point `ConnectionStrings:SlotKeeperDb` in `src/SlotKeeper.Api/appsettings.Development.json` at any SQL Server instance you have, then:

```
dotnet run --project src/SlotKeeper.Api
```

## Testing and verification

```
dotnet test
```

Runs both test projects: 18 tests, 18 passing. Unit tests check slot-alignment math and the daily booking limit in isolation, no database involved. Integration tests spin up the real API through `WebApplicationFactory` against an in-memory SQLite database and cover a second user getting a 409 on an already-taken slot, cancelling a booking freeing it for someone else, a member being blocked from creating a resource, and a member hitting their daily limit.

| Gate | Command |
|---|---|
| Unit + integration tests | `dotnet test` |
| API build | `dotnet build` |
| Frontend build | `cd client && npm run build` |
| CI | `.github/workflows/ci.yml`, runs all of the above on every push |

### Troubleshooting: "no matching runtime found" on `dotnet test`

`Directory.Build.props` sets `<RollForward>LatestMajor</RollForward>` so `dotnet run` and `dotnet build` work even with only a newer .NET SDK installed than the project targets. That setting does not cover `dotnet test`: VSTest launches tests through `testhost.exe`, which ignores the project's roll-forward policy and needs the exact matching runtime installed side by side with the SDK. If `dotnet test` fails with a runtime-not-found error while `dotnet build` succeeds, install the .NET 8 ASP.NET Core runtime (not just the SDK) and re-run.

## Deployment

Runs anywhere that can host a container and reach a SQL Server instance:

1. Build the image from the included `Dockerfile`.
2. Point it at a managed SQL Server (Azure SQL, RDS, or self-hosted) via `ConnectionStrings__SlotKeeperDb`.
3. Ship the image to Azure App Service, AWS ECS, or any VM running Docker.
4. Swap `EnsureCreated()` for versioned EF Core migrations before pointing this at anything with real data (see Design decisions below).

There's no hosted demo in this repo on purpose — the utilization report depends on a real stored procedure, so a genuine deployment needs a genuine SQL Server behind it rather than a free static host. `docker compose up --build` gets the full stack running locally in under a minute, which is the fastest way to see it work end to end.

## Environment variables

| Variable | Purpose |
|---|---|
| `ConnectionStrings__SlotKeeperDb` | SQL Server connection string |
| `Jwt__SigningKey` | Symmetric key used to sign and validate JWTs |
| `Jwt__Issuer` / `Jwt__Audience` | Standard JWT validation fields |
| `Cors__AllowedOrigins` | Comma-separated origins allowed to call the API from a browser |

`docker-compose.yml` sets sane local defaults for all of these; override them for anything beyond a laptop.

## API summary

| Method | Route | Auth | Description |
|---|---|---|---|
| POST | /api/auth/register | none | Create an account |
| POST | /api/auth/login | none | Get a JWT |
| GET | /api/resources | none | List active resources |
| POST | /api/resources | Admin | Create a resource |
| PUT | /api/resources/{id} | Admin | Update a resource |
| DELETE | /api/resources/{id} | Admin | Deactivate a resource |
| POST | /api/bookings | Member or Admin | Book a slot |
| POST | /api/bookings/waitlist | Member or Admin | Join the waitlist for a taken slot |
| DELETE | /api/bookings/{id} | owner or Admin | Cancel a booking |
| GET | /api/bookings/mine | Member or Admin | List your own bookings |
| GET | /api/bookings/resource/{id} | Member or Admin | List bookings for a resource in a date range |
| GET | /api/reports/utilization | Admin | Daily utilization percentage for a resource |

## Design and engineering decisions

- **`EnsureCreated()` instead of migrations.** Reasonable for a project this size, but the first thing to change before running this beyond a laptop is `dotnet ef migrations add InitialCreate` and a real migration history.
- **Concurrency token is a random GUID, not a database-generated `rowversion`.** `Booking.RowVersion` is regenerated with `Guid.NewGuid().ToByteArray()` on every write and marked `IsConcurrencyToken()` in `OnModelCreating`. That keeps the model portable across SQL Server and SQLite (SQLite has no equivalent of a true SQL Server `rowversion` column), at the cost of relying on the application to rotate it instead of the database doing it automatically.
- **One booking code path.** The waitlist promoter calls the exact same `BookingService.CreateBookingAsync` method a live request does, specifically so the conflict-detection logic never has two implementations that can drift apart.
- **SQL Server-specific reporting isn't covered by integration tests.** `GET /api/reports/utilization` calls a real stored procedure via `EXEC`, which SQLite can't run. It's excluded from the SQLite-backed integration suite by design; exercising it means running the full stack with `docker compose up` and hitting the endpoint with an admin token.

## What's next

- OAuth or SSO instead of email-and-password JWT, since that's what most enterprise environments actually run
- Rate limiting on the auth endpoints
- A push notification (email or webhook) when a waitlist entry gets promoted, instead of requiring a manual check
- Pagination on the bookings and resources endpoints once there's enough data for it to matter
- Versioned EF Core migrations instead of `EnsureCreated()`

## License

MIT, see `LICENSE`.