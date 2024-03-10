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
    public class TransactionLockTest : IClassFixture<DbResetFixture>
    {
        private readonly Faker _faker = new Faker();
        private readonly AsyncManualResetEvent updateSignal = new AsyncManualResetEvent(false);
        private readonly AsyncManualResetEvent readySignal = new AsyncManualResetEvent(false);
        private readonly ITestOutputHelper console;

        public TransactionLockTest(ITestOutputHelper testOutputHelper)
        {
            console = testOutputHelper;
        }


        /// <summary>
        /// Concurence test
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task UpdateWhileWriteLockTest()
        {
            await Task.WhenAny(UpdateWalleBalanceInTransaction("1"), readySignal.WaitOneAsync());

            //expect to failed because the transaction 
            var ex = await Assert.ThrowsAsync<ApiErrorException>(async () => await UpdateWalletName("1", _faker.Name.FullName()));
            Assert.Equal(HttpStatusCode.Conflict, ex.ApiError.Code);
            Assert.Equal(1200, ex.ApiError.ErrorNum);
            Assert.Equal("timeout waiting to lock key Operation timed out: Timeout waiting to lock key - in index primary of type primary over '_key'; conflicting key: 1", ex.ApiError.ErrorMessage);
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
        [InlineData("1")]
        public async Task UpdateWalleBalanceInTransactionTest(string walletKey)
        {
            updateSignal.Set();
            await UpdateWalleBalanceInTransaction(walletKey);

            var wallet = await GetWallet(walletKey);
            Assert.Equal(101, wallet.balance);
        }

        async Task UpdateWalletName(string walletKey, string newName)
        {
            console.WriteLine("UpdateWalletName...");
            using (var adb = DbSetup.CreateAdbClient())
            {
                var wallet = await adb.Document.GetDocumentAsync<Wallet>(collectionName: "wallet", documentKey: walletKey);
                wallet.name = newName;
                await adb.Document.PatchDocumentAsync<Wallet, Wallet>(collectionName: "wallet", documentKey: walletKey, body: wallet);
            }
        }

        async Task UpdateWalleBalanceInTransaction(string walletKey)
        {
            console.WriteLine("UpdateWalleBalanceInTransaction...");
            using (var adb = DbSetup.CreateAdbClient())
            {

                var tx = await adb.Transaction.BeginTransaction(new StreamTransactionBody
                {
                    Collections = new PostTransactionRequestCollections
                    {
                        Write = ["wallet"],
                    }
                });


                var dbTransactionId = tx.Result.Id;

                var wallet = await adb.Document.GetDocumentAsync<Wallet>(collectionName: "wallet", documentKey: walletKey, headers: new DocumentHeaderProperties { TransactionId = dbTransactionId });
                wallet.balance += 1;
                await adb.Document.PatchDocumentAsync<Wallet, Wallet>(collectionName: "wallet", documentKey: walletKey, body: wallet, headers: new DocumentHeaderProperties { TransactionId = dbTransactionId });

                //the wallet is locked here
                readySignal.Set();
                console.WriteLine("Pause UpdateWalleBalanceInTransaction");
                await updateSignal.WaitOneAsync();

                await adb.Transaction.CommitTransaction(dbTransactionId);
            }
        }

        async Task<Wallet> GetWallet(string walletKey)
        {
            using (var adb = DbSetup.CreateAdbClient())
            {
                return await adb.Document.GetDocumentAsync<Wallet>(collectionName: "wallet", documentKey: walletKey);
            }
        }
    }
}
