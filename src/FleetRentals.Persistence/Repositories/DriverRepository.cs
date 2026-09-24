using Dapper;
using FleetRentals.Application.Features.Drivers.Common;
using FleetRentals.Domain.Drivers;

namespace FleetRentals.Persistence.Repositories;

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
            new { Id = driver.Id, Name = driver.Name },
            cancellationToken: cancellationToken));
    }

    public async Task<Driver?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        await using var connection = await connectionFactory.OpenAsync(cancellationToken);
        const string sql = """
            SELECT id AS "Id", name AS "Name"
            FROM drivers
            WHERE id = @Id
            """;

        var row = await connection.QuerySingleOrDefaultAsync<DriverRow>(
            new CommandDefinition(sql, new { Id = id }, cancellationToken: cancellationToken));

        return row is null
            ? null
            : new Driver(row.Id, row.Name);
    }

    private sealed class DriverRow
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = string.Empty;
    }
}
