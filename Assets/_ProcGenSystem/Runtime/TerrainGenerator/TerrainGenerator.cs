using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


namespace BMD.ProcGen
{

    public partial class TerrainGenerator : MonoBehaviour
    {
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

            yield return ThrottleProgress();    // Applies performance throttling

            for (int i = 0; i < length; i++)               // Iterates based on the number of rooms
            {
                GrowthParameters gp = new(i);
                yield return GrowBud(gp);
                if (gp.growth != -1) totalPathLength += gp.growth;

                yield return ThrottleProgress();    // Applies performance throttling
            }

            // Add the end room at the end of the main path
            GrowthParameters p = new(
                length,
                0,
                RoomType.Boss           // TODO add selection later for branch end
                );
            yield return GrowBud(p);
            yield return ThrottleProgress();    // Applies performance throttling
            if (p.growth != -1) totalPathLength += p.growth;

            branchLengths[0] = totalPathLength;
            yield return null; // Wait a frame to allow the end room to initialize, always wait on the last node

        }
        IEnumerator GrowBud(GrowthParameters parameters, int retries = 0)
        {
            int sourceNodeID = parameters.sourceNodeID;
            int branchID = parameters.branchID;
            RoomType roomType = parameters.roomType;

            //GrowthAttempt attempt = CreateGrowthAttempt(parameters, sourceNodeID, retries);

            if (retries >= 6)
            {
                Debug.LogError($"GrowBud failing repeatedly, please check settings. \n" +
                    $"SourceNodeID: {sourceNodeID}, BranchID: {branchID}, retries: {retries}");
                parameters.growth = -1;
                yield break;
            }

            // Gets the most recent node on the branch with the given ID
            PathMapNode sourceNode = generatedNodes
                .Where(kvp => kvp.Key.Branch == branchID)
                .Aggregate((max, cur) => cur.Key.Depth > max.Key.Depth ? cur : max)
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

                GameObject[] prefabPool = ShouldUseRootPathPrefab(growthSegments) 
                    ? rootPathPrefabs 
                    : pathNodePrefabs;

                if (!TrySelectPathPrefab(prefabPool, remainingGrowth, out GameObject segmentPrefab))
                {
                    //CleanupAttempt(attempt);
                    yield break;
                }

                PathMapNode segment = new PathMapNode
                {
                    self = Instantiate(segmentPrefab, transform).GetComponent<Node>(),
                    PrefabName = segmentPrefab.gameObject.name
                };

                segment.self.name = $"X:X:X_{segment.PrefabName}";
                growthSegments.Add(segment);
                yield return ThrottleProgress();    // Applies performance throttling
            }
            // Now we have generated the new bud and growth segments move the bud to the bottom in hierarchy to give a consistent order
            newBud.self.transform.SetAsLastSibling();

            if (loopCounter >= LOOP_PROTECTION_LIMIT) Debug.LogError("Branch grow loop exited after failing to create segments");

            // Next we need to lay out the nodes.
            // If no growth segments connect directly
            if (growthSegments.Count == 0) TryCreateTestConnection(sourceNode, newBud);
            else
            {
                bool success = false;
                success = success || TryCreateTestConnection(sourceNode, growthSegments[0]); // Connect source node with first growth node
                for(int i = 0; i < growthSegments.Count - 1; i++)   // Connect each growth node to each other, stop at count - 1 as the last sewgment will be connected to the new bud
                {
                    success = success && TryCreateTestConnection(growthSegments[i], growthSegments[i + 1]);
                }
                success = success && TryCreateTestConnection(growthSegments.Last(), newBud);    // Connect last growth node with the new bud
                if (!success)
                {
                    parameters.growth = -1;
                    yield return GrowBud(parameters, retries + 1);
                    yield break;
                }
            }
            yield return ThrottleProgress();    // Applies performance throttling


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

                yield return ThrottleProgress();    // Applies performance throttling

                // Check each path against the next
                for (int i = 0; i < growthSegments.Count - 1; i++)
                {
                    if (GetBoundsOverlap(growthSegments[i].self, growthSegments[i + 1].self) > pathMaxOverlap) Debug.LogWarning($"Overlapping paths detected but no handler");

                    yield return ThrottleProgress();    // Applies performance throttling
                }

                // Connect last path vs new room
                if (GetBoundsOverlap(growthSegments.Last().self, newBud.self) > pathMaxOverlap) Debug.LogWarning($"Overlapping paths detected but no handler");

                yield return ThrottleProgress();    // Applies performance throttling
            }

            // Now we have tested the geometry finalise the connection links
            Connection.CompleteTestLinks(sourceNode.self.Connections);
            for (int i = 0; i < growthSegments.Count; i++)   // Connect each growth node to each other
            {
                PathMapNode segment = growthSegments[i];
                Connection.CompleteTestLinks(segment.self.Connections);
                segment.self.name = $"{branchID}:{sourceNodeID + 1}:{i}_{segment.PrefabName}";

                yield return ThrottleProgress();    // Applies performance throttling
            }
            newBud.self.name = $"{branchID}:{sourceNodeID + 1}:{growthSegments.Count}_{newBud.PrefabName}";

            yield return ThrottleProgress();    // Applies performance throttling

            // Now update the path map with child links
            // Source to next segment first
            if (growthSegments.Count > 0)   sourceNode.AddChild(growthSegments.First());
            else                            sourceNode.AddChild(newBud);       // Add new bud directly to source if no growth segments

            yield return ThrottleProgress();    // Applies performance throttling

            for (int i = 0; i < growthSegments.Count - 1; i++)   // Dont include last member
            {
                growthSegments[i].AddChild(growthSegments[i + 1]);
                yield return ThrottleProgress();    // Applies performance throttling
            }
            if (growthSegments.Count > 0) growthSegments.Last().AddChild(newBud);
            yield return ThrottleProgress();    // Applies performance throttling

            // Finally add to the generated nodes path
            // Find the key first. This is more stable than tracking the index with random lengths and possible retries. But more expensive
            NodeAddress foundKey = new(0,0);

            foreach (var kvp in generatedNodes)
            {
                if (kvp.Value == sourceNode)
                {
                    foundKey = kvp.Key;
                    break;
                }
                yield return ThrottleProgress();    // Applies performance throttling
            }
            int branchIndex = foundKey.Branch;
            int pathDepthIndex = foundKey.Depth;
            int nextNodeIndex = pathDepthIndex + 1;

            // Now loop through growth segments adding to generated nodes.
            for (int i = 0; i < growthSegments.Count; i++)   // Dont include last member
            {
                generatedNodes[new(branchIndex, nextNodeIndex)] = growthSegments[i];
                nextNodeIndex++;
                parameters.growth++;    // Also increment growth
                yield return ThrottleProgress();    // Applies performance throttling
            }
            generatedNodes[new(branchIndex, nextNodeIndex)] = newBud;  // Add the new bud last
            parameters.growth++;

            yield return ThrottleProgress();    // Applies performance throttling
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
            generatedNodes[new(0, 0)] = pathNode;
            branchLengths[0] = 1;   // Store the length of the main path in the branch lengths dictionary with branch index 0 representing the main path. Add one for start and one for end

            return generatedNodes[new(0, 0)];
        }
        bool TryCreateTestConnection(PathMapNode firstNode, PathMapNode secondNode)
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
        bool TrySelectPathPrefab(GameObject[] prefabs, int maxLength, out GameObject selectedPrefab)
        {
            List<GameObject> validPrefabs = new();

            foreach (GameObject prefab in prefabs)
            {
                if (prefab.TryGetComponent(out PathNode pathNode) && pathNode.Length <= maxLength)
                {
                    validPrefabs.Add(prefab);
                }
            }

            if (validPrefabs.Count == 0)
            {
                selectedPrefab = null;
                return false;
            }

            selectedPrefab = validPrefabs[rng.Next(validPrefabs.Count)];
            return true;
        }
        
    }
}