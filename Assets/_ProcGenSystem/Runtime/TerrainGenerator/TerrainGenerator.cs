using System.Collections;
using System.Linq;
using UnityEngine;


namespace BMD.ProcGen
{

    public partial class TerrainGenerator : MonoBehaviour
    {
        void Update()
        {
            // For manual step through, sometimes two throttles are called on the same frame This protects against that.
            debugStepDoneThisFrame = false;
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

            if (SetThrottleYield()) yield return Throttle;

            for (int i = 0; i <= length; i++)               // Iterates based on the number of rooms
            {
                GrowthParameters gp = new(i);

                if(i == length) gp.roomType = RoomType.Boss;   // Set the last room to be the boss room

                yield return GrowBud(gp);
                if (!gp.success) Debug.Log("Failed to grow bud for main path, skipping. This may cause issues with future growth.");
                else totalPathLength += gp.growth;

                if (SetThrottleYield()) yield return Throttle;
            }

            branchLengths[0] = totalPathLength;

            if (SetThrottleYield()) yield return Throttle;

        }
        IEnumerator GrowBud(GrowthParameters parameters, int retries = 0)
        {

            //IEnumerator GrowBud(GrowthParameters parameters, int retries = 0)
            //{
            //    if (!TryCreateGrowthAttempt(parameters, retries, out GrowthAttempt attempt))
            //        yield break;

            //    if (!TryBuildGrowthSegments(attempt))
            //    {
            //        CleanupAttempt(attempt);
            //        yield break;
            //    }

            //    if (!TryConnectGrowth(attempt))
            //    {
            //        CleanupAttempt(attempt);
            //        yield return GrowBud(parameters, retries + 1);
            //        yield break;
            //    }

            //    if (!IsGrowthValid(attempt, retries))
            //    {
            //        CleanupAttempt(attempt);
            //        yield return GrowBud(parameters, retries + 1);
            //        yield break;
            //    }

            //    FinaliseGrowth(attempt);
            //}

            parameters.growth = 0;    // Reset growth for this attempt

            // This fails if retries is too high
            if (!TryCreateGrowthAttempt(parameters, retries, out GrowthAttempt attempt)) yield break;
            if (SetThrottleYield()) yield return Throttle;

            yield return TryBuildGrowthSegments(attempt);
            if (!attempt.BuildSucceeded) yield break;
            if (SetThrottleYield()) yield return Throttle;

            // Next we need to lay out the nodes.
            // If no growth segments connect directly
            if (attempt.Segments.Count == 0) 
            { 
                if (TryCreateTestConnection(attempt.SourceNode, attempt.NewBud))
                {
                    if (SetThrottleYield()) yield return Throttle;
                    CleanupAttempt(attempt);
                    yield return GrowBud(parameters, retries + 1);
                    yield break;
                } 
            }
            else
            {
                bool success = TryCreateTestConnection(attempt.SourceNode, attempt.Segments[0]); // Connect source node with first growth node
                for(int i = 0; i < attempt.Segments.Count - 1; i++)   // Connect each growth node to each other, stop at count - 1 as the last sewgment will be connected to the new bud
                {
                    success = success && TryCreateTestConnection(attempt.Segments[i], attempt.Segments[i + 1]);
                }
                success = success && TryCreateTestConnection(attempt.Segments.Last(), attempt.NewBud);    // Connect last growth node with the new bud
                if (!success)
                {
                    if (SetThrottleYield()) yield return Throttle;
                    CleanupAttempt(attempt);
                    yield return GrowBud(parameters, retries + 1);
                    yield break;
                }
            }
            
            if (SetThrottleYield()) yield return Throttle;


            // In case of any overlapping 
            // Now we check for  room overlap 
            float overlap = roomMaxOverlap;
            if (retries >= 4) overlap += 0.1f;
            if (GetBoundsOverlap(attempt.SourceNode.self, attempt.NewBud.self) > overlap)
            {
                if (SetThrottleYield()) yield return Throttle;
                CleanupAttempt(attempt);
                yield return GrowBud(parameters, retries + 1);
                yield break;
            }

            // Now check if paths overlap
            if (attempt.Segments.Count > 0)
            {
                // Check first room and first path
                if (GetBoundsOverlap(attempt.SourceNode.self, attempt.Segments[0].self) > pathMaxOverlap) Debug.LogWarning($"Overlapping paths detected but no handler");

                if (SetThrottleYield()) yield return Throttle;

                // Check each path against the next
                for (int i = 0; i < attempt.Segments.Count - 1; i++)
                {
                    if (GetBoundsOverlap(attempt.Segments[i].self, attempt.Segments[i + 1].self) > pathMaxOverlap) Debug.LogWarning($"Overlapping paths detected but no handler");

                    if (SetThrottleYield()) yield return Throttle;
                }

                // Connect last path vs new room
                if (GetBoundsOverlap(attempt.Segments.Last().self, attempt.NewBud.self) > pathMaxOverlap) Debug.LogWarning($"Overlapping paths detected but no handler");

                if (SetThrottleYield()) yield return Throttle;
            }

            // Now we have tested the geometry finalise the connection links
            Connection.CompleteTestLinks(attempt.SourceNode.self.Connections);
            for (int i = 0; i < attempt.Segments.Count; i++)   // Connect each growth node to each other
            {
                PathMapNode segment = attempt.Segments[i];
                Connection.CompleteTestLinks(segment.self.Connections);
                segment.self.name = $"{attempt.BranchID}:{attempt.SourceNodeID + 1}:{i}_{segment.PrefabName}";

                if (SetThrottleYield()) yield return Throttle;
            }
            attempt.NewBud.self.name = 
                $"{attempt.BranchID}:{attempt.SourceNodeID + 1}:{attempt.Segments.Count}" +
                $"_{attempt.NewBud.PrefabName}";

            if (SetThrottleYield()) yield return Throttle;

            // Now update the path map with child links
            // Source to next segment first
            if (attempt.Segments.Count > 0) attempt.SourceNode.AddChild(attempt.Segments.First());
            else                            attempt.SourceNode.AddChild(attempt.NewBud);       // Add new bud directly to source if no growth segments

                

            for (int i = 0; i < attempt.Segments.Count - 1; i++)   // Dont include last member
            {
                attempt.Segments[i].AddChild(attempt.Segments[i + 1]);
                if (SetThrottleYield()) yield return Throttle;
            }
            if (attempt.Segments.Count > 0) attempt.Segments.Last().AddChild(attempt.NewBud);
            
            if (SetThrottleYield()) yield return Throttle;

            // Finally add to the generated nodes path
            // Find the key first. This is more stable than tracking the index with random lengths and possible retries. But more expensive
            NodeAddress foundKey = new(0,0);

            foreach (var kvp in generatedNodes)
            {
                if (kvp.Value == attempt.SourceNode)
                {
                    foundKey = kvp.Key;
                    break;
                }
                if (SetThrottleYield()) yield return Throttle;
            }
            int branchIndex = foundKey.Branch;
            int pathDepthIndex = foundKey.Depth;
            int nextNodeIndex = pathDepthIndex + 1;

            // Now loop through growth segments adding to generated nodes.
            for (int i = 0; i < attempt.Segments.Count; i++)   // Dont include last member
            {
                generatedNodes[new(branchIndex, nextNodeIndex)] = attempt.Segments[i];
                nextNodeIndex++;

                if (SetThrottleYield()) yield return Throttle;
            }
            generatedNodes[new(branchIndex, nextNodeIndex)] = attempt.NewBud;  // Add the new bud last

            parameters.growth = attempt.TotalGrowth;    // Update growth with the total growth from the attempt.
            parameters.success = true;

            if (SetThrottleYield()) yield return Throttle;
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
        
        
    }
}