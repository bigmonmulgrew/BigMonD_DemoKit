using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BMD.ProcGen
{
    public partial class TerrainGenerator : MonoBehaviour
    {
        #region Helper Methods
        List<ConnectionDirection> SelectDirectionPool()
        {
            // No need to check if BOTH are empty, this is done in a previous step
            if (biasDirections.Count == 0) return allowedDirections;
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
        float GetBoundsOverlap(Node nodeA, Node nodeB)
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
        bool ShouldUseRootPathPrefab(List<PathMapNode> growthSegments)
        {
            return growthSegments.Count == 0 && generatedNodes.Count == 1 && rootPathPrefabs.Length > 0;
        }
        GrowthAttempt CreateGrowthAttempt(GrowthParameters parameters, int sourceNode, int retries)
        {
            throw new NotImplementedException();

        }
        void CleanupAttempt(GrowthAttempt attempt)
        {
            throw new NotImplementedException();
        }
        IEnumerator WaitForDebugStep()
        {
            if (!stepThroughGeneration) yield break;

            while (!Input.GetKeyDown(KeyCode.Space))        // TODO need to look up if the new input system has a single line alternative.
                yield return null;
        }

        IEnumerator ThrottleProgress()
        {
            if (slowGeneration)
            {
                if (PauseGeneration) yield return null;
                else yield break;
            }
            else
            {
                while (true)
                {
                    yield return new WaitForFixedUpdate();  // Makes the frame rate fixed instead of performance related
                    if (PauseGeneration) yield break;   // When in slow generation PauseGeneration waits a number of frames rather than counting steps
                }
            }
        }
        #endregion

    }
}

