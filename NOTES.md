# Implementation notes

## Design

- `FleetRentals.Api` contains HTTP controllers, request models, a response envelope, Swagger configuration, and exception handling. Controllers manually map API requests to Application commands and queries.
- `FleetRentals.Application` has one file per use case, with the request, FluentValidation validator, and MediatR handler together. A MediatR pipeline behavior validates requests before their handlers execute. Repository interfaces live next to the features that use them.
- `FleetRentals.Domain` contains flat vehicle, driver, and rental models with primitive identifiers and properties. It has no HTTP, SQL, validation, or system clock access.
- `FleetRentals.Persistence` implements the interfaces with PostgreSQL, Npgsql, and Dapper. The API composes the layers through their `DependencyInjection` methods.

## Business choices

- Vehicle license plates are trimmed, uppercased, and unique without regard to case. Driver names are trimmed but are not unique.
- A vehicle's response status is derived from whether it has an active rental. It is not stored separately. The rental's `Active` or `Finished` status is derived from whether `finished_at_utc` is null.
- A vehicle or driver can have only one active rental. PostgreSQL partial unique indexes enforce both rules even when requests arrive concurrently.
- Starting a rental inserts one row; finishing it conditionally updates that row. Each statement is atomic. Partial unique indexes prevent concurrent starts from creating two active rentals for the same vehicle or driver, and the conditional update lets only one concurrent finish succeed.
- The Application receives the current time through `TimeProvider`.
- A missing vehicle, driver, or rental returns 404. Invalid input returns 400; attempts to violate an active-rental or duplicate-plate rule return 409. `GET /vehicles/{id}/active-rental` returns 404 when the vehicle exists but has no active rental.

## Verification and scope

The unit tests cover flat domain data, validation, and use case decisions. PostgreSQL integration tests cover persistence and concurrent start/finish requests. The full demo stack starts with `docker compose up --build -d`; Swagger is at `http://localhost:5294/swagger`.

`schema.sql` initializes a fresh database. `migrations/001_drop_vehicle_status.sql` updates databases created with the earlier schema. Authentication, billing, tracking, and a user interface are outside this exercise.
