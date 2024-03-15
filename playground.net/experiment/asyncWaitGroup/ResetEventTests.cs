using CodeTiger.Threading;
using Xunit.Abstractions;

namespace experiment
{
    public class ResetEventTests
    {
        private readonly ITestOutputHelper console;

        public ResetEventTests(ITestOutputHelper testOutputHelper)
        {
            this.console = testOutputHelper;
        }

        [Fact]
        public async Task ManualResetEventTestAsync()
        {
            var manualResetEvent = new AsyncManualResetEvent(false);
            var t1 = Do(1, manualResetEvent);
            var t2 = Do(2, manualResetEvent);
            var t3 = Do(3, manualResetEvent);

            await Task.Delay(200);
            console.WriteLine("Set");
            manualResetEvent.Set();
            await Task.Delay(200);

            var t4 = Do(4, manualResetEvent);
            var t5 = Do(5, manualResetEvent);

            await Task.Delay(200);
            console.WriteLine("Reset");
            manualResetEvent.Reset();
            await Task.Delay(200);

            var t6 = Do(6, manualResetEvent);
            var t7 = Do(7, manualResetEvent);
            var t8 = Do(8, manualResetEvent);

            await Task.Delay(200);
            console.WriteLine("Set");
            manualResetEvent.Set();
            await Task.Delay(200);

            await Task.WhenAll(t1, t2, t3, t4, t5, t6, t7, t8);
        }

        [Fact]
        public async Task AutoResetEventTestAsync()
        {
            var autoResetEvent = new AsyncAutoResetEvent(false);
            var t1 = Do(1, autoResetEvent);
            var t2 = Do(2, autoResetEvent);
            var t3 = Do(3, autoResetEvent);

            await Task.Delay(200);
            console.WriteLine("Set");
            autoResetEvent.Set();
            await Task.Delay(200);

            await Task.Delay(200);
            console.WriteLine("Set");
            autoResetEvent.Set();
            await Task.Delay(200);

            await Task.Delay(200);
            console.WriteLine("Set");
            autoResetEvent.Set();
            await Task.Delay(200);

            await Task.WhenAll(t1, t2, t3);
        }

        async Task Do(int i, AsyncWaitHandle eventWaitHandle)
        {
            console.WriteLine($"Task {i} start");
            await Task.Delay(100);
            await eventWaitHandle.WaitOneAsync();
            console.WriteLine($"Task {i} end");
        }
    }
}
