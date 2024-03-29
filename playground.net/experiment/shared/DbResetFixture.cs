﻿using ArangoDBNetStandard;

namespace experiment;

/// <summary>
/// Reset data in the database
/// </summary>
public class DbResetFixture : IAsyncLifetime
{
    private readonly DbSetupFixture _dbSetupFixture;

    public DbResetFixture(DbSetupFixture dbSetupFixture)
    {
        _dbSetupFixture = dbSetupFixture;
    }
    public async Task InitializeAsync()
    {
        using var adb = _dbSetupFixture.CreateAdbClient();

        await ResetWallet(adb, "1");
        await ResetWallet(adb, "2");
        await ResetWallet(adb, "3");

        await removeAllTrans(adb);
    }

    private static async Task ResetWallet(ArangoDBClient adb, string walletKey)
    {
        await adb.Cursor.PostCursorAsync(@"
                UPSERT { _key: @walletKey }
                    INSERT { _key:@walletKey, balance:100 }
                    UPDATE { balance: 100 }
                IN wallet
            ", new Dictionary<string, object> { { "walletKey", walletKey } });
    }

    private static async Task removeAllTrans(ArangoDBClient adb)
    {
        await adb.Cursor.PostCursorAsync(@"
                FOR t IN trans
                    REMOVE t IN trans
            ");
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }
}
