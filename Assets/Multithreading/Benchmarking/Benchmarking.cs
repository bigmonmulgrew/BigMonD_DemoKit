using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

using Debug = UnityEngine.Debug;

public partial class Benchmarking : MonoBehaviour
{
    const int STARTUP_DELAY = 3;        // Delay in seconds before starting the benchmark to allow Unity to initialize
    private const string TimerName = "Benchmark";

    private void Start()
    {
        // Start work with a small delay to ensure that the Unity engine has fully initialized before running benchmarks.
        StartCoroutine(DelayedStart());
    }

    IEnumerator DelayedStart()
    {
        // We call the timers now to that constructors run before we need to access them.
        // This is to avoid the overhead of creating the timers during the benchmark.
        Timers.Reset(TimerName);
        Timers.Start(TimerName);
        Timers.Stop(TimerName);

        yield return new WaitForSeconds(STARTUP_DELAY);
        RunBenchmarks();
    }
    void RunBenchmarks()
    {
        Timers.Reset(TimerName);
        Timers.Start(TimerName);

        switch (benchmarkMode)
        {
            case BenchmarkMode.Single:
                RunSingle();
                break;

            case BenchmarkMode.Multi:
                RunMulti();
                break;

            case BenchmarkMode.SafeMulti:
                RunSafeMulti();
                break;

            case BenchmarkMode.SafeSingle:
                RunSafeSingle();
                break;
            case BenchmarkMode.All:
                RunSingle();
                RunMulti();
                RunSafeMulti();
                RunSafeSingle();
                break;
        }

        Timers.Stop(TimerName);

        Debug.Log($"Benchmark complete. Mode: {benchmarkMode}, Time: {Timers.TimeMs(TimerName):F4} ms");
    }
}