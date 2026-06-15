using System.Collections.Generic;
using System.Diagnostics;
using Unity.Mathematics;

using Debug = UnityEngine.Debug;

public static class Timers
{
    private static readonly Dictionary<string, Stopwatch> timers = new();

    public static void Start(string name = "default")
    {
        GetTimer(name).Start();
    }

    public static void Stop(string name = "default")
    {
        GetTimer(name).Stop();
    }

    public static void Reset(string name = "default")
    {
        GetTimer(name).Reset();
    }

    public static double Time(string name = "default")
    {
        return GetTimer(name).Elapsed.TotalSeconds;
    }

    public static double TimeMs(string name = "default")
    {
        return GetTimer(name).Elapsed.TotalMilliseconds;
    }

    private static Stopwatch GetTimer(string name)
    {
        if (!timers.TryGetValue(name, out Stopwatch timer))
        {
            timer = new();
            timers.Add(name, timer);
        }

        return timer;
    }
}