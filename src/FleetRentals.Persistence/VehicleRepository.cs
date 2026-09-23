using Dapper;
using FleetRentals.Application.Features.Vehicles;
using FleetRentals.Domain.Vehicles;
using Npgsql;

namespace FleetRentals.Persistence;

public sealed class VehicleRepository(NpgsqlConnectionFactory connectionFactory) : IVehicleRepository
{
    public async Task<bool> AddAsync(Vehicle vehicle, CancellationToken cancellationToken)
    {
        await using var connection = await connectionFactory.OpenAsync(cancellationToken);
        const string sql = """
            INSERT INTO vehicles (id, license_plate, status)
            VALUES (@Id, @LicensePlate, @Status)
            """;

        try
        {
            await connection.ExecuteAsync(new CommandDefinition(
                sql,
                new { Id = vehicle.Id.Value, LicensePlate = vehicle.LicensePlate.Value, Status = vehicle.Status.ToString() },
                cancellationToken: cancellationToken));
            return true;
        }
        catch (PostgresException exception) when (
            exception.SqlState == PostgresErrorCodes.UniqueViolation &&
            exception.ConstraintName == "vehicles_license_plate_unique")
        {
            return false;
        }
    }

    public async Task<Vehicle?> GetByIdAsync(VehicleId id, CancellationToken cancellationToken)
    {
        await using var connection = await connectionFactory.OpenAsync(cancellationToken);
        const string sql = """
            SELECT id AS "Id", license_plate AS "LicensePlate", status AS "Status"
            FROM vehicles
            WHERE id = @Id
            """;
        var row = await connection.QuerySingleOrDefaultAsync<VehicleRow>(
            new CommandDefinition(sql, new { Id = id.Value }, cancellationToken: cancellationToken));

        if (row is null)
            return null;

        return Vehicle.Restore(
            VehicleId.Create(row.Id).Value,
            LicensePlate.Create(row.LicensePlate).Value,
            Enum.Parse<VehicleStatus>(row.Status)).Value;
    }

    private sealed class VehicleRow
    {
        public Guid Id { get; set; }

        public string LicensePlate { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;
    }
}
