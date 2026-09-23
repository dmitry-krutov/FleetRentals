# Fleet rentals API

The API is built with .NET 10, PostgreSQL, and Dapper. It implements vehicle and driver registration and lookup, plus rental start, finish, and lookup. HTTP routes use ASP.NET Core controllers. Each Application use case keeps its request and handler in one file under `Features/<feature>`.

## Run locally

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

Open [Swagger UI](http://localhost:5294/swagger) to call the API. The development connection string in `src/FleetRentals.Api/appsettings.Development.json` points to the Compose database. Override it with `ConnectionStrings__FleetRentals` for another PostgreSQL instance.

For future schema changes to an existing database, add an explicit migration or `ALTER` script; `CREATE TABLE IF NOT EXISTS` does not update existing tables.

## Vehicle endpoints

| Method | Path | Body | Success |
| --- | --- | --- | --- |
| POST | `/vehicles` | `{"licensePlate":"AB-123"}` | 201, vehicle and `Location` header |
| GET | `/vehicles/{id}` | — | 200, vehicle |

The response envelope contains `result` on success or `errors` on failure. Invalid input returns 400, a missing vehicle returns 404, and a duplicate license plate returns 409. Plates are trimmed and normalized to uppercase. A vehicle starts as `Available` and becomes `Rented` when a rental starts.

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

Starting a rental returns 400 for invalid identifiers, 404 when the vehicle or driver does not exist, and 409 if either already has an active rental. The vehicle status becomes `Rented` in the same database transaction. The active rental response identifies the driver; use `GET /drivers/{id}` to retrieve the driver's name. Finishing a rental records the return time and makes the vehicle `Available` in one transaction. An already finished rental returns 409; a missing rental returns 404. The vehicle and driver can be used in a new rental after the finish succeeds.

## Tests

Run unit tests with `dotnet test tests/FleetRentals.UnitTests`. With the Compose database running, run the PostgreSQL tests with `dotnet test tests/FleetRentals.IntegrationTests`. Set `FLEET_RENTALS_TEST_CONNECTION` to override the integration test connection string. Each integration test uses a temporary schema and removes it afterward.
