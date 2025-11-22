using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance;

    #region Configuration
    [Header("Chunk tempaltes")]
    [SerializeField] SceneAsset[] roomScenes;
    [SerializeField] SceneAsset[] bridgeScenesNorthSouth;
    [SerializeField] SceneAsset[] bridgeScenesEastWest;

    [Header("Generation Settings")]
    [Range(1, 50)]
    [Tooltip("Number of room chunks to generate, bridge chunks will be added automatically where relevant.")]
    [SerializeField] int roomChunks = 5;

    [Tooltip("Delay in frames between chunk generation to avoid performance spikes")]
    [SerializeField] int chunkProcessingDelay = 1;
    #endregion

    #region Cached References
    List<Scene> loadedScenes = new List<Scene>();
    List<Scene> activeScenes = new List<Scene>();

    Scene startScene;
    string startSceneName;

    Dictionary<Vector3Int, Scene> chunkLookup = new Dictionary<Vector3Int, Scene>();    // Storing as Vector3Int for future 3D expansion. This gives chunk relative position.
    #endregion

    #region Runtime Variables
    Coroutine generateCoroutine;
    #endregion

    public static Scene StartScene => Instance.startScene;

    private void Awake()
    {
        if (SetupInstance()) return;        // Setup instance and exit if this object was destroyed
  
        startScene = SceneManager.GetActiveScene();
        startSceneName = startScene.name;

        loadedScenes.Add(startScene);

        GenerateChunks();
        //LoadChunks();
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
    private void GenerateChunks()
    {

        generateCoroutine = StartCoroutine(GenerateChunksCoroutine());
    }
    IEnumerator GenerateChunksCoroutine()
    {
        for (int i = 0; i < roomChunks - 1; i++)
        {
            yield return GenerateChunk(i);
        }

        generateCoroutine = null;
    }

    /// <summary>
    /// Introduces a delay in chunk processing by yielding control for a specified number of frames.
    /// </summary>
    IEnumerator ChunkProcessingDelay()
    {
        for (int f = 0; f < chunkProcessingDelay; f++) yield return null;
    }


    IEnumerator GenerateChunk(int i)
    {
        // Delay to avoid performance spikes
        yield return ChunkProcessingDelay();

        // Determine chunk grid position
        Vector3Int chunkPosition = new Vector3Int(i, 0, 0);     // TODO linerly along x axis for now
        string chunkName = $"{startSceneName}_Chunk_{i}";

        // Create new in-memory additive scene
        Scene chunkScene = SceneManager.GetSceneByName(startSceneName);

        // Pick a random template room scene and load it additively
        string templateSceneName = roomScenes[UnityEngine.Random.Range(0, roomScenes.Length)].name;

        // Load and wait for it to complete
        AsyncOperation loadOp = SceneManager.LoadSceneAsync(templateSceneName, LoadSceneMode.Additive);
        yield return loadOp;

        Scene templateScene = SceneManager.GetSceneByName(templateSceneName);
        if (!templateScene.isLoaded)
        {
            Debug.LogError("Failed to load template scene: " + templateSceneName);
            yield break;
        }

        // Clone the root objects from the template into the chunk scene
        foreach (GameObject root in templateScene.GetRootGameObjects())
        {
            GameObject clone = Instantiate(root);
            SceneManager.MoveGameObjectToScene(clone, chunkScene);
        }

        // Store the in-memory chunk scene
        chunkLookup[chunkPosition] = chunkScene;

    }
    private void LoadChunks()
    {
        StartCoroutine(LoadChunkScenes());
    }
    IEnumerator LoadChunkScenes()
    {
        // Wait for generation to complete
        while (generateCoroutine != null) yield return null;

        
        // Load each chunk scene
        foreach (var kvp in chunkLookup)
        {
            Scene chunkScene = kvp.Value;

            // Delay to avoid performance spikes
            yield return ChunkProcessingDelay();

            AsyncOperation op = SceneManager.LoadSceneAsync(chunkScene.name, LoadSceneMode.Additive);

            loadedScenes.Add(chunkScene);
            yield return op;
        }

    }

}
