using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System;

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
        bool flowControl = SetupInstance();
        if (!flowControl)
        {
            return;
        }
        startScene = SceneManager.GetActiveScene();
        loadedScenes.Add(startScene);

        GenerateChunks();
    }

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
            return false;
        }

        return true;
    }

    private void GenerateChunks()
    {
        throw new NotImplementedException();
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
