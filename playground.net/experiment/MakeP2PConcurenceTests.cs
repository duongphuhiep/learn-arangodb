using arango_model_csharp;
using ArangoDBNetStandard;
using ArangoDBNetStandard.DocumentApi.Models;
using CodeTiger.Threading;
using System.Net;
using Xunit.Abstractions;

namespace experiment;

[Collection(nameof(DbSetupFixture))]
public class MakeP2PConcurenceTests : IClassFixture<DbResetFixture>
{
    private readonly DbSetupFixture _dbSetupFixture;
    private readonly ITestOutputHelper console;
    private readonly AsyncManualResetEvent updateSignal = new AsyncManualResetEvent(false);
    private readonly SemaphoreSlim readySignal = new SemaphoreSlim(0, 3);

    public MakeP2PConcurenceTests(DbSetupFixture dbSetupFixture, ITestOutputHelper console)
    {
        _dbSetupFixture = dbSetupFixture;
        this.console = console;
    }

    /// <summary>
    /// Create 3 tasks which will concurently read (the same) wallet balance
    /// then All of them try to increase the balance by 1 at the same time
    /// Only 1 task will success, others will failed
    /// </summary>
    /// <param name="walletKey"></param>
    /// <returns></returns>
    [Theory]
    [InlineData("3")]
    public async Task ModifyBalanceRaceCondition(string walletKey)
    {
        var ex = await Assert.ThrowsAsync<ApiErrorException>(() => Task.WhenAll(
            ModifyBalanceAfterSignal("task-A", walletKey),
            ModifyBalanceAfterSignal("task-B", walletKey),
            ModifyBalanceAfterSignal("task-C", walletKey),
            DoConcurencUpdate()));
        Assert.Equal("conflict, _rev values do not match", ex.ApiError.ErrorMessage);
        Assert.Equal(HttpStatusCode.PreconditionFailed, ex.ApiError.Code);
        Assert.Equal(1200, ex.ApiError.ErrorNum);
        using (var adb = _dbSetupFixture.CreateAdbClient())
        {
            var wallet = await adb.Document.GetDocumentAsync<Wallet>("wallet", walletKey);
            Assert.Equal(101, wallet.balance);
        }
    }

    /// <summary>
    /// Wait for all the ReadySignal then trigger the UpdateSignal for concurence update
    /// </summary>
    /// <returns></returns>
    async Task DoConcurencUpdate()
    {
        //wait for the readySignal (coming from other Task)
        await readySignal.WaitAsync();

        console.WriteLine("send update signal");

        //trigger the updatSignal (ask other Tasks perfom the Update)
        updateSignal.Set();
    }

    /// <summary>
    /// Read wallet balance from the database.
    /// Compute the new balance.
    /// Wait for signal.
    /// Write the new balance to the database.
    /// </summary>
    /// <param name="walletKey"></param>
    async Task ModifyBalanceAfterSignal(string taskName, string walletKey)
    {
        //Act
        using (var adb = _dbSetupFixture.CreateAdbClient())
        {
            var wallet = await adb.Document.GetDocumentAsync<Wallet>("wallet", walletKey);
            wallet.balance += 1;

            //notify the balance computation is finished and the result is ready to write to the database
            readySignal.Release();

            console.WriteLine($"{taskName} ready for database update, wait for update signal");
            await updateSignal.WaitOneAsync();

            console.WriteLine($"{taskName} starts update balance {wallet}");
            await adb.Document.PatchDocumentAsync("wallet/" + walletKey, wallet, new PatchDocumentQuery { IgnoreRevs = false });
            console.WriteLine($"{taskName} success update balance {@wallet}");
        }
    }

    [Theory]
    [InlineData("1")]
    public async Task ModifyBalanceSimple(string walletKey)
    {
        //Act
        using (var adb = _dbSetupFixture.CreateAdbClient())
        {
            var wallet = await adb.Document.GetDocumentAsync<Wallet>("wallet", walletKey);
            wallet.balance += 1;
            await adb.Document.PatchDocumentAsync("wallet/" + walletKey, wallet);
        }
        //Assert
        using (var adb = _dbSetupFixture.CreateAdbClient())
        {
            var wallet = await adb.Document.GetDocumentAsync<Wallet>("wallet", walletKey);
            Assert.Equal(101, wallet.balance);
        }
    }
}
