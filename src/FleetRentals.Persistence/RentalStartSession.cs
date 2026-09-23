using System.Data;
using Dapper;
using FleetRentals.Application.Features.Rentals;
using FleetRentals.Domain.Drivers;
using FleetRentals.Domain.Rentals;
using FleetRentals.Domain.Vehicles;
using Npgsql;

namespace FleetRentals.Persistence;

public sealed class RentalStartSessionFactory(NpgsqlConnectionFactory connectionFactory) : IRentalStartSessionFactory
{
    public async Task<IRentalStartSession> OpenAsync(CancellationToken cancellationToken)
    {
        var connection = await connectionFactory.OpenAsync(cancellationToken);
        try
        {
            var transaction = await connection.BeginTransactionAsync(
                IsolationLevel.ReadCommitted, cancellationToken);
            return new RentalStartSession(connection, transaction);
        }
        catch
        {
            await connection.DisposeAsync();
            throw;
        }
    }
}

internal sealed class RentalStartSession(NpgsqlConnection connection, NpgsqlTransaction transaction) : IRentalStartSession
{
    private bool _committed;

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

    public async Task<bool> UpdateVehicleStatusAsync(Vehicle vehicle, CancellationToken cancellationToken)
    {
        const string sql = """
            UPDATE vehicles
            SET status = @Status
            WHERE id = @Id AND status = 'Available'
            """;

        var affected = await connection.ExecuteAsync(new CommandDefinition(
            sql,
            new { Id = vehicle.Id.Value, Status = vehicle.Status.ToString() },
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
}
