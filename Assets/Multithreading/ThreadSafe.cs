public class ThreadSafe<T>
{
    private T backingField;

    public ThreadSafe(T initialValue = default)
    {
        backingField = initialValue;
    }

    public T Value
    {
        get => backingField;
        set => backingField = value;
    }
}