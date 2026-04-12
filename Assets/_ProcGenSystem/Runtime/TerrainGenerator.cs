using System.Collections.Generic;
using UnityEngine;

public class TerrainGenerator : MonoBehaviour
{
    public static TerrainGenerator Instance { get; private set; }

    #region Configuration
    [SerializeField, Min(1)] int minPathLength = 5;
    [SerializeField, Min(2)] int maxPathLength = 10;
    [SerializeField] int branchesPerPath = 2;
    [SerializeField, Min(1)] int minBranchLength = 3;
    [SerializeField, Min(2)] int maxBranchLength = 5;

    [SerializeField] GameObject[] pathPrefabs;
    [SerializeField] GameObject[] nodePrefabs;
    [SerializeField] GameObject[] rootPrefabs;
    [SerializeField] GameObject[] endPrefabs;
    [SerializeField] GameObject[] branchEndPrefabs;
    #endregion

    #region References
    // Key: (x, y) coordinates of the node, x = branch index, y = depth level in the path
    Dictionary<(int, int), PathNode> generatedNodes = new();
    PathNode currentNode;   // Location of the player, used for saving.
    #endregion
    #region Runtime variables
    bool isGenerating = false;
    #endregion
    #region Properties
    public bool TerrainReady => !isGenerating;
    #endregion
    private void Awake()
    {
        CreateInstance();
        
    }
    
    private void CreateInstance()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        
    }
    public void SetPlayerLocation(PathNode node)
    {
        currentNode = node;
    }
}
