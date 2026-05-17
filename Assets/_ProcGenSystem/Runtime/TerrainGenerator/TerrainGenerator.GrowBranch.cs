using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using UnityEngine;
using static PlasticGui.GetProcessName;

namespace BMD.ProcGen
{
    public partial class TerrainGenerator : MonoBehaviour
    {
        bool TryCreateGrowthAttempt(GrowthParameters parameters, int retries, out GrowthAttempt attempt)
        {
            if (retries > MAX_GROWTH_RETRIES)
            {
                Debug.LogError($"GrowBud failing repeatedly, please check settings. \n" +
                    $"SourceNodeID: {parameters.sourceNodeID}, " +
                    $"BranchID: {parameters.branchID}, " +
                    $"retries: {retries}");
                attempt = null;
                return false;
            }

            if (!TryGetLastNodeOnBranch(parameters.branchID, out PathMapNode sourceNode))
            {
                Debug.LogError($"Unable to find last node on branch. \n" +
                    $"SourceNodeID: {parameters.sourceNodeID}, " +
                    $"BranchID: {parameters.branchID}");
                attempt = null;
                return false;
            }

            attempt = new() { Parameters = parameters };

            attempt.SourceNode = sourceNode;

            // Create a room, and initialise it.
            GameObject roomPrefab;

            switch (attempt.RoomType)
            {
                case RoomType.BranchEnd:
                    roomPrefab = branchEndPrefabs[rng.Next(0, branchEndPrefabs.Length)];
                    break;
                case RoomType.Boss:
                    roomPrefab = endRoomPrefabs.Length > 0 ?
                        endRoomPrefabs[rng.Next(0, endRoomPrefabs.Length)] :
                        roomNodePrefabs[rng.Next(0, roomNodePrefabs.Length)];
                    break;
                case RoomType.Standard:
                default:
                    roomPrefab = roomNodePrefabs[rng.Next(0, roomNodePrefabs.Length)];
                    break;
            }

            PathMapNode newBud = new PathMapNode
            {
                self = Instantiate(roomPrefab, transform).GetComponent<Node>(),
                PrefabName = roomPrefab.name
            };

            newBud.self.name = $"X:X:X_{newBud.PrefabName}";

            attempt.NewBud = newBud;

            attempt.TargetLength = rng.Next(bridgeLength.Min, bridgeLength.Max + 1) + retries + (attempt.NewBud == null ? 0 : 1);      // We add the retries with the assumption that extra space will create a higher success chance

            return true;
        }

        bool TryGetLastNodeOnBranch(int branchID, out PathMapNode sourceNode)
        {
            // Gets the most recent node on the branch with the given ID

            sourceNode = null;
            int highestDepth = int.MinValue;

            foreach (var kvp in generatedNodes)
            {
                if (kvp.Key.Branch != branchID) continue;

                if (kvp.Key.Depth > highestDepth)
                {
                    highestDepth = kvp.Key.Depth;
                    sourceNode = kvp.Value;
                }
            }

            return sourceNode != null;
        }
        bool TrySelectPathPrefab(GrowthAttempt attempt, out GameObject selectedPrefab)
        {
            validPathPrefabs.Clear();

            GameObject[] prefabPool = ShouldUseRootPathPrefab(attempt.Segments)
                    ? rootPathPrefabs
                    : pathNodePrefabs;

            foreach (GameObject prefab in prefabPool)
            {
                if (prefab.TryGetComponent(out PathNode pathNode) && pathNode.Length <= attempt.RemainingGrowth)
                {
                    validPathPrefabs.Add(prefab);
                }
            }

            if (validPathPrefabs.Count == 0)
            {
                selectedPrefab = null;
                return false;
            }

            selectedPrefab = validPathPrefabs[rng.Next(validPathPrefabs.Count)];
            return true;
        }

        IEnumerator TryBuildGrowthSegments(GrowthAttempt attempt)
        {
            int loopCounter = 0;

            while (!attempt.GrowthComplete && loopCounter++ < LOOP_PROTECTION_LIMIT)
            {

                GameObject[] prefabPool = ShouldUseRootPathPrefab(attempt.Segments)
                    ? rootPathPrefabs
                    : pathNodePrefabs;

                if (!TrySelectPathPrefab(attempt, out GameObject segmentPrefab))
                {
                    if (SetThrottleYield()) yield return Throttle;
                    CleanupAttempt(attempt);
                    Debug.LogError($"Branch grow loop exited after failing to create segments \n" +
                        $"SourceNodeID: {attempt.Parameters.sourceNodeID}, " +
                        $"BranchID: {attempt.Parameters.branchID}, ");
                    yield break;
                }

                PathMapNode segment = new PathMapNode
                {
                    self = Instantiate(segmentPrefab, transform).GetComponent<Node>(),
                    PrefabName = segmentPrefab.gameObject.name
                };

                segment.self.name = $"X:X:X_{segment.PrefabName}";
                attempt.Segments.Add(segment);

                if (SetThrottleYield()) yield return Throttle;
            }
            // Now we have generated the new bud and growth segments move the bud to the bottom in hierarchy to give a consistent order
            attempt.NewBud.self.transform.SetAsLastSibling();

            if (loopCounter >= LOOP_PROTECTION_LIMIT)
            {
                Debug.LogError($"Loop protection limit reached, Branch grow loop exited after failing to create segments \n" +
                    $"SourceNodeID: {attempt.Parameters.sourceNodeID}, " +
                    $"BranchID: {attempt.Parameters.branchID}, ");
                yield break;
            }
            attempt.BuildSucceeded = true;
        }

        bool TryConnectGrowth(GrowthAttempt attempt)
        {
            // If no growth segments connect directly
            if (attempt.Segments.Count == 0)
            {
                if (!TryCreateTestConnection(attempt.SourceNode, attempt.NewBud)) return false;
                return true;    // Direct connection successful
            }
            
            // Else
            // Connect source node with first growth node
            bool success = TryCreateTestConnection(attempt.SourceNode, attempt.Segments[0]); 

            // Connect each growth node to each other, stop at count - 1 as the last segment will be connected to the new bud
            for (int i = 0; i < attempt.Segments.Count - 1; i++)   
            {
                success = success && TryCreateTestConnection(attempt.Segments[i], attempt.Segments[i + 1]);
            }

            // Connect last growth node with the new bud
            success = success && TryCreateTestConnection(attempt.Segments.Last(), attempt.NewBud);    
            if (!success) return false;
            
            return true;
        }
    }

}
