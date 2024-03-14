using Testcontainers.ArangoDb;

await using var container = new ArangoDbBuilder().Build();

await container.StartAsync();

var dbUrl = container.GetTransportAddress();

Console.WriteLine($"Started {dbUrl}");
