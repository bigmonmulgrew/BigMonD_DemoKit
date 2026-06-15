using System.Threading;
using UnityEngine;

public partial class Benchmarking
{
    private void RunMulti()
    {
        int value = 0;

        // We pass value as a reference because we want to modify the same variable from both threads.
        // If we passed it by value, each thread would have its own copy of the variable and the changes made in one thread would not be reflected in the other.
        Thread threadA = new( () => AddFive(ref value) );

        Thread threadB = new( () => AddFive(ref value) );

        threadA.Start();
        threadB.Start();

        threadA.Join();
        threadB.Join();

        int expected = iterations * 10;

        Debug.Log($"Unsafe Multi Threaded | Expected: {expected}, Actual: {value}");
    }


}