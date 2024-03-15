using arango_model_csharp;
using ArangoDBNetStandard;
using ArangoDBNetStandard.DocumentApi.Models;
using ArangoDBNetStandard.TransactionApi.Models;
using Bogus;
using CodeTiger.Threading;
using System.Net;
using Xunit.Abstractions;

namespace experiment
{
    /// <summary>
    /// See impact of a ArangoDB transaction 
    /// when it hold the write lock on the wallet collection to update the wallet balance then 
    /// the lock will block everybody from editing the wallet name
    /// </summary>
    [Collection(nameof(DbSetupFixture))]
    public class TransactionLockTest : IClassFixture<DbResetFixture>
    {
        private readonly Faker _faker = new Faker();
        private readonly AsyncManualResetEvent updateSignal = new AsyncManualResetEvent(false);
        private readonly AsyncManualResetEvent readySignal = new AsyncManualResetEvent(false);
        private readonly DbSetupFixture _dbSetupFixture;
        private readonly ITestOutputHelper console;

        public TransactionLockTest(DbSetupFixture dbSetupFixture, ITestOutputHelper testOutputHelper)
        {
            _dbSetupFixture = dbSetupFixture;
            console = testOutputHelper;
        }

        /// <summary>
        /// Concurence test
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task UpdateWhileWriteLockTest()
        {
            await Task.WhenAny(UpdateWalleBalanceInTransaction("1", "2"), readySignal.WaitOneAsync());

            // expect success, the wallet "3" is not locked by the transaction
            await UpdateWalletName("3", _faker.Name.FullName());

            //expect to failed because the transaction lock

            var ex1 = await Assert.ThrowsAsync<ApiErrorException>(async () => await UpdateWalletName("1", _faker.Name.FullName()));
            Assert.Equal(HttpStatusCode.Conflict, ex1.ApiError.Code);
            Assert.Equal(1200, ex1.ApiError.ErrorNum);
            Assert.StartsWith("timeout waiting to lock key Operation timed out: Timeout waiting to lock key - in index primary of type primary over '_key'; conflicting key:", ex1.ApiError.ErrorMessage);


            var ex2 = await Assert.ThrowsAsync<ApiErrorException>(async () => await UpdateWalletName("2", _faker.Name.FullName()));
            Assert.Equal(HttpStatusCode.Conflict, ex2.ApiError.Code);
            Assert.Equal(1200, ex2.ApiError.ErrorNum);
            Assert.StartsWith("timeout waiting to lock key Operation timed out: Timeout waiting to lock key - in index primary of type primary over '_key'; conflicting key:", ex2.ApiError.ErrorMessage);
        }

        /// <summary>
        /// Normal execution (no concurence)
        /// </summary>
        /// <param name="walletKey"></param>
        /// <returns></returns>
        [Theory]
        [InlineData("1")]
        public async Task UpdateWalletNameTest(string walletKey)
        {
            var newName = _faker.Name.FullName();
            await UpdateWalletName(walletKey, newName);

            var wallet = await GetWallet(walletKey);
            Assert.Equal(newName, wallet.name);
        }


        /// <summary>
        /// Normal execution (no concurence)
        /// </summary>
        /// <param name="walletKey"></param>
        /// <returns></returns>
        [Theory]
        [InlineData("1", "2")]
        public async Task UpdateWalleBalanceInTransactionTest(string walletKey1, string walletKey2)
        {
            updateSignal.Set();
            await UpdateWalleBalanceInTransaction(walletKey1, walletKey2);

            var wallet1 = await GetWallet(walletKey1);
            Assert.Equal(101, wallet1.balance);

            var wallet2 = await GetWallet(walletKey2);
            Assert.Equal(101, wallet2.balance);
        }

        async Task UpdateWalletName(string walletKey, string newName)
        {
            console.WriteLine("UpdateWalletName...");
            using (var adb = _dbSetupFixture.CreateAdbClient())
            {
                var wallet = await adb.Document.GetDocumentAsync<Wallet>(collectionName: "wallet", documentKey: walletKey);
                wallet.name = newName;
                await adb.Document.PatchDocumentAsync<Wallet, Wallet>(collectionName: "wallet", documentKey: walletKey, body: wallet);
            }
        }

        async Task UpdateWalleBalanceInTransaction(string walletKey1, string walletKey2)
        {
            console.WriteLine("UpdateWalleBalanceInTransaction...");
            using (var adb = _dbSetupFixture.CreateAdbClient())
            {

                var tx = await adb.Transaction.BeginTransaction(new StreamTransactionBody
                {
                    Collections = new PostTransactionRequestCollections
                    {
                        Write = ["wallet"],
                    }
                });

                var dbTransactionId = tx.Result.Id;

                console.WriteLine($"Patch wallet {walletKey1}");

                var wallet1 = await adb.Document.GetDocumentAsync<Wallet>(collectionName: "wallet", documentKey: walletKey1, headers: new DocumentHeaderProperties { TransactionId = dbTransactionId });
                wallet1.balance += 1;
                await adb.Document.PatchDocumentAsync<Wallet, Wallet>(collectionName: "wallet", documentKey: walletKey1, body: wallet1, headers: new DocumentHeaderProperties { TransactionId = dbTransactionId });

                console.WriteLine($"Patch wallet {walletKey2}");

                var wallet2 = await adb.Document.GetDocumentAsync<Wallet>(collectionName: "wallet", documentKey: walletKey2, headers: new DocumentHeaderProperties { TransactionId = dbTransactionId });
                wallet2.balance += 1;
                await adb.Document.PatchDocumentAsync<Wallet, Wallet>(collectionName: "wallet", documentKey: walletKey2, body: wallet2, headers: new DocumentHeaderProperties { TransactionId = dbTransactionId });


                //the wallet1 and wallet2 is locked here
                readySignal.Set();
                console.WriteLine("Pause UpdateWalleBalanceInTransaction");
                await updateSignal.WaitOneAsync();

                await adb.Transaction.CommitTransaction(dbTransactionId);
                console.WriteLine("Commit transaction");
            }
        }

        async Task<Wallet> GetWallet(string walletKey)
        {
            using (var adb = _dbSetupFixture.CreateAdbClient())
            {
                return await adb.Document.GetDocumentAsync<Wallet>(collectionName: "wallet", documentKey: walletKey);
            }
        }
    }
}
