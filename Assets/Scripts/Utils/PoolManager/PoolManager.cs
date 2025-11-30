using UnityEngine;
using UnityEngine.Pool;
using System.Collections.Generic;
using System;

namespace Utils
{
    public class PoolManager : MonoBehaviour
    {
        #region Statics
        public static PoolManager Instance;
        #endregion

        #region Runtime Variables
        // Gameobject references are created by hashing the object to create a fingerprint, works all objects including runtime generated.
        Dictionary<int, Pool> poolMap= new(); 
        #endregion

        private void Awake()
        {
            SetupDefaultPools();
        }

        private void SetupDefaultPools()
        {
            foreach (var poolDef in PoolConfig.DefaultPools)
            {
                int hash = HashUtils.GetObjectHash(poolDef.Prefab);
                poolMap[hash] = poolDef;
                poolMap[hash].InitPool();
            }
        }

        #region Get Object

        public static GameObject Get(GameObject gameObject)
        {
            // Placeholder default instanticate for now
            var go = Instantiate(gameObject);

            return go;
        }
        #endregion

        #region Release object
        public static void Release(GameObject go)
        {
            // Placeholder default destroy for now
            Destroy(go);
        }
        #endregion

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void RuntimeInit()
        {
            if (Instance != null) return;

            // Create singleton pool manager object
            GameObject poolGameObject = new GameObject("Utilities: Pool Manager");
            Instance = poolGameObject.AddComponent<PoolManager>();
            DontDestroyOnLoad(poolGameObject);

        }
    }
}