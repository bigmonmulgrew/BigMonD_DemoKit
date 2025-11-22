using NUnit.Framework.Internal.Commands;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Provides functionality to manage the state of objects within the current scene
/// </summary>
public class ChunkManager : MonoBehaviour
{
    #region Configuration
    [Tooltip("Delay in seconds before loading assets for a chunk after it becomes active")]
    [SerializeField] float assetLoadDelay = 0.1f;
    [SerializeField] GameObject[] _ignoreList;
    #endregion

    #region Runtime Variables
    List<GameObject> ignoreList = new List<GameObject>();
    List<GameObject> managedObjects = new List<GameObject>();
    #endregion

    private void Start()
    {
        BuildRuntimeIgnoreList();
        BuildManagedObjectList();

        ShowHideManaged(false);

        StartCoroutine(SlowLoad());
    }

    private void BuildManagedObjectList()
    {
        // Find all root objects in the scene
        List<GameObject> rootObjects = gameObject.scene.GetRootGameObjects().ToList<GameObject>();

        // Set rootObjects to managedObjects excluding those in ignoreList
        // Filter first to prevent reprocessing child of ignored.
        managedObjects = rootObjects.FindAll(obj => !ignoreList.Contains(obj));

        foreach (var obj in managedObjects.ToArray())
        {
            // Also add all children of managed objects
            foreach (Transform child in obj.transform)
            {
                if (ignoreList.Contains(child.gameObject)) continue;    // Skip any ignored children

                managedObjects.Add(child.gameObject);
            }
        }
    }

    private void BuildRuntimeIgnoreList()
    {
        // Add self to ignore list
        ignoreList.Add(gameObject);

        // Build the ignore list including all children of the specified objects
        foreach (var obj in _ignoreList)
        {
            if (obj == null) continue;  // Skip null

            ignoreList.Add(obj);
            
            // Also ignore all children
            foreach (Transform child in obj.transform)
            {
                ignoreList.Add(child.gameObject);
            }
        }

        // Apply filrers to ignoreList 
        // TODO : consider what object types need to be filtered out
        // TODO : Make this configurable
        // TODO : Add tag based filtering
        // TODO : Add layer based filtering
        // TODO : Add component based filtering
        ignoreList = ignoreList.Where(obj => obj != Camera.main).ToList();
    }

    IEnumerator SlowLoad()
    {
        GameObject[] objects = managedObjects.Where(obj => obj != null).ToArray();

        foreach(var obj in objects)
        {
            obj.SetActive(true);
            yield return new WaitForSeconds(assetLoadDelay);
        }
    }

    private void ShowHideManaged(bool show)
    {
        foreach(var obj in managedObjects)
        {
            if (obj == null) continue;
            obj.SetActive(show);
        }
    }
}
