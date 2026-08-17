# SlotKeeper

![CI](https://img.shields.io/badge/CI-GitHub_Actions-2088FF?logo=githubactions&logoColor=white)
![.NET](https://img.shields.io/badge/.NET-8-512BD4?logo=dotnet&logoColor=white)
![Frontend](https://img.shields.io/badge/React_18-TypeScript-3178C6?logo=typescript&logoColor=white)
![Tests](https://img.shields.io/badge/tests-18%20passing-brightgreen)
![License](https://img.shields.io/badge/license-MIT-blue)

A resource booking API built around one rule: two people can never end up holding the same time slot, no matter how close together their requests land.

## Why this exists

Most beginner booking demos check for overlapping bookings with a SELECT before the INSERT, then hope nothing else writes to the table in between. That works fine in a demo and falls apart the moment two requests hit the same slot within a few milliseconds of each other, because the check and the insert are not atomic. SlotKeeper closes that gap by making the database the source of truth instead of trusting application code to win a race it cannot see.

## Live links

| What | Where |
|---|---|
| Source | `github.com/<your-username>/slotkeeper` — swap in your handle once it's pushed |
| Live demo | none by design — the API needs a real SQL Server behind it, which isn't a fit for a free static host |
| CI | `.github/workflows/ci.yml`, runs `dotnet restore`, `build`, and both test projects on every push |

## Features

| Area | What it does |
|---|---|
| Booking | Books a resource for one or more fixed slots in a single atomic write |
| Conflict safety | A unique database index rejects double-bookings at write time, not read time |
| Waitlist | Background sweep every 30 seconds promotes waitlisted users through the same booking path a normal request uses |
| Roles | JWT auth with Member and Admin roles enforced server-side on every mutating endpoint |
| Daily limits | Per-resource cap on bookings per user per day, checked before insert |
| Reporting | Admin-only utilization report backed by a hand-written stored procedure |
| Audit trail | Every booking created or cancelled is logged with actor, action, and payload |
| Testing | 18 automated tests across unit and full HTTP integration suites |

## How the conflict-safety actually works

Every booking is broken into fixed slot boundaries (30 minutes by default, configurable per resource). Each slot becomes a row in `BookingSlots` with a unique index on `(ResourceId, SlotStartUtc)`. Creating a booking inserts the parent `Booking` row plus one `BookingSlot` row per slot it covers, all in the same `SaveChanges` call. If any slot is already taken, the unique index rejects the whole write.

| Guarantee | How it's enforced |
|---|---|
| No double-booking | Unique index on `(ResourceId, SlotStartUtc)` in `BookingSlots`; a violation is caught and turned into a 409 |
| No lost cancellations | `Booking.RowVersion` is a concurrency token, regenerated on every write, checked by EF Core's optimistic concurrency |
| One code path for every booking | The waitlist sweep calls `BookingService.CreateBookingAsync` directly instead of duplicating the conflict logic |
| Cross-database conflict detection | `BookingService` catches SQL Server error codes 2627/2601 in production and the SQLite "UNIQUE constraint failed" message in tests, so the same 409 behavior is verified without a real SQL Server |

That last point mattered more than it sounds like it should: the integration tests run against SQLite (see below), so the conflict-detection code has to recognize a constraint violation from either engine, not just the one it ships against.

Cancelling a booking deletes its slot rows, freeing them immediately for the next request.

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

## Getting started

Requires the .NET 8 SDK, Node 18+, and Docker.

Start SQL Server and the API together:

```
docker compose up --build
```

The API listens on `http://localhost:8080`, with Swagger at `http://localhost:8080/swagger` in Development mode.

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

Runs both test projects: 18 tests, 18 passing. Unit tests check slot-alignment math and the daily booking limit in isolation, no database involved. Integration tests spin up the real API through `WebApplicationFactory` against an in-memory SQLite database and cover things like a second user getting a 409 on an already-taken slot, cancelling a booking freeing it for someone else, a member being blocked from creating a resource, and a member hitting their daily limit.

| Gate | Command |
|---|---|
| Unit + integration tests | `dotnet test` |
| API build | `dotnet build` |
| Frontend build | `cd client && npm run build` |
| CI | `.github/workflows/ci.yml`, runs all of the above on every push |

### Troubleshooting: "no matching runtime found" on `dotnet test`

`Directory.Build.props` sets `<RollForward>LatestMajor</RollForward>` so `dotnet run` and `dotnet build` work even if you only have a newer .NET SDK installed than the project targets. That setting does not cover `dotnet test`: VSTest launches tests through `testhost.exe`, which ignores the project's roll-forward policy and needs the exact matching runtime installed side by side with the SDK. If `dotnet test` fails with a runtime-not-found error while `dotnet build` succeeds, install the .NET 8 ASP.NET Core runtime (not just the SDK) and re-run.

## Deployment

There's no hosted live demo, on purpose. The utilization report depends on a real stored procedure, which means a real SQL Server behind the API, not a fit for a free static host. To deploy for real:

1. Build the container image from the included `Dockerfile`.
2. Point it at a managed SQL Server (Azure SQL, RDS, or self-hosted) via `ConnectionStrings__SlotKeeperDb`.
3. Push the image to any container host that can reach that database — Azure App Service, AWS ECS, or a plain VM running Docker all work.
4. Swap `EnsureCreated()` for versioned EF Core migrations before pointing this at anything with real data (see below).

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
- **Concurrency token is a random GUID, not a database-generated `rowversion`.** `Booking.RowVersion` is regenerated with `Guid.NewGuid().ToByteArray()` on every write and marked `IsConcurrencyToken()` in `OnModelCreating`. That keeps the model portable across SQL Server and SQLite (a true SQL Server `rowversion` column isn't something SQLite has an equivalent for), at the cost of relying on the application to remember to rotate it instead of the database doing it automatically.
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