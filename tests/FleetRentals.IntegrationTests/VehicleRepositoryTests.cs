using FleetRentals.Application.Features.Vehicles;
using FleetRentals.Domain.Vehicles;
using FleetRentals.Persistence;
using Npgsql;
using Xunit;

namespace FleetRentals.IntegrationTests;

public sealed class VehicleRepositoryTests
{
    [Fact]
    public async Task Registered_vehicle_can_be_loaded_from_postgresql()
    {
        await using var database = await IsolatedDatabase.CreateAsync();
        var repository = new VehicleRepository(new NpgsqlConnectionFactory(database.DataSource));
        var register = new RegisterVehicleCommandHandler(repository);

        var created = await register.HandleAsync(new RegisterVehicleCommand(" ab-123 "), default);
        var loaded = await new GetVehicleQueryHandler(repository)
            .HandleAsync(new GetVehicleQuery(created.Value.Id), default);

        Assert.True(created.IsSuccess);
        Assert.True(loaded.IsSuccess);
        Assert.Equal(created.Value.Id, loaded.Value.Id);
        Assert.Equal("AB-123", loaded.Value.LicensePlate);
        Assert.Equal("Available", loaded.Value.Status);
    }

    [Fact]
    public async Task Database_refuses_same_plate_with_different_case()
    {
        await using var database = await IsolatedDatabase.CreateAsync();
        var repository = new VehicleRepository(new NpgsqlConnectionFactory(database.DataSource));
        var register = new RegisterVehicleCommandHandler(repository);

        var first = await register.HandleAsync(new RegisterVehicleCommand("ab-123"), default);
        var duplicate = await register.HandleAsync(new RegisterVehicleCommand("AB-123"), default);

        Assert.True(first.IsSuccess);
        Assert.True(duplicate.IsFailure);
        Assert.Equal("vehicle.license_plate.exists", duplicate.Error.Code);
        Assert.NotNull(await repository.GetByIdAsync(VehicleId.Create(first.Value.Id).Value, default));
    }

    private sealed class IsolatedDatabase : IAsyncDisposable
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
            var schema = "vehicle_test_" + Guid.NewGuid().ToString("N");

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
}
