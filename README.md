# Learn [ArangoDB]

Perform difference experiments on [ArangoDB]

## Start an ArangoDB on `localhost:8529` to play

- Goto [`database`](./database/) folder
- `docker compose up`

## Various Experiment in [`playground.net`](./playground.net/)

- The tests use [Testscontainer] to setup a ArangoDB on localhost (random port) before running
- It creates 3 wallets '1', '2', '3', with balance = 100 for each.
- The tests will create some P2P to transfer money from a wallet to other wallet

I performed some experiments to see How ArangoDB would help to keep the database consistence.

**Experiment 1)** Happy path test:

[Source code](./playground.net/experiment/MakeP2PTests.cs):

- Open a database transaction
- perform the P2P
- Commit or Abort the database transaction should give expected result

**Experiment 2)** Race condition protection (without opening a transaction or lock anything)

[Source code](./playground.net/experiment/MakeP2PConcurenceTests.cs):

- execute 3 tasks which try to increase the wallet balance by 1 concurently
- only 1 task will successfully modify the wallet balance, the other 2 must to failed
- the wallet balance is increase to 1 after the test

**Experiment 3)** Database Transaction locking experiment

[Source code](./playground.net/experiment/TransactionLockTest.cs):

- Open a transaction to update the balance of the wallet "1 and "2" (in the same transaction).
- The wallet "1" and "2" are (write) locked until the transaction is committed. The test was unable to modify the name of these 2 wallets.
- The wallet "3" is not locked. The test successfully modify the name of the wallet "3".

## My first impression

- Easy to learn and become productive in a very short time
- An easier Document database than MongoDB
- I enjoyed writing [AQL](https://docs.arangodb.com/3.12/aql/) more than [DQL](https://dgraph.io/docs/dql/)

[ArangoDB]: https://arangodb.com/
[Testscontainer]: https://testcontainers.com/