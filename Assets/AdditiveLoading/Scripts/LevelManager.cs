using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System;
using System.Collections;

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance;

    #region Configuration
    [SerializeField] Scene[] roomScenes;
    [SerializeField] Scene[] bridgeScenesNorthSouth;
    [SerializeField] Scene[] bridgeScenesEastWest;
    [Range(1,50)]
    [SerializeField] int roomChunks;

    [Tooltip("Delay in frames between chunk generation to avoid performance spikes")]
    [SerializeField] int chunkGenerationDelay;
    #endregion

    #region Cached References
    List<Scene> loadedScenes = new List<Scene>();
    Scene startScene;
    Dictionary<string, Scene> chunkLookup = new Dictionary<string, Scene>();
    #endregion

    public static Scene StartScene => Instance.startScene;

    private void Awake()
    {
        if (SetupInstance()) return;        // Setup instance and exit if this object was destroyed

        startScene = SceneManager.GetActiveScene();
        loadedScenes.Add(startScene);

        GenerateChunkScenes();
    }

    /// <summary>
    /// Ensures that only one instance of the object exists in the scene.
    /// </summary>
    /// <remarks>If no instance exists, this object is set as the instance and marked to persist across scene
    /// loads.  If an instance already exists, this object is destroyed.</remarks>
    /// <returns><see langword="true"/> if the object was destroyed because an instance already exists;  otherwise, <see
    /// langword="false"/>.</returns>
    private bool SetupInstance()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return true;
        }

        return false;
    }

    private void GenerateChunkScenes()
    {
        StartCoroutine(GenerateChunks());
    }

    IEnumerator GenerateChunks()
    {
        int i = 0;

        while (i < roomChunks)
        {
            // Create room chunk
            string sceneName = $"RoomChunk_{i}";

            // Select random room scene and create a copy
            Scene roomScene = roomScenes[UnityEngine.Random.Range(0, roomScenes.Length)];
            Scene newScene = SceneManager.CreateScene(sceneName);

            yield return null;

        }
    }

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
