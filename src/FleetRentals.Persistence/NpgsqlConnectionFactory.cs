using Npgsql;

namespace FleetRentals.Persistence;

public sealed class NpgsqlConnectionFactory(NpgsqlDataSource dataSource)
{
    public Task<NpgsqlConnection> OpenAsync(CancellationToken cancellationToken) =>
        dataSource.OpenConnectionAsync(cancellationToken).AsTask();
}