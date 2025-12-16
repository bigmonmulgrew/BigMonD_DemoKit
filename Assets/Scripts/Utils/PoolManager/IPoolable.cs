
public interface IPoolable
{
    void OnGet();
    void OnRelease();
    void OnDestroyed();
}