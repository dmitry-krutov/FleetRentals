using Dapper;
using FleetRentals.Application.Features.Rentals;
using FleetRentals.Application.Features.Rentals.Common;
using FleetRentals.Domain.Rentals;
using Npgsql;

namespace FleetRentals.Persistence.Repositories;

public sealed class RentalRepository(NpgsqlConnectionFactory connectionFactory) : IRentalRepository
{
    public Task<Rental?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT id AS "Id", vehicle_id AS "VehicleId", driver_id AS "DriverId",
                   started_at_utc AS "StartedAtUtc", finished_at_utc AS "FinishedAtUtc"
            FROM rentals
            WHERE id = @Id
            """;

        return GetAsync(sql, new { Id = id }, cancellationToken);
    }

    public Task<Rental?> GetActiveByVehicleIdAsync(Guid vehicleId, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT id AS "Id", vehicle_id AS "VehicleId", driver_id AS "DriverId",
                   started_at_utc AS "StartedAtUtc", finished_at_utc AS "FinishedAtUtc"
            FROM rentals
            WHERE vehicle_id = @VehicleId AND finished_at_utc IS NULL
            """;

        return GetAsync(sql, new { VehicleId = vehicleId }, cancellationToken);
    }

    public async Task<RentalInsertOutcome> TryAddAsync(Rental rental, CancellationToken cancellationToken)
    {
        await using var connection = await connectionFactory.OpenAsync(cancellationToken);
        const string sql = """
            INSERT INTO rentals (id, vehicle_id, driver_id, started_at_utc)
            VALUES (@Id, @VehicleId, @DriverId, @StartedAtUtc)
            """;

        try
        {
            await connection.ExecuteAsync(new CommandDefinition(
                sql,
                new { rental.Id, rental.VehicleId, rental.DriverId, rental.StartedAtUtc },
                cancellationToken: cancellationToken));
            return RentalInsertOutcome.Inserted;
        }
        catch (PostgresException exception) when (
            exception.SqlState == PostgresErrorCodes.ForeignKeyViolation &&
            exception.ConstraintName == "rentals_vehicle_fk")
        {
            return RentalInsertOutcome.VehicleNotFound;
        }
        catch (PostgresException exception) when (
            exception.SqlState == PostgresErrorCodes.ForeignKeyViolation &&
            exception.ConstraintName == "rentals_driver_fk")
        {
            return RentalInsertOutcome.DriverNotFound;
        }
        catch (PostgresException exception) when (
            exception.SqlState == PostgresErrorCodes.UniqueViolation &&
            exception.ConstraintName == "rentals_active_vehicle_unique")
        {
            return RentalInsertOutcome.VehicleAlreadyRented;
        }
        catch (PostgresException exception) when (
            exception.SqlState == PostgresErrorCodes.UniqueViolation &&
            exception.ConstraintName == "rentals_active_driver_unique")
        {
            return RentalInsertOutcome.DriverAlreadyRented;
        }
    }

    public async Task<bool> TryFinishAsync(
        Guid id, DateTimeOffset finishedAtUtc, CancellationToken cancellationToken)
    {
        await using var connection = await connectionFactory.OpenAsync(cancellationToken);
        const string sql = """
            UPDATE rentals
            SET finished_at_utc = @FinishedAtUtc
            WHERE id = @Id AND finished_at_utc IS NULL AND started_at_utc <= @FinishedAtUtc
            """;

        var affected = await connection.ExecuteAsync(new CommandDefinition(
            sql,
            new { Id = id, FinishedAtUtc = finishedAtUtc },
            cancellationToken: cancellationToken));
        return affected == 1;
    }

    private async Task<Rental?> GetAsync(string sql, object parameters, CancellationToken cancellationToken)
    {
        await using var connection = await connectionFactory.OpenAsync(cancellationToken);
        var row = await connection.QuerySingleOrDefaultAsync<RentalRow>(new CommandDefinition(
            sql, parameters, cancellationToken: cancellationToken));

        return row is null
            ? null
            : new Rental(row.Id, row.VehicleId, row.DriverId, row.StartedAtUtc, row.FinishedAtUtc);
    }

    private sealed class RentalRow
    {
        public Guid Id { get; set; }

        public Guid VehicleId { get; set; }

        public Guid DriverId { get; set; }

        public DateTimeOffset StartedAtUtc { get; set; }

        public DateTimeOffset? FinishedAtUtc { get; set; }
    }
}
