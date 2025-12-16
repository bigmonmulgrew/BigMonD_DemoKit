using UnityEngine;

public class PoolWrapper<T> : IPool where T : Component
{
    private ObjectPool<T> pool;
    private T prefab;

    public PoolWrapper(GameObject prefabGO)
    {
        this.prefab = prefabGO.GetComponent<T>();

        pool = new ObjectPool<T>(
            createFunc: Create,
            actionOnGet: OnGet,
            actionOnRelease: OnRelease,
            actionOnDestroy: OnDestroy
        );
    }

    private T Create()
    {
        var instance = UnityEngine.Object.Instantiate(prefab);
        var data = instance.gameObject.AddComponent<PoolInstanceData>();
        data.originPrefab = prefab.gameObject;
        return instance;
    }

    private void OnGet(T instance)
    {
        instance.gameObject.SetActive(true);
        (instance as IPoolable)?.OnGet();
    }

    private void OnRelease(T instance)
    {
        (instance as IPoolable)?.OnRelease();
        instance.gameObject.SetActive(false);
    }

    private void OnDestroy(T instance)
    {
        (instance as IPoolable)?.OnDestroyed();
        UnityEngine.Object.Destroy(instance.gameObject);
    }

    public UnityEngine.Object Get() => pool.Get();
    public void Release(UnityEngine.Object obj) => pool.Release((T)obj);
}
