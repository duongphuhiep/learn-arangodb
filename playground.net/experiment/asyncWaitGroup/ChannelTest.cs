using System.Threading.Channels;

namespace experiment
{
    public class ChannelTest
    {
        [Fact(Skip = "Not work! channel never close, wait infinite")]
        public async Task AggregationTest()
        {
            var tasks = new Func<int>[] { DoWork1, DoWork2, DoWork3 };
            var resultsChannel = Channel.CreateUnbounded<int>();

            foreach (var task in tasks)
            {
                _ = Task.Run(async () =>
                {
                    int result = task();
                    await resultsChannel.Writer.WriteAsync(result);
                });
            }

            int total = 0;
            await foreach (var result in resultsChannel.Reader.ReadAllAsync())
            {
                total += result;
            }

            Console.WriteLine($"Total: {total}");
        }

        static int DoWork1() { /* Simulated work */ return 1; }
        static int DoWork2() { /* Simulated work */ return 2; }
        static int DoWork3() { /* Simulated work */ return 3; }
    }
}
