# Learn [ArangoDB]

Perform various experiments on [ArangoDB]

## Start an ArangoDB on `localhost:8529` to play with

- Goto [`database`](./database/) folder
- `docker compose up`

## Experiments in [`playground.net`](./playground.net/)

- The tests use [Testscontainer] to setup a ArangoDB on localhost (random port)
- The database is reset to initial data after each test case.
- The initial data has 3 wallets '1', '2', '3', with balance = 100 for each.
- The goal is to transfer money from a wallet to other wallet (P2P) by creating a new "trans" edge and adjusting wallets balance; all in the same "database transaction".

I performed some experiments to see How ArangoDB would help to keep the database consistence.

**Experiment 1)** Happy path test:

[Source code](./playground.net/experiment/MakeP2PTests.cs):

- Open a database transaction
- perform the P2P
- Commit or Abort the database transaction should give expected result

**Experiment 2)** Race condition protection (without opening a transaction, nor locking anything)

[Source code](./playground.net/experiment/MakeP2PConcurenceTests.cs):

- execute 3 tasks which concurently increase the wallet balance by 1
- only 1 task would successfully modify the wallet balance, the other 2 must failed
- the wallet balance is increase to 1 after the test

**Experiment 3)** Database Transaction locking experiment

[Source code](./playground.net/experiment/TransactionLockTest.cs):

- Open a transaction to update the balance of the wallet "1 and "2" (in the same transaction).
- The wallet "1" and "2" are (write) locked until the transaction is committed. The test was unable to modify the name of these 2 wallets.
- The wallet "3" is not locked. The test successfully modify the name of the wallet "3".

## My first impression

- ArangoDb is easy to learn compare to other databases (Mongo, Couchbase, Dgraph, RavenDb..). I feel like becoming productive real quick:
  - ArangoDb is like an easier Document database than MongoDB, Couchbase and at the same time: an easier Graph database than DGraph.
  - I enjoyed writing [AQL](https://docs.arangodb.com/3.12/aql/) (a lot) more than [DQL](https://dgraph.io/docs/dql/) or [Cypher](https://neo4j.com/docs/getting-started/cypher-intro/)
- (Not happy) [.NET driver](https://github.com/ArangoDB-Community/arangodb-net-standard) is (not yet) supporting [Velostream](https://github.com/arangodb/velocystream)
- (Skeptical) [Foxx](https://docs.arangodb.com/3.11/develop/foxx-microservices/): IMO, it is similarly to stored-proc.. just more advance / power.. it is a good thing we have a powerful tool, just use with extra-caution + justification.

[ArangoDB]: https://arangodb.com/
[Testscontainer]: https://testcontainers.com/
