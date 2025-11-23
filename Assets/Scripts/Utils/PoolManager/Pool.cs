using UnityEngine;
using UnityEngine.Pool;
using System.Collections.Generic;
namespace Utils
{
    /// <summary>
    /// Defines a pool of objects for pooling purposes.
    /// </summary>
    [System.Serializable]
    public class Pool
    {
        #region Configuration
        [SerializeField] GameObject prefab;

        [Min(1)] [SerializeField] int defaultSize = 10;
        [Min(0)] [SerializeField] int minSize = 5;
        [Min(2)] [SerializeField] int maxSize = 100;

        [SerializeField] int expansionPriority = 0;
        #endregion

        #region Runtime Variables
        private ObjectPool<GameObject> pool;
        #endregion

        #region Properties
        public GameObject Prefab => prefab;
        public int Size => pool.CountAll;
        #endregion

        public void InitPool() 
        {
            pool = new ObjectPool<GameObject>(
                createFunc: CreateItem,
                actionOnGet: OnGet,
                actionOnRelease: OnRelease,
                actionOnDestroy: OnDestroyItem,
                collectionCheck: true,   // helps catch double-release mistakes
                defaultCapacity: defaultSize,
                maxSize: 100                
            ); 
        }

        public GameObject Get()
        {
            GameObject go = pool.Get();
            // Get the first object in pooledObjects
            //if (pooledObjects.Count > 0)
            //{
            //    var obj = pooledObjects[0];
            //    pooledObjects.RemoveAt(0);
            //    obj.SetActive(true);
            //    return obj;
            //}
            //else
            //{
            //    // If no objects are available, instantiate a new one
            //    var newObj = GameObject.Instantiate(prefab);
            //    return newObj;
            //}
            return go;
        }

        // Creates a new pooled GameObject the first time (and whenever the pool needs more).
        private GameObject CreateItem()
        {
            GameObject gameObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            gameObject.name = "PooledCube";
            gameObject.SetActive(false);
            return gameObject;
        }

        // Called when an item is taken from the pool.
        private void OnGet(GameObject gameObject)
        {
            gameObject.SetActive(true);
        }

        // Called when an item is returned to the pool.
        private void OnRelease(GameObject gameObject)
        {
            gameObject.SetActive(false);
        }

        // Called when the pool decides to destroy an item (e.g., above max size).
        private void OnDestroyItem(GameObject gameObject)
        {
            MonoBehaviour.Destroy(gameObject);
        }
    }
}