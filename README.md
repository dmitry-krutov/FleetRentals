# Fleet rentals API

The API is built with .NET 10, PostgreSQL, and Dapper. It implements vehicle and driver registration and lookup, plus rental start, finish, and lookup. ASP.NET Core controllers send commands and queries through MediatR, which runs FluentValidation before each handler. Each Application use case keeps its request and handler in one file under `Features/<feature>`.

## Quick start with Docker

With Docker and Docker Compose installed, run this from the repository root:

```bash
docker compose up --build -d --wait
```

Open [Swagger UI](http://localhost:5294/swagger). The Compose stack starts the API and PostgreSQL, and applies `schema.sql` automatically when its database volume is first created. No local .NET SDK or manual SQL command is needed for this path. If port 5294 is occupied, run `FLEET_API_PORT=5295 docker compose up --build -d --wait` and open `http://localhost:5295/swagger`.

To check the full flow in Swagger:

1. `POST /vehicles` with `{"licensePlate":"AB-123"}` and `POST /drivers` with `{"name":"Alex Driver"}`. Copy the IDs from the `result` objects.
2. `POST /rentals` with both IDs. `GET /vehicles/{id}` now shows `Rented`, and `GET /vehicles/{id}/active-rental` shows the rental and driver.
3. Try the same rental again: the API returns 409 and leaves the first rental active.
4. `POST /rentals/{id}/finish`. The vehicle is `Available` again; a new rental using the same vehicle and driver succeeds.

Stop the demo with `docker compose down`. Data stays in the Docker volume. To start with an empty demo database, run `docker compose down -v` before starting again; this deletes only the demo stack's database volume.

## Develop locally with the .NET SDK

Start PostgreSQL:

```bash
docker compose -f compose.infra.yml up -d --wait
```

Apply the schema to the database:

```bash
docker compose -f compose.infra.yml exec -T postgres psql -U fleet_rentals -d fleet_rentals < schema.sql
```

Start the API:

```bash
dotnet run --project src/FleetRentals.Api --launch-profile http
```

Open [Swagger UI](http://localhost:5294/swagger) to call the API. The development connection string in `src/FleetRentals.Api/appsettings.Development.json` points to the infrastructure Compose database. Override it with `ConnectionStrings__FleetRentals` for another PostgreSQL instance. The infrastructure and demo Compose stacks use separate database volumes; only `compose.infra.yml` exposes PostgreSQL on host port 21016.

If the database was created with the previous schema, remove the obsolete vehicle status column before running this version:

```bash
docker compose -f compose.infra.yml exec -T postgres psql -U fleet_rentals -d fleet_rentals < migrations/001_drop_vehicle_status.sql
```

For the full demo stack, use `docker compose exec -T postgres ...` with the same `psql` arguments. `schema.sql` only initializes a new database volume; it does not migrate an existing one.

## Vehicle endpoints

| Method | Path | Body | Success |
| --- | --- | --- | --- |
| POST | `/vehicles` | `{"licensePlate":"AB-123"}` | 201, vehicle and `Location` header |
| GET | `/vehicles/{id}` | — | 200, vehicle |

The response envelope contains `result` on success or `errors` on failure. Invalid input returns 400, a missing vehicle returns 404, and a duplicate license plate returns 409. Plates are trimmed and normalized to uppercase. The response status is `Rented` when the vehicle has an active rental and `Available` otherwise; it is not stored on the vehicle row.

## Driver endpoints

| Method | Path | Body | Success |
| --- | --- | --- | --- |
| POST | `/drivers` | `{"name":"Alex Driver"}` | 201, driver and `Location` header |
| GET | `/drivers/{id}` | — | 200, driver |

Names are trimmed, must not be blank, and may contain at most 200 characters. Invalid input returns 400 and a missing driver returns 404. Names are not unique: different drivers can have the same name.

## Rental endpoints

| Method | Path | Body | Success |
| --- | --- | --- | --- |
| POST | `/rentals` | `{"vehicleId":"<guid>","driverId":"<guid>"}` | 201, active rental and `Location` header |
| GET | `/rentals/{id}` | — | 200, rental |
| POST | `/rentals/{id}/finish` | — | 200, finished rental |
| GET | `/vehicles/{id}/active-rental` | — | 200, current rental for the vehicle |

Starting a rental returns 400 for invalid identifiers, 404 when the vehicle or driver does not exist, and 409 if either already has an active rental. The active rental response identifies the driver; use `GET /drivers/{id}` to retrieve the driver's name. Finishing a rental records the return time with a conditional update. An already finished rental returns 409; a missing rental returns 404. The vehicle and driver can be used in a new rental after the finish succeeds. PostgreSQL partial unique indexes enforce one active rental per vehicle and driver even for concurrent requests.

## Tests

Run unit tests with `dotnet test tests/FleetRentals.UnitTests`. With the `compose.infra.yml` database running, run the PostgreSQL tests with `dotnet test tests/FleetRentals.IntegrationTests`. Set `FLEET_RENTALS_TEST_CONNECTION` to override the integration test connection string. Each integration test uses a temporary schema and removes it afterward.
