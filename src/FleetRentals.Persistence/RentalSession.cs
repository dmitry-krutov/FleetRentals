using System.Data;
using Dapper;
using FleetRentals.Application.Features.Rentals;
using FleetRentals.Domain.Drivers;
using FleetRentals.Domain.Rentals;
using FleetRentals.Domain.Vehicles;
using Npgsql;

namespace FleetRentals.Persistence;

public sealed class RentalSessionFactory(NpgsqlConnectionFactory connectionFactory) : IRentalSessionFactory
{
    public async Task<IRentalSession> OpenAsync(CancellationToken cancellationToken)
    {
        var connection = await connectionFactory.OpenAsync(cancellationToken);
        try
        {
            var transaction = await connection.BeginTransactionAsync(
                IsolationLevel.ReadCommitted, cancellationToken);
            return new RentalSession(connection, transaction);
        }
        catch
        {
            await connection.DisposeAsync();
            throw;
        }
    }
}

internal sealed class RentalSession(NpgsqlConnection connection, NpgsqlTransaction transaction) : IRentalSession
{
    private bool _committed;

    public Task<Rental?> GetRentalAsync(RentalId id, CancellationToken cancellationToken) =>
        LoadRentalAsync(id, forUpdate: false, cancellationToken);

    public Task<Rental?> GetRentalForUpdateAsync(RentalId id, CancellationToken cancellationToken) =>
        LoadRentalAsync(id, forUpdate: true, cancellationToken);

    public async Task<Vehicle?> GetVehicleForUpdateAsync(VehicleId id, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT id AS "Id", license_plate AS "LicensePlate", status AS "Status"
            FROM vehicles
            WHERE id = @Id
            FOR UPDATE
            """;

        var row = await connection.QuerySingleOrDefaultAsync<VehicleRow>(new CommandDefinition(
            sql, new { Id = id.Value }, transaction, cancellationToken: cancellationToken));

        return row is null
            ? null
            : Vehicle.Restore(
                VehicleId.Create(row.Id).Value,
                LicensePlate.Create(row.LicensePlate).Value,
                Enum.Parse<VehicleStatus>(row.Status)).Value;
    }

    public async Task<Driver?> GetDriverForUpdateAsync(DriverId id, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT id AS "Id", name AS "Name"
            FROM drivers
            WHERE id = @Id
            FOR UPDATE
            """;

        var row = await connection.QuerySingleOrDefaultAsync<DriverRow>(new CommandDefinition(
            sql, new { Id = id.Value }, transaction, cancellationToken: cancellationToken));

        return row is null
            ? null
            : Driver.Restore(DriverId.Create(row.Id).Value, DriverName.Create(row.Name).Value);
    }

    public Task<bool> HasActiveRentalForDriverAsync(DriverId id, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT EXISTS (
                SELECT 1 FROM rentals
                WHERE driver_id = @DriverId AND finished_at_utc IS NULL
            )
            """;

        return connection.ExecuteScalarAsync<bool>(new CommandDefinition(
            sql, new { DriverId = id.Value }, transaction, cancellationToken: cancellationToken));
    }

    public async Task<RentalInsertOutcome> TryInsertAsync(Rental rental, CancellationToken cancellationToken)
    {
        const string sql = """
            INSERT INTO rentals (id, vehicle_id, driver_id, started_at_utc, finished_at_utc)
            VALUES (@Id, @VehicleId, @DriverId, @StartedAtUtc, NULL)
            """;

        try
        {
            await connection.ExecuteAsync(new CommandDefinition(
                sql,
                new
                {
                    Id = rental.Id.Value,
                    VehicleId = rental.VehicleId.Value,
                    DriverId = rental.DriverId.Value,
                    rental.StartedAtUtc,
                },
                transaction,
                cancellationToken: cancellationToken));
            return RentalInsertOutcome.Inserted;
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

    public async Task<bool> UpdateRentalFinishAsync(Rental rental, CancellationToken cancellationToken)
    {
        const string sql = """
            UPDATE rentals
            SET finished_at_utc = @FinishedAtUtc
            WHERE id = @Id AND finished_at_utc IS NULL
            """;

        var affected = await connection.ExecuteAsync(new CommandDefinition(
            sql,
            new { Id = rental.Id.Value, rental.FinishedAtUtc },
            transaction,
            cancellationToken: cancellationToken));
        return affected == 1;
    }

    public async Task<bool> UpdateVehicleStatusAsync(
        Vehicle vehicle, VehicleStatus expectedStatus, CancellationToken cancellationToken)
    {
        const string sql = """
            UPDATE vehicles
            SET status = @Status
            WHERE id = @Id AND status = @ExpectedStatus
            """;

        var affected = await connection.ExecuteAsync(new CommandDefinition(
            sql,
            new
            {
                Id = vehicle.Id.Value,
                Status = vehicle.Status.ToString(),
                ExpectedStatus = expectedStatus.ToString(),
            },
            transaction,
            cancellationToken: cancellationToken));
        return affected == 1;
    }

    public async Task CommitAsync(CancellationToken cancellationToken)
    {
        await transaction.CommitAsync(cancellationToken);
        _committed = true;
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            if (!_committed)
                await transaction.RollbackAsync();
        }
        finally
        {
            try
            {
                await transaction.DisposeAsync();
            }
            finally
            {
                await connection.DisposeAsync();
            }
        }
    }

    private async Task<Rental?> LoadRentalAsync(
        RentalId id, bool forUpdate, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT id AS "Id", vehicle_id AS "VehicleId", driver_id AS "DriverId",
                   started_at_utc AS "StartedAtUtc", finished_at_utc AS "FinishedAtUtc"
            FROM rentals
            WHERE id = @Id
            """;

        var commandText = forUpdate ? sql + "\nFOR UPDATE" : sql;
        var row = await connection.QuerySingleOrDefaultAsync<RentalRow>(new CommandDefinition(
            commandText, new { Id = id.Value }, transaction, cancellationToken: cancellationToken));

        return row is null
            ? null
            : Rental.Restore(
                RentalId.Create(row.Id).Value,
                VehicleId.Create(row.VehicleId).Value,
                DriverId.Create(row.DriverId).Value,
                row.StartedAtUtc,
                row.FinishedAtUtc).Value;
    }

    private sealed class VehicleRow
    {
        public Guid Id { get; set; }

        public string LicensePlate { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;
    }

    private sealed class DriverRow
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = string.Empty;
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
