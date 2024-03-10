using arango_model_csharp;
using ArangoDBNetStandard;
using ArangoDBNetStandard.CollectionApi.Models;
using ArangoDBNetStandard.DatabaseApi;
using ArangoDBNetStandard.DatabaseApi.Models;
using ArangoDBNetStandard.Transport.Http;

namespace experiment;

public class DbSetup
{
    [Fact]
    async Task CreateSomeWallets()
    {
        using var adb = CreateAdbClient();
        await adb.Document.PostDocumentAsync("wallet", new Wallet { _key = "1", balance = 100 });
        await adb.Document.PostDocumentAsync("wallet", new Wallet { _key = "2", balance = 100 });
        await adb.Document.PostDocumentAsync("wallet", new Wallet { _key = "3", balance = 100 });
    }

    [Fact]
    async Task AddCollection()
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

    [Fact]
    async Task CreateDatabase()
    {
        // You must use the _system database to create databases
        using (var systemDbTransport = HttpApiTransport.UsingBasicAuth(
        new Uri("http://localhost:8529/"),
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

    public static ArangoDBClient CreateAdbClient()
    {
        return new ArangoDBClient(HttpApiTransport.UsingBasicAuth(
            new Uri("http://localhost:8529"),
            "lemon",
            "adminmb",
            ""));
    }

}