using Npgsql;

namespace FleetRentals.IntegrationTests;

internal sealed class IsolatedDatabase : IAsyncDisposable
{
    private readonly string _baseConnectionString;
    private readonly string _schema;

    private IsolatedDatabase(string baseConnectionString, string schema, NpgsqlDataSource dataSource)
    {
        _baseConnectionString = baseConnectionString;
        _schema = schema;
        DataSource = dataSource;
    }

    public NpgsqlDataSource DataSource { get; }

    public static async Task<IsolatedDatabase> CreateAsync()
    {
        var baseConnectionString = Environment.GetEnvironmentVariable("FLEET_RENTALS_TEST_CONNECTION")
            ?? "Host=localhost;Port=21016;Database=fleet_rentals;Username=fleet_rentals;Password=fleet_rentals";
        var schema = "fleet_test_" + Guid.NewGuid().ToString("N");

        await using (var connection = new NpgsqlConnection(baseConnectionString))
        {
            await connection.OpenAsync();
            await using var createSchema = new NpgsqlCommand($"CREATE SCHEMA \"{schema}\"", connection);
            await createSchema.ExecuteNonQueryAsync();
        }

        var testConnectionString = new NpgsqlConnectionStringBuilder(baseConnectionString)
        {
            SearchPath = schema,
        }.ConnectionString;
        var dataSource = NpgsqlDataSource.Create(testConnectionString);

        try
        {
            await using var connection = await dataSource.OpenConnectionAsync();
            var schemaSql = await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory, "schema.sql"));
            await using var applySchema = new NpgsqlCommand(schemaSql, connection);
            await applySchema.ExecuteNonQueryAsync();
            return new IsolatedDatabase(baseConnectionString, schema, dataSource);
        }
        catch
        {
            await dataSource.DisposeAsync();
            await DropSchemaAsync(baseConnectionString, schema);
            throw;
        }
    }

    public async ValueTask DisposeAsync()
    {
        await DataSource.DisposeAsync();
        await DropSchemaAsync(_baseConnectionString, _schema);
    }

    private static async Task DropSchemaAsync(string connectionString, string schema)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var dropSchema = new NpgsqlCommand($"DROP SCHEMA \"{schema}\" CASCADE", connection);
        await dropSchema.ExecuteNonQueryAsync();
    }
}
