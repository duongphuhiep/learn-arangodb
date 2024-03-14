using arango_model_csharp;
using ArangoDBNetStandard;
using ArangoDBNetStandard.CollectionApi.Models;
using ArangoDBNetStandard.DatabaseApi;
using ArangoDBNetStandard.DatabaseApi.Models;
using ArangoDBNetStandard.Transport.Http;
using Testcontainers.ArangoDb;

namespace experiment;

/// <summary>
/// Spin up a database with some data
/// </summary>
public class DbSetupFixture : IAsyncLifetime
{
    private string? _dbUrl;
    private readonly ArangoDbContainer _container = new ArangoDbBuilder()
        .Build();
    public string? DbUrl => _dbUrl;

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        _dbUrl = _container.GetTransportAddress();
        await CreateDatabase();
        await AddCollection();
        await CreateSomeWallets();
    }

    public async Task DisposeAsync()
    {
        await _container.DisposeAsync();
    }

    private async Task CreateSomeWallets()
    {
        using var adb = CreateAdbClient();
        await adb.Document.PostDocumentAsync("wallet", new Wallet { _key = "1", balance = 100 });
        await adb.Document.PostDocumentAsync("wallet", new Wallet { _key = "2", balance = 100 });
        await adb.Document.PostDocumentAsync("wallet", new Wallet { _key = "3", balance = 100 });
    }

    private async Task AddCollection()
    {
        using var adb = CreateAdbClient();

        // Create a collection in the database
        await adb.Collection.PostCollectionAsync(
            new PostCollectionBody
            {
                Name = "wallet",
                // A whole heap of other options exist to define key options, 
                // sharding options, etc
            });

        await adb.Collection.PostCollectionAsync(
            new PostCollectionBody
            {
                Name = "trans",
                Type = CollectionType.Edge
                // A whole heap of other options exist to define key options, 
                // sharding options, etc
            });
    }

    private async Task CreateDatabase()
    {
        // You must use the _system database to create databases
        using (var systemDbTransport = HttpApiTransport.UsingBasicAuth(
        new Uri(_dbUrl),
        "_system",
        "root",
        "root"))
        {
            var systemDb = new DatabaseApiClient(systemDbTransport);

            // Create a new database with one user.
            await systemDb.PostDatabaseAsync(
                new PostDatabaseBody
                {
                    Name = "lemon",
                    Users = new List<DatabaseUser>
                    {
                new DatabaseUser
                {
                    Username = "adminmb",
                    Passwd = ""
                }
                    }
                });
        }
    }

    public ArangoDBClient CreateAdbClient() => CreateAdbClient(_dbUrl);

    private static ArangoDBClient CreateAdbClient(string? dbUrl)
    {
        ArgumentNullException.ThrowIfNull(dbUrl);

        return new ArangoDBClient(HttpApiTransport.UsingBasicAuth(
            new Uri(dbUrl),
            "lemon",
            "adminmb",
            ""));
    }
}

[CollectionDefinition(nameof(DbSetupFixture))]
public class DatabaseSetupCollectionDefinition : ICollectionFixture<DbSetupFixture>
{
    // This class has no code, and is never created. Its purpose is simply
    // to be the place to apply [CollectionDefinition] and all the
    // ICollectionFixture<> interfaces.
}