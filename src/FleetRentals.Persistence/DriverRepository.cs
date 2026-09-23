using Dapper;
using FleetRentals.Application.Features.Drivers;
using FleetRentals.Domain.Drivers;

namespace FleetRentals.Persistence;

public sealed class DriverRepository(NpgsqlConnectionFactory connectionFactory) : IDriverRepository
{
    public async Task AddAsync(Driver driver, CancellationToken cancellationToken)
    {
        await using var connection = await connectionFactory.OpenAsync(cancellationToken);
        const string sql = """
            INSERT INTO drivers (id, name)
            VALUES (@Id, @Name)
            """;

        await connection.ExecuteAsync(new CommandDefinition(
            sql,
            new { Id = driver.Id.Value, Name = driver.Name.Value },
            cancellationToken: cancellationToken));
    }

    public async Task<Driver?> GetByIdAsync(DriverId id, CancellationToken cancellationToken)
    {
        await using var connection = await connectionFactory.OpenAsync(cancellationToken);
        const string sql = """
            SELECT id AS "Id", name AS "Name"
            FROM drivers
            WHERE id = @Id
            """;

        var row = await connection.QuerySingleOrDefaultAsync<DriverRow>(
            new CommandDefinition(sql, new { Id = id.Value }, cancellationToken: cancellationToken));

        return row is null
            ? null
            : Driver.Restore(DriverId.Create(row.Id).Value, DriverName.Create(row.Name).Value);
    }

    private sealed class DriverRow
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = string.Empty;
    }
}
