using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;

namespace BMD.ProcGen
{

    public class TerrainGenerator : MonoBehaviour
    {
        public static TerrainGenerator Instance { get; private set; }

        #region Configuration
        [Header("Generation settings"), Tooltip("Length is in number of rooms, not total nodes, connecting paths will be added automatically.")]
        [SerializeField, Min(1)] int minPathLength = 5;
        [SerializeField, Min(2)] int maxPathLength = 10;
        [SerializeField] int branchesPerPath = 2;
        [SerializeField, Min(1)] int minBranchLength = 3;
        [SerializeField, Min(2)] int maxBranchLength = 5;
        [SerializeField] int GenerateNodesPerFrame = 2; // Limit how many nodes are generated each frame to avoid performance spikes

        [Header("Node prefabs")]
        [Tooltip("The starting node of the game path. This is where the player will spawn")]
        [SerializeField] GameObject[] rootPrefabs;
        [Tooltip("Path variants used just to exit the root.\n\n If this is empty a normal path piece will be used.")]
        [SerializeField] GameObject[] rootPathPrefabs;
        [Tooltip("Path pieces used to connect each room node")]
        [SerializeField] GameObject[] pathNodePrefabs;
        [Tooltip("Room nodes used to build the level")]
        [SerializeField] GameObject[] roomNodePrefabs;
        [Tooltip("The final node of the main path. This is where the boss will be located")]
        [SerializeField] GameObject[] endRoomPrefabs;
        [Tooltip("Branch end nodes. These are used to end the branches that come out of the main path.\n\n If this is empty a normal end room will be used.")]
        [SerializeField] GameObject[] branchEndPrefabs;
        #endregion

        #region References
        // Key: (x, y) coordinates of the node, x = branch index, y = depth level in the path
        Dictionary<(int, int), PathNode> generatedNodes = new();
        PathNode currentPlayerNode;   // Location of the player.
        PathNode currentBossNode;     // Location of the boss.
        #endregion

        #region Runtime variables
        Coroutine generationCoroutine;
        bool isGenerating = false;
        bool generationComplete = false;
        int generationStepsThisFrame; // Counter to track how many nodes have been generated in the current frame
        #endregion
        #region Properties
        public bool TerrainReady => !isGenerating && generationComplete;
        private bool PauseGeneration {
            get
            {
                generationStepsThisFrame++;
                if(generationStepsThisFrame >= GenerateNodesPerFrame)
                {
                    generationStepsThisFrame = 0; // Reset the counter for the next frame
                    return true; // Pause generation to wait for the next frame
                }
                return false; // Continue generation in the current frame
            } 
        }
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
        private void Start()
        {
            generationCoroutine = StartCoroutine(GenerateLevel());
        }

        IEnumerator GenerateLevel()
        {
            if (isGenerating)
            {
                Debug.LogWarning("Terrain generation is already in progress.");
                yield break;
            }

            isGenerating = true;
            
            ClearOldTerrain();


            // Generate the main path
            int mainPathLength = Random.Range(minPathLength, maxPathLength + 1);
            int lengthIncludingConnections = mainPathLength * 2 - 1; // Each room is connected by a path, so total nodes = rooms + paths = 2*rooms - 1
            yield return GenerateMainPath(lengthIncludingConnections);

            //// Generate branches
            //for (int branchIndex = 0; branchIndex < branchesPerPath; branchIndex++)
            //{
            //    int branchLength = Random.Range(minBranchLength, maxBranchLength + 1);
            //    GenerateBranch(branchIndex, branchLength);
            //}

            isGenerating = false;
            generationComplete = true;

            // Print generated nodes for debugging
            foreach (var kvp in generatedNodes)
            {
                Debug.Log($"Node at {kvp.Key}: {kvp.Value.self.name}");
            }

        }
        private void ClearOldTerrain()
        {
            foreach (var node in generatedNodes.Values)
            {
                if (node != null)
                {
                    node.self.Clear();
                }
            }
            generatedNodes.Clear();
        }

        IEnumerator GenerateMainPath(int length)
        {
            // Create the root node
            generatedNodes[(0,0)] = CreateNode(rootPrefabs);
            if(PauseGeneration) yield return null; // Wait a frame to allow the root node to initialize before we start adding more nodes

            // Add the first path node right after the root. This ensures we have a clear exit from the starting area.
            generatedNodes[(0, 1)] = CreateNode(rootPathPrefabs.Length > 0 ? rootPathPrefabs : pathNodePrefabs);
            if(PauseGeneration) yield return null; // Wait a frame to allow the first path node to initialize before placing the next node


            for (int i = 2; i < length; i++)
            {
                // check if i is even or odd to determine if we are placing a path node or a room node
                GameObject[] prefabsToUse = (i % 2 == 0) ? pathNodePrefabs : roomNodePrefabs;
                generatedNodes[(0, i)] = CreateNode(prefabsToUse, generatedNodes[(0, i - 1)]);
                if(PauseGeneration) yield return null; // Wait a frame after placing each node to allow it to initialize before placing the next one
            }

            // Add the end room at the end of the main path
            generatedNodes[(0, length)] = CreateNode(endRoomPrefabs, generatedNodes[(0, length - 1)]);
            yield return null; // Wait a frame to allow the end room to initialize, always wait on the last node

        }
        PathNode CreateNode(GameObject[] nodes, PathNode parent = null)
        {
            GameObject prefab = nodes[Random.Range(0, nodes.Length)];
            PathNode pathNode = new PathNode
            {
                self = Instantiate(prefab, transform).GetComponent<Node>()
            };

            if (parent != null) parent.AddChild(pathNode);

            return pathNode;
        }
        
        public void SetPlayerLocation(PathNode node)
        {
            currentPlayerNode = node;
        }
        public void SetBossLocation(PathNode node)
        {
            currentBossNode = node;
        }
    }
}