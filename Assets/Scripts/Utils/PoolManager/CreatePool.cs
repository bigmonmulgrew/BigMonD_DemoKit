using System;
using UnityEngine;

private IPool CreatePool(GameObject prefab)
{
    // Create Component for pool wrapper type
    var componentType = prefab.GetComponent<Component>().GetType();

    Type wrapperType = typeof(PoolWrapper<>).MakeGenericType(componentType);

    // Instantiate wrapper dynamically
    IPool wrapper = (IPool)Activator.CreateInstance(wrapperType, prefab);

    pools[prefab] = wrapper;

    return wrapper;
}