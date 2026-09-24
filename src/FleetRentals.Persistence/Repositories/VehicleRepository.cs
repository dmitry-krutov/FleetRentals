using Dapper;
using FleetRentals.Application.Features.Vehicles.Common;
using FleetRentals.Domain.Vehicles;
using Npgsql;

namespace FleetRentals.Persistence.Repositories;

public sealed class VehicleRepository(NpgsqlConnectionFactory connectionFactory) : IVehicleRepository
{
    public async Task<bool> AddAsync(Vehicle vehicle, CancellationToken cancellationToken)
    {
        await using var connection = await connectionFactory.OpenAsync(cancellationToken);
        const string sql = """
            INSERT INTO vehicles (id, license_plate)
            VALUES (@Id, @LicensePlate)
            """;

        try
        {
            await connection.ExecuteAsync(new CommandDefinition(
                sql,
                new { Id = vehicle.Id, LicensePlate = vehicle.LicensePlate },
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

    public async Task<Vehicle?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        await using var connection = await connectionFactory.OpenAsync(cancellationToken);
        const string sql = """
            SELECT v.id AS "Id", v.license_plate AS "LicensePlate",
                   EXISTS (
                       SELECT 1 FROM rentals r
                       WHERE r.vehicle_id = v.id AND r.finished_at_utc IS NULL
                   ) AS "IsRented"
            FROM vehicles v
            WHERE v.id = @Id
            """;
        var row = await connection.QuerySingleOrDefaultAsync<VehicleRow>(
            new CommandDefinition(sql, new { Id = id }, cancellationToken: cancellationToken));

        if (row is null)
            return null;

        return new Vehicle(
            row.Id,
            row.LicensePlate,
            row.IsRented ? VehicleStatus.Rented : VehicleStatus.Available);
    }

    private sealed class VehicleRow
    {
        public Guid Id { get; set; }

        public string LicensePlate { get; set; } = string.Empty;

        public bool IsRented { get; set; }
    }
}
