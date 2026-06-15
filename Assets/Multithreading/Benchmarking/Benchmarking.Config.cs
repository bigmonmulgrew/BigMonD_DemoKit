using UnityEngine;

public partial class Benchmarking : MonoBehaviour
{
    public enum BenchmarkMode
    {
        All        = 0,
        Single     = 1,
        Multi      = 2,
        SafeMulti  = 3,
        SafeSingle = 4
    }

    [Header("Benchmark Config")]
    [SerializeField] BenchmarkMode benchmarkMode = BenchmarkMode.Single;

    [SerializeField] int iterations = 1;
}