using System.Threading;
using UnityEngine;

public partial class Benchmarking
{

    private void RunSafeMulti()
    {
        ThreadSafe<int> value = new(0);

        Thread threadA = new( () => AddFive(ref value) );
        
        Thread threadB = new( () => AddFive(ref value) );

        threadA.Start();
        threadB.Start();

        threadA.Join();
        threadB.Join();

        int expected = iterations * 10;

        Debug.Log($"Safe Multi Threaded | Expected: {expected}, Actual: {value.Value}");
    }
}