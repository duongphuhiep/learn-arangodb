using Xunit.Abstractions;

namespace experiment;

public class CountDownEventTests
{
    private readonly ITestOutputHelper console;
    private readonly SemaphoreSlim semaphoreSlim = new SemaphoreSlim(0, 2);

    public CountDownEventTests(ITestOutputHelper testOutputHelper)
    {
        console = testOutputHelper;
    }

    [Fact]
    public async Task CountDownEventTest()
    {
        await Task.WhenAll(WaitCountdown(), Do(1), Do(2));
    }

    async Task Do(int i)
    {
        console.WriteLine($"Task {i} start");
        await Task.Delay(100);
        semaphoreSlim.Release();
        console.WriteLine($"Task {i} end");
    }

    async Task WaitCountdown()
    {
        console.WriteLine($"WaitCountDown start");
        await semaphoreSlim.WaitAsync();
        console.WriteLine($"WaitCountDown end");
    }
}
