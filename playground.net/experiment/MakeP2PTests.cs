using arango_model_csharp;
using ArangoDBNetStandard;
using ArangoDBNetStandard.TransactionApi.Models;

namespace experiment;

[Collection(nameof(DbSetupFixture))]
public class MakeP2PTests : IClassFixture<DbResetFixture>
{
    private readonly DbSetupFixture _dbSetupFixture;

    public MakeP2PTests(DbSetupFixture dbSetupFixture)
    {
        _dbSetupFixture = dbSetupFixture;
    }
    [Fact]
    public async Task CreateP2P_Sucess()
    {
        var amount = 2;
        var walletSenderKey = "1";
        var walletReceiverKey = "2";

        //Act: Make P2P in a transaction then commit it
        using (var adb = _dbSetupFixture.CreateAdbClient())
        {

            var tx = await adb.Transaction.BeginTransaction(new StreamTransactionBody
            {
                Collections = new PostTransactionRequestCollections
                {
                    Write = ["wallet", "trans"]
                }
            });

            var dbTransactionId = tx.Result.Id;

            await createP2P(amount, walletSenderKey, walletReceiverKey, adb, dbTransactionId);

            await adb.Transaction.CommitTransaction(dbTransactionId);
        }

        //Assert

        using (var adb = _dbSetupFixture.CreateAdbClient())
        {
            var walletSender = await adb.Document.GetDocumentAsync<Wallet>("wallet", walletSenderKey);
            var walletReceiver = await adb.Document.GetDocumentAsync<Wallet>("wallet", walletReceiverKey);
            var resp = (await adb.Cursor.PostCursorAsync<Trans>("FOR t IN trans LIMIT 1 RETURN t"));
            var trans = resp.Result.First();

            Assert.Equal(98, walletSender.balance);
            Assert.Equal(102, walletReceiver.balance);
            Assert.Equal(2, trans.amount);
            Assert.Equal($"wallet/{walletSenderKey}", trans._from);
            Assert.Equal($"wallet/{walletReceiverKey}", trans._to);
        }
    }

    [Fact]
    public async Task CreateP2P_Aborted()
    {
        var amount = 2;
        var walletSenderKey = "1";
        var walletReceiverKey = "2";

        //Act: Make P2P in a transaction but abort it
        using (var adb = _dbSetupFixture.CreateAdbClient())
        {
            var tx = await adb.Transaction.BeginTransaction(new StreamTransactionBody
            {
                Collections = new PostTransactionRequestCollections
                {
                    Write = ["wallet", "trans"]
                },
            });

            var dbTransactionId = tx.Result.Id;

            await createP2P(amount, walletSenderKey, walletReceiverKey, adb, dbTransactionId);

            await adb.Transaction.AbortTransaction(dbTransactionId);
        }

        //Assert
        using (var adb = _dbSetupFixture.CreateAdbClient())
        {
            var walletSender = await adb.Document.GetDocumentAsync<Wallet>("wallet", walletSenderKey);
            var walletReceiver = await adb.Document.GetDocumentAsync<Wallet>("wallet", walletReceiverKey);
            var resp = await adb.Cursor.PostCursorAsync<Trans>("FOR t IN trans LIMIT 1 RETURN t");

            //that: wallet balances aren't changed
            Assert.Equal(100, walletSender.balance);
            Assert.Equal(100, walletReceiver.balance);
            //that: trans P2P was created
            Assert.False(resp.Result.Any());
        }
    }

    private static async Task createP2P(int amount, string walletSenderKey, string walletReceiverKey, ArangoDBClient adb, string dbTransactionId)
    {
        await UpdateWalletBalance(walletSenderKey, -amount, adb, dbTransactionId);
        await adb.Document.PostDocumentAsync("trans",
            new Trans { _from = $"wallet/{walletSenderKey}", _to = $"wallet/{walletReceiverKey}", amount = amount },
            headers: new ArangoDBNetStandard.DocumentApi.Models.DocumentHeaderProperties { TransactionId = dbTransactionId });
        await UpdateWalletBalance(walletReceiverKey, amount, adb, dbTransactionId);
    }

    private static async Task UpdateWalletBalance(string walletKey, int deltaAmount, ArangoDBClient adb, string transactionId)
    {
        await adb.Cursor.PostCursorAsync(@"
                LET w = DOCUMENT(CONCAT('wallet/', @walletSenderKey))
                UPDATE w WITH {
                    balance: w.balance+@deltaAmount
                } IN wallet"
            , new Dictionary<string, object> {
                    { "walletSenderKey",walletKey },
                    { "deltaAmount", deltaAmount }
        }, transactionId: transactionId);
    }

}
