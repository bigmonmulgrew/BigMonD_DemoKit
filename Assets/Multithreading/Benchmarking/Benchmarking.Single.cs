using UnityEngine;

public partial class Benchmarking
{
    private void RunSingle()
    {
        int value = 0;

        for (int i = 0; i < iterations; i++)
        {
            value += 5;
            value += 5;
        }

        int expected = iterations * 10;

        Debug.Log($"Single Threaded | Expected: {expected}, Actual: {value}");
    }
}