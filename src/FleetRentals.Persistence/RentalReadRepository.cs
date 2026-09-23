using Dapper;
using FleetRentals.Application.Features.Rentals;
using FleetRentals.Domain.Drivers;
using FleetRentals.Domain.Rentals;
using FleetRentals.Domain.Vehicles;

namespace FleetRentals.Persistence;

public sealed class RentalReadRepository(NpgsqlConnectionFactory connectionFactory) : IRentalReadRepository
{
    public Task<Rental?> GetByIdAsync(RentalId id, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT id AS "Id", vehicle_id AS "VehicleId", driver_id AS "DriverId",
                   started_at_utc AS "StartedAtUtc", finished_at_utc AS "FinishedAtUtc"
            FROM rentals
            WHERE id = @Id
            """;

        return GetAsync(sql, new { Id = id.Value }, cancellationToken);
    }

    public Task<Rental?> GetActiveByVehicleIdAsync(VehicleId vehicleId, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT id AS "Id", vehicle_id AS "VehicleId", driver_id AS "DriverId",
                   started_at_utc AS "StartedAtUtc", finished_at_utc AS "FinishedAtUtc"
            FROM rentals
            WHERE vehicle_id = @VehicleId AND finished_at_utc IS NULL
            """;

        return GetAsync(sql, new { VehicleId = vehicleId.Value }, cancellationToken);
    }

    private async Task<Rental?> GetAsync(string sql, object parameters, CancellationToken cancellationToken)
    {
        await using var connection = await connectionFactory.OpenAsync(cancellationToken);
        var row = await connection.QuerySingleOrDefaultAsync<RentalRow>(new CommandDefinition(
            sql, parameters, cancellationToken: cancellationToken));

        return row is null
            ? null
            : Rental.Restore(
                RentalId.Create(row.Id).Value,
                VehicleId.Create(row.VehicleId).Value,
                DriverId.Create(row.DriverId).Value,
                row.StartedAtUtc,
                row.FinishedAtUtc).Value;
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
