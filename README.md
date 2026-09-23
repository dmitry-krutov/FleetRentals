# Fleet rentals API

The API is built with .NET 10, PostgreSQL, and Dapper. The current slices implement vehicle and driver registration and lookup; rental operations follow in later slices. HTTP routes use ASP.NET Core controllers. Each Application use case keeps its request and handler in one file under `Features/<feature>`.

## Run locally

Start PostgreSQL:

```bash
docker compose -f compose.infra.yml up -d --wait
```

Apply the current schema to a fresh database, or rerun it to add the new `drivers` table to a database created in the vehicle slice:

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

The response envelope contains `result` on success or `errors` on failure. Invalid input returns 400, a missing vehicle returns 404, and a duplicate license plate returns 409. Plates are trimmed and normalized to uppercase. The vehicle status is `Available` until the rental feature is added.

## Driver endpoints

| Method | Path | Body | Success |
| --- | --- | --- | --- |
| POST | `/drivers` | `{"name":"Alex Driver"}` | 201, driver and `Location` header |
| GET | `/drivers/{id}` | — | 200, driver |

Names are trimmed, must not be blank, and may contain at most 200 characters. Invalid input returns 400 and a missing driver returns 404. Names are not unique: different drivers can have the same name.

## Tests

Run unit tests with `dotnet test tests/FleetRentals.UnitTests`. With the Compose database running, run the PostgreSQL tests with `dotnet test tests/FleetRentals.IntegrationTests`. Set `FLEET_RENTALS_TEST_CONNECTION` to override the integration test connection string. Each integration test uses a temporary schema and removes it afterward.
