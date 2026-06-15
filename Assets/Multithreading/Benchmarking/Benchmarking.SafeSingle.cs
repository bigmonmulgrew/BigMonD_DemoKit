using UnityEngine;

public partial class Benchmarking
{
    private void RunSafeSingle()
    {
        ThreadSafe<int> value = new (0);

        for (int i = 0; i < iterations; i++)
        {
            value.Value += 5;
            value.Value += 5;
        }

        int expected = iterations * 10;

        Debug.Log($"Safe Single Threaded | Expected: {expected}, Actual: {value.Value}");
    }
}