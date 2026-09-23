# Implementation notes

## Design

- `FleetRentals.Api` contains HTTP controllers, request models, response envelopes, Swagger configuration, and exception handling. Controllers delegate work to Application handlers.
- `FleetRentals.Application` has one file per use case, with the request and handler together. Repository and transactional session interfaces live next to the features that use them.
- `FleetRentals.Domain` owns vehicle, driver, and rental entities, identifiers, value objects, statuses, and state transitions. It has no HTTP, SQL, or system clock access.
- `FleetRentals.Persistence` implements the interfaces with PostgreSQL, Npgsql, and Dapper. The API composes the layers through their `DependencyInjection` methods.

The HTTP layer uses `Controllers/` in place of the example `Endpoints/` folder from the brief. Each controller action maps a route and request to one use case and returns its result through the `EndpointResult` response pattern. Use case handlers remain independent of HTTP.

## Business choices

- Vehicle license plates are trimmed, uppercased, and unique without regard to case. Driver names are trimmed but are not unique.
- A newly registered vehicle is `Available`. Starting a rental makes it `Rented`; finishing makes it `Available` again. The rental's `Active` or `Finished` status is derived from whether `finished_at_utc` is null.
- A vehicle or driver can have only one active rental. PostgreSQL partial unique indexes enforce both rules even when requests arrive concurrently.
- Starting and finishing a rental update the rental and vehicle in one database transaction. Rows are locked in a consistent vehicle-then-driver order. A failed operation rolls back, so it cannot leave a partial change.
- The Application receives the current time through `TimeProvider`. Domain methods accept the time as an argument and store UTC values.
- A missing vehicle, driver, or rental returns 404. Invalid input returns 400; attempts to violate an active-rental or duplicate-plate rule return 409. `GET /vehicles/{id}/active-rental` returns 404 when the vehicle exists but has no active rental.

## Verification and scope

The unit tests cover domain transitions and use case decisions. PostgreSQL integration tests cover persistence, all four requested scenarios, and concurrent start/finish requests. The full demo stack starts with `docker compose up --build -d`; Swagger is at `http://localhost:5294/swagger`.

`schema.sql` initializes a fresh database. Schema migrations for existing installations, authentication, billing, tracking, and a user interface are outside this exercise.
