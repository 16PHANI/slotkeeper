# SlotKeeper

A resource booking API built to solve one specific problem properly: never letting two people book the same time slot, even under concurrent load, without leaning on application-level locks that fall apart under real traffic.

## Why this exists

Most beginner booking demos check for overlapping bookings with a SELECT before the INSERT, then hope nothing else writes to the table in between. That works fine in a demo and falls apart the moment two requests hit the same slot within a few milliseconds of each other, because the check and the insert are not atomic. I wanted to close that gap for real, using the database as the source of truth instead of trusting application code to win a race it cannot see.

## The core idea

Every booking is broken into fixed slot boundaries (30 minutes by default, configurable per resource). Each slot becomes a row in a `BookingSlots` table with a unique index on `(ResourceId, SlotStartUtc)`. Booking a resource means inserting the parent `Booking` row plus one `BookingSlot` row per slot it covers, in the same SaveChanges call. If any of those slots is already taken, the unique index rejects the insert and the whole write fails together. There is no SELECT-then-check window for two requests to slip through, because the constraint is enforced at write time by the database itself, not read time by application code.

Cancelling a booking deletes its slot rows, which frees them up immediately for the next request.

A background service sweeps the waitlist every 30 seconds and tries to promote pending entries through the exact same booking method a normal request uses, so there is only one code path in the whole system that actually creates a booking. That was a deliberate choice: duplicating the conflict logic between "book now" and "promote from waitlist" is exactly the kind of thing that drifts out of sync over time.

## Stack

- ASP.NET Core 8 Web API (C#), split into Domain, Infrastructure, and Api projects
- Entity Framework Core 8 with SQL Server
- JWT authentication with role-based authorization (Member / Admin)
- A hand-written stored procedure for the utilization report, called through EF Core's raw SQL support
- React 18 with TypeScript (Vite) on the frontend
- xUnit for unit tests; xUnit plus WebApplicationFactory plus SQLite for integration tests
- Docker Compose for local SQL Server, GitHub Actions for CI

## Project layout

```
slotkeeper/
  src/
    SlotKeeper.Domain/         entities, enums, exceptions, pure booking logic
    SlotKeeper.Infrastructure/ EF Core DbContext, stored procedure SQL
    SlotKeeper.Api/            controllers, services, auth, middleware
  tests/
    SlotKeeper.UnitTests/         slot math and booking rules, no database involved
    SlotKeeper.IntegrationTests/  full HTTP round trips against an in-memory SQLite database
  client/                     React and TypeScript frontend
  database/                   reference copy of the stored procedure SQL
  docker-compose.yml          API plus SQL Server for local development
```

## Running it locally

You need the .NET 8 SDK, Node 18 or newer, and Docker.

Start SQL Server and the API together:

```
docker compose up --build
```

The API listens on `http://localhost:8080`, with Swagger at `http://localhost:8080/swagger` when running in Development mode.

In a second terminal, start the frontend:

```
cd client
npm install
npm run dev
```

The frontend runs on `http://localhost:5173` and expects the API at `http://localhost:8080` (see `client/.env.example`).

If you would rather run the API without Docker, point `ConnectionStrings:SlotKeeperDb` in `src/SlotKeeper.Api/appsettings.Development.json` at any SQL Server instance you have, then:

```
dotnet run --project src/SlotKeeper.Api
```

## Running the tests

```
dotnet test
```

This runs both test projects. The unit tests check the slot-alignment math and the daily booking limit in isolation, with no database involved. The integration tests spin up the actual API through `WebApplicationFactory` against an in-memory SQLite database and check things like: a second user trying to book an already-taken slot gets a 409, cancelling a booking frees the slot for someone else, a member cannot create a resource, and a member cannot book past their daily limit.

## A note on how this was verified

I do not currently have a machine with .NET and open NuGet access in front of me while writing this, so I was not able to run `dotnet build` or `dotnet test` end to end before pushing. What I did do: wrote every project by hand rather than copying a template, ran the React/TypeScript half through a real `npm install` and `npm run build` (it compiles clean), and ran every `.cs` file through the Roslyn compiler directly against the .NET 8 reference assemblies to catch real syntax and naming errors, ignoring the expected "package not found" noise for things like EF Core and xUnit that only resolve through a normal restore. That pass caught one genuine bug: a namespace named `SlotKeeper.Domain.Booking` collided with the `Booking` entity class and made `Booking` ambiguous inside its own namespace. Fixed by renaming the namespace to `SlotKeeper.Domain.BookingLogic`. I also wired up the GitHub Actions workflow in `.github/workflows/ci.yml` to run `dotnet restore`, `dotnet build`, and both test projects on every push, so the first push to GitHub gives a real, independently verifiable green or red build. Run `dotnet build` and `dotnet test` yourself before relying on this anywhere important.

## A note on the SQL Server specific pieces

The utilization report endpoint (`GET /api/reports/utilization`) calls a real stored procedure through `EXEC`, which only exists on SQL Server. It is intentionally not covered by the integration tests, which run against SQLite. If you want to exercise it, run the full stack through `docker compose up` and hit the endpoint with an admin token.

The schema is created at startup with `EnsureCreated()` rather than versioned EF Core migrations. For a project this size that is a reasonable tradeoff, but the first thing I would change before running this anywhere beyond a laptop is switching to `dotnet ef migrations add InitialCreate` and a proper migration history.

## What I would add next

- OAuth or SSO instead of the current email-and-password JWT flow, since that is what most enterprise environments actually run
- Rate limiting on the auth endpoints
- A push notification (email or webhook) when a waitlist entry gets promoted, instead of requiring the user to check back
- Pagination on the bookings and resources endpoints once there is enough data for it to matter
- Versioned EF Core migrations instead of `EnsureCreated()`

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

## License

MIT, see `LICENSE`.
