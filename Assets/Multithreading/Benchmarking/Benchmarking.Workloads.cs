using System.Threading;

public partial class Benchmarking
{
    void AddFive(ref int value)
    {
        for (int i = 0; i < iterations; i++)
        {
            int localCopy = value;
            Thread.Sleep(1);
            value = localCopy + 5;
        }
    }

    void AddFive(ref ThreadSafe<int> value)
    {
        for (int i = 0; i < iterations; i++)
        {
            // Lock must use an object common to all threads, so we use the Benchmarking instance itself.
            // This is not ideal for real-world code, but it serves the purpose of this benchmark.
            lock (this)
            {
                value.Value += 5;
            }
        }
    }
}
