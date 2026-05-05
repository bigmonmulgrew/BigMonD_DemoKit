using BMD.DataTypes;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


namespace BMD.ProcGen
{

    public class TerrainGenerator : MonoBehaviour
    {
        public static TerrainGenerator Instance { get; private set; }
        const int LOOP_PROTECTION_LIMIT = 10;
        
        enum RoomType
        {
            Standard,
            Boss,
            BranchEnd
        }
        class GrowthParameters
        {
            public int sourceNodeID;
            public int branchNodeID = 0;
            public RoomType roomType = RoomType.Standard;
            public int growth = 0;

            public GrowthParameters(int sourceNodeID) : this(sourceNodeID, 0, RoomType.Standard) { }
            
            public GrowthParameters(int sourceNodeID, int branchNodeID, RoomType roomType)
            {
                this.sourceNodeID = sourceNodeID;
                this.branchNodeID = branchNodeID;
                this.roomType = roomType;
            }
        }

        #region Configuration
        [Header("Generation settings"), Tooltip("Length is in number of rooms, not total nodes, connecting paths will be added automatically.")]
        
        [SerializeField] IntRange roomsOnMainPath = new(5,10);
        [SerializeField] int branchesPerPath = 2;
        [SerializeField] IntRange roomsOnBranches = new(3, 5);
        [SerializeField] int GenerateStepsPerFrame = 2;     // Limit how many nodes are generated each frame to avoid performance spikes
        [Tooltip("Switches the Generation Steps Per Frame setting to be Frames per step.\n" +
            "This slows down generation considerably for debugging purposes")]
        [SerializeField] bool slowGeneration = false;
        [SerializeField] int randomSeed = 0;                // Seed for random number generation, set to 0 for a random seed based on current time

        [Tooltip("Directions the map can generate in.\n\n If no valid directions are selected, or the selected ones are not available, then any valid connection will be selected.")]
        [SerializeField] List<ConnectionDirection> allowedBranchDirections = new() { ConnectionDirection.North, ConnectionDirection.East, ConnectionDirection.South, ConnectionDirection.West };
        [Tooltip("Directions the generator will prefer when creating connections. This is not a hard requirement, just a bias.\n\n If no valid directions are selected, or the selected ones are not available, then any valid connection will be selected.")]
        [SerializeField] List<ConnectionDirection> directionalBias = new() { ConnectionDirection.North, ConnectionDirection.West };
        [Range(0,1), Tooltip("Value between 0 and 1 that determines how strong the directional bias is when selecting connections.\n\n 0 means no bias.\n1 means only select from the biased directions")]
        [SerializeField] float directionalBiasStrength = 0.5f; // Value between 0 and 1 that determines how strong the directional bias is when selecting connections. 0 means no bias, 1 means only select from the biased directions
        [SerializeField] IntRange bridgeLength = new(1,3);
        [Range(0, 1)]
        [SerializeField] float roomMaxOverlap = 0;
        [Range(0, 1)]
        [SerializeField] float pathMaxOverlap = 0.1f;

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
        Dictionary<(int, int), PathMapNode> generatedNodes = new();
        Dictionary<int, int> branchLengths = new();
        PathMapNode currentPlayerNode;   // Location of the player.
        PathMapNode currentBossNode;     // Location of the boss.
        #endregion

        #region Runtime variables
        System.Random rng;
        Coroutine generationCoroutine;
        bool isGenerating = false;
        bool generationComplete = false;
        int generationStepsThisFrame; // Counter to track how many nodes have been generated in the current frame
        #endregion

        #region Preallocations
        // These are preallocated to save assignment performance
        readonly List<ConnectionDirection> allowedDirections     = new();
        readonly List<ConnectionDirection> biasDirections        = new();
        List<ConnectionDirection>          selectedDirectionList = new();
        readonly List<Connection>          selectedConnections   = new();

        #endregion
        #region Properties
        public bool TerrainReady => !isGenerating && generationComplete;
        private bool PauseGeneration {
            get
            {
                generationStepsThisFrame++;
                if(generationStepsThisFrame >= GenerateStepsPerFrame)
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
            SetRandomSeed();
            SanityChecks();
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
        private void SetRandomSeed()
        {
            if(randomSeed == 0)
            {
                randomSeed = System.Environment.TickCount; // Use current time as seed if 0 is specified
                Debug.Log($"Random seed set to {randomSeed} based on current time.");
            }
            rng = new System.Random(randomSeed);
        }
        private void SanityChecks()
        {
            if (rootPrefabs.Length == 0) Debug.LogError("No root prefabs assigned. The generator needs at least one prefab to create the starting node.");
            if (pathNodePrefabs.Length == 0) Debug.LogError("No path node prefabs assigned. The generator needs at least one prefab to create the paths between rooms.");
            if (roomNodePrefabs.Length == 0) Debug.LogError("No room node prefabs assigned. The generator needs at least one prefab to create the rooms in the level.");
            if (endRoomPrefabs.Length == 0) Debug.LogError("No end room prefabs assigned. The generator needs at least one prefab to create the final room of the main path.");

            if (allowedBranchDirections.Count == 0)
            {
                Debug.LogWarning("No allowed branch directions selected. The generator will not be able to create branches.");
                allowedBranchDirections.AddRange(new[] { ConnectionDirection.North, ConnectionDirection.East, ConnectionDirection.South, ConnectionDirection.West });
            }

            int count = Array.FindAll(pathNodePrefabs, go => {
                PathNode node = go.GetComponent<PathNode>();
                return node != null && node.Length == bridgeLength.Min;
            }).Length;

            if (count == 0) Debug.LogError($"No Path Nodes specified with a minimum length that matches bridgeLength.Min:{bridgeLength.Min}. There must be at least one that matches the minimum");
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
            int numberOfRooms = rng.Next(roomsOnMainPath.Min, roomsOnMainPath.Max + 1);
            yield return GenerateBranch(numberOfRooms);

            // TODO add generating branches from route
            //// Generate branches
            
            isGenerating = false;
            generationComplete = true;
 
        }
        private void ClearOldTerrain()
        {
            foreach (var node in generatedNodes.Values)
            {
                node?.self.Clear();
            }
            generatedNodes.Clear();

            branchLengths.Clear();
        }
        IEnumerator GenerateBranch(int length, PathMapNode growFrom = null)
        {
            // Creates the seed point of the tree if growFrom is null and performs additional validity checks.
            if (!CheckGrowFromIsValid(growFrom)) yield break;

            int totalPathLength = 0;

            if (PauseGeneration) yield return null;         // Wait a frame to allow the root node to initialize before we start adding more nodes

            //GeneratePathConnection(ref totalPathLength);
            if (PauseGeneration) yield return null;         // Wait a frame to allow the node to initialize before we start adding more nodes

            for (int i = 0; i < length; i++)               // Iterates based on the number of rooms
            {
                GrowthParameters gp = new(i);
                yield return GrowBud(gp);
                if (gp.growth != -1) totalPathLength += gp.growth;

            }

            // Add the end room at the end of the main path
            GrowthParameters p = new(
                length,
                0,
                RoomType.Boss           // TODO add selection later for branch end
                );
            yield return GrowBud(p);
            if (p.growth != -1) totalPathLength += p.growth;

            branchLengths[0] = totalPathLength;
            yield return null; // Wait a frame to allow the end room to initialize, always wait on the last node

        }
        IEnumerator GrowBud(GrowthParameters parameters, int retries = 0)
        {
            int sourceNodeID = parameters.sourceNodeID;
            int branchID = parameters.branchNodeID;
            RoomType roomType = parameters.roomType;

            if (retries >= 6)
            {
                Debug.LogError($"GrowBud failing repeatedly, please check settings. \n" +
                    $"SourceNodeID: {sourceNodeID}, BranchID: {branchID}, retries: {retries}");
                parameters.growth = -1;
                yield break;
            }

            // Gets the most recent node on the branch with the given ID
            PathMapNode sourceNode = generatedNodes
                .Where(kvp => kvp.Key.Item1 == branchID)
                .Aggregate((max, cur) => cur.Key.Item2 > max.Key.Item2 ? cur : max)
                .Value;

            // Create a room, and initialise it.
            GameObject roomPrefab;

            switch (roomType)
            {
                case RoomType.BranchEnd:
                    roomPrefab = branchEndPrefabs[rng.Next(0, branchEndPrefabs.Length)];
                    break;
                case RoomType.Boss:
                    roomPrefab = endRoomPrefabs[rng.Next(0, endRoomPrefabs.Length)];
                    break;
                case RoomType.Standard:
                default:
                    roomPrefab = roomNodePrefabs[rng.Next(0, roomNodePrefabs.Length)];
                    break;
            }

            PathMapNode newBud = new PathMapNode
            {
                self = Instantiate(roomPrefab, transform).GetComponent<Node>(),
                PrefabName = roomPrefab.gameObject.name
            };

            newBud.self.name = $"X:X:X_{newBud.PrefabName}";

            // Choose the length of the branch growth
            int targetBranchLength = rng.Next(bridgeLength.Min, bridgeLength.Max + 1) + retries;

            // Create the branch segments
            
            List<PathMapNode> growthSegments = new();

            int totalGrowthLength() => growthSegments.Sum(n => ((PathNode)n.self).Length);  // Slower than storing but less error prone.
            bool lengthIncomplete() => totalGrowthLength() < targetBranchLength;
            int loopCounter = 0;
            
            while (lengthIncomplete() && loopCounter++ < LOOP_PROTECTION_LIMIT)
            {
                int remainingGrowth = targetBranchLength - totalGrowthLength();

                GameObject segmentPrefab;

                // Select the segment prefab
                // For first segment only, and when connecting with just a start node as a seed present
                if (growthSegments.Count == 0 && generatedNodes.Count == 1 && rootPathPrefabs.Length > 0)
                {
                    GameObject[] validPrefabs = rootPathPrefabs
                    .Where(go =>
                    {
                        PathNode pathNode = go.GetComponent<PathNode>();
                        return pathNode != null && pathNode.Length <= remainingGrowth;
                    })
                    .ToArray();

                    if (validPrefabs.Length == 0)
                    {
                        Debug.LogWarning($"No valid path prefabs found for remaining growth: {remainingGrowth}");
                        parameters.growth = -1;
                        yield break; // or retry / stop this branch
                    }
                    segmentPrefab = validPrefabs[rng.Next(0, validPrefabs.Length)];
                }
                else
                {
                    GameObject[] validPrefabs = pathNodePrefabs
                    .Where(go =>
                    {
                        PathNode pathNode = go.GetComponent<PathNode>();
                        return pathNode != null && pathNode.Length <= remainingGrowth;
                    })
                    .ToArray();

                    if (validPrefabs.Length == 0)
                    {
                        Debug.LogWarning($"No valid path prefabs found for remaining growth: {remainingGrowth}");
                        parameters.growth = -1;
                        yield break; // or retry / stop this branch
                    }
                    segmentPrefab = validPrefabs[rng.Next(0, validPrefabs.Length)];
                }

                PathMapNode segment = new PathMapNode
                {
                    self = Instantiate(segmentPrefab, transform).GetComponent<Node>(),
                    PrefabName = segmentPrefab.gameObject.name
                };

                segment.self.name = $"X:X:X_{segment.PrefabName}";
                growthSegments.Add(segment);
            }
            // Now we have generated the new bud and growth segments move the bud to the bottom in hierarchy to give a consistent order
            newBud.self.transform.SetAsLastSibling();

            if (loopCounter >= LOOP_PROTECTION_LIMIT) Debug.LogError("Branch grow loop exited after failing to create segments");

            // Next we need to lay out the nodes.
            // If no growth segments connect directly
            if (growthSegments.Count == 0) ConnectNodePair(sourceNode, newBud);
            else
            {
                bool success = false;
                success = success || ConnectNodePair(sourceNode, growthSegments[0]); // Connect source node with first growth node
                for(int i = 0; i < growthSegments.Count - 1; i++)   // Connect each growth node to each other, stop at count - 1 as the last sewgment will be connected to the new bud
                {
                    success = success && ConnectNodePair(growthSegments[i], growthSegments[i + 1]);
                }
                success = success && ConnectNodePair(growthSegments.Last(), newBud);    // Connect last growth node with the new bud
                if (!success)
                {
                    parameters.growth = -1;
                    yield return GrowBud(parameters, retries + 1);
                    yield break;
                }
            }

            // In case of any overlapping 
            // Now we check for  room overlap 
            float overlap = roomMaxOverlap;
            if (retries >= 4) overlap += 0.1f;
            if (GetBoundsOverlap(sourceNode.self, newBud.self) > overlap)
            {
                parameters.growth = -1;
                yield return GrowBud(parameters, retries + 1);
                yield break;
            }

            // Now check if paths overlap
            if (growthSegments.Count > 0)
            {
                // Check first room and first path
                if (GetBoundsOverlap(sourceNode.self, growthSegments[0].self) > pathMaxOverlap) Debug.LogWarning($"Overlapping paths detected but no handler");

                // Check each path against the next
                for (int i = 0; i < growthSegments.Count - 1; i++)
                {
                    if (GetBoundsOverlap(growthSegments[i].self, growthSegments[i + 1].self) > pathMaxOverlap) Debug.LogWarning($"Overlapping paths detected but no handler");
                }

                // Connect last path vs new room
                if (GetBoundsOverlap(growthSegments.Last().self, newBud.self) > pathMaxOverlap) Debug.LogWarning($"Overlapping paths detected but no handler");
            }

            // Now we have tested the geometry finalise the connection links
            Connection.CompleteTestLinks(sourceNode.self.Connections);
            for (int i = 0; i < growthSegments.Count; i++)   // Connect each growth node to each other
            {
                PathMapNode segment = growthSegments[i];
                Connection.CompleteTestLinks(segment.self.Connections);
                segment.self.name = $"{branchID}:{sourceNodeID + 1}:{i}_{segment.PrefabName}";
           
            }
            newBud.self.name = $"{branchID}:{sourceNodeID + 1}:{growthSegments.Count}_{newBud.PrefabName}";

            // Now update the path map with child links
            // Source to next segment first
            if (growthSegments.Count > 0)   sourceNode.AddChild(growthSegments.First());
            else                            sourceNode.AddChild(newBud);       // Add new bud directly to source if no growth segments

            for(int i = 0; i < growthSegments.Count - 1; i++)   // Dont include last member
            {
                growthSegments[i].AddChild(growthSegments[i + 1]);
            }
            if (growthSegments.Count > 0) growthSegments.Last().AddChild(newBud);

            // Finally add to the generated nodes path
            // Find the key first. This is more stable than tracking the index with random lengths and possible retries. But more expensive
            (int,int) foundKey = (0,0);

            foreach (var kvp in generatedNodes)
            {
                if (kvp.Value == sourceNode)
                {
                    foundKey = kvp.Key;
                    break;
                }
            }
            int branchIndex = foundKey.Item1;
            int pathDepthIndex = foundKey.Item2;
            int nextNodeIndex = pathDepthIndex + 1;

            // Now loop through growth segments adding to generated nodes.
            for (int i = 0; i < growthSegments.Count; i++)   // Dont include last member
            {
                generatedNodes[(branchIndex, nextNodeIndex)] = growthSegments[i];
                nextNodeIndex++;
                parameters.growth++;    // Also increment growth
            }
            generatedNodes[(branchIndex, nextNodeIndex)] = newBud;  // Add the new bud last
            parameters.growth++;
        }
        bool CheckGrowFromIsValid(PathMapNode growFrom)
        {
            if (growFrom == null && generatedNodes.Count > 0)
            {
                Debug.LogError($"A terrain origin has already been seeded, you  must specify a node to growFrom. Randomly selecting a node to grow from is not yet supported.");
                return false;
            }

            if (growFrom == null) growFrom = SeedOriginPoint();
            
            if (growFrom != null)
            {
                // Check if growFrom has type "Node"
                if (growFrom.self is not RoomNode)
                {
                    Debug.Log($"growFrom specified but contained object of {growFrom.self.name} is not a RoomNode. Growing from other node types is not yet supported.");
                    return false;
                }
            }

            return true;
        }
        PathMapNode SeedOriginPoint()
        {
            GameObject prefab = rootPrefabs[rng.Next(0, rootPrefabs.Length)];
            PathMapNode pathNode = new PathMapNode
            {
                self = Instantiate(prefab, transform).GetComponent<Node>(),
                PrefabName = prefab.gameObject.name

            };

            pathNode.self.name = $"0:0:0_{pathNode.self.name}";

            // Create the root node
            generatedNodes[(0, 0)] = pathNode;
            branchLengths[0] = 1;   // Store the length of the main path in the branch lengths dictionary with branch index 0 representing the main path. Add one for start and one for end

            return generatedNodes[(0, 0)];
        }
        bool ConnectNodePair(PathMapNode firstNode, PathMapNode secondNode)
        {
            // These are preallocated and cleared.
            // This is faster since we create and remove these many times during terrain generation
            allowedDirections.Clear();
            biasDirections.Clear();

            allowedDirections.AddRange(allowedBranchDirections);
            biasDirections.AddRange(directionalBias);

            ConnectionDirection selectedDirection;
            ConnectionDirection reverseDirection;

            while (allowedDirections.Count + biasDirections.Count > 0)
            {
                selectedDirectionList = SelectDirectionPool();

                selectedDirection = selectedDirectionList[rng.Next(selectedDirectionList.Count)];
                reverseDirection = Reverse(selectedDirection);

                Connection firstConnection = GetRandomConnection(firstNode.self, selectedDirection);

                if (firstConnection == null)
                {
                    selectedDirectionList.Remove(selectedDirection);
                    continue;
                }

                Connection secondConnection = FindConnectionWithRotation(secondNode.self, reverseDirection);

                if (secondConnection == null)
                {
                    selectedDirectionList.Remove(selectedDirection);
                    continue;
                }

                Connection.TestLink(firstConnection, secondConnection);
                
                return true;
            }

            return false;
        }
        public void SetPlayerLocation(PathMapNode node)
        {
            currentPlayerNode = node;
        }
        public void SetBossLocation(PathMapNode node)
        {
            currentBossNode = node;
        }
        #region Helper Methods
        List<ConnectionDirection> SelectDirectionPool()
        {
            // No need to check if BOTH are empty, this is done in a previous step
            if (biasDirections.Count == 0)    return allowedDirections;
            if (allowedDirections.Count == 0) return biasDirections;

            return rng.NextDouble() <= directionalBiasStrength ? biasDirections : allowedDirections;
        }
        ConnectionDirection Reverse(ConnectionDirection direction)
        {
            return direction switch
            {
                ConnectionDirection.North => ConnectionDirection.South,
                ConnectionDirection.South => ConnectionDirection.North,
                ConnectionDirection.East => ConnectionDirection.West,
                ConnectionDirection.West => ConnectionDirection.East,
                _ => throw new ArgumentOutOfRangeException(nameof(direction), direction, null)
            };
        }
        Connection GetRandomConnection(Node node, ConnectionDirection direction)
        {
            selectedConnections.Clear();
            selectedConnections.AddRange(node.GetConnectionsByDirection(direction));

            return selectedConnections.Count == 0
                ? null
                : selectedConnections[rng.Next(selectedConnections.Count)];
        }
        Connection FindConnectionWithRotation(Node node, ConnectionDirection direction)
        {
            for (int i = 0; i < 4; i++)
            {
                // TODO check if rotation is valid and skip if not
                Connection connection = GetRandomConnection(node, direction);

                // End when we have found a valid rotation
                if (connection != null) return connection;

                node.Rotate();
            }

            node.ResetRotation();

            return null;
        }
        float GetBoundsOverlap(Node nodeA,  Node nodeB)
        {

            Bounds boundsA = new Bounds(
                nodeA.transform.TransformPoint(nodeA.Bounds.center),    // Convert local coordinates for centre to global coordinates
                nodeA.Bounds.size                                       // Size remains the same
                );
            Bounds boundsB = new Bounds(
                nodeB.transform.TransformPoint(nodeB.Bounds.center),    // Convert local coordinates for centre to global coordinates
                nodeB.Bounds.size                                       // Size remains the same
                );

            float overlapPercent = 0f;

            // If either box is fully inside the other return overlap of 1
            bool aInB = boundsB.Contains(boundsA.min) && boundsB.Contains(boundsA.max);
            bool bInA = boundsA.Contains(boundsB.min) && boundsA.Contains(boundsB.max);
            if (aInB || bInA) return 1.0f;


            if (boundsA.Intersects(boundsB))
            {
                Vector3 min = Vector3.Max(boundsA.min, boundsB.min);
                Vector3 max = Vector3.Min(boundsA.max, boundsB.max);

                Vector3 size = max - min;

                float intersectionVolume = size.x * size.y * size.z;
                float volumeA = boundsA.size.x * boundsA.size.y * boundsA.size.z;

                overlapPercent = intersectionVolume / volumeA; // 0–1 range
            }

            return overlapPercent;
        }
        #endregion
    }
}