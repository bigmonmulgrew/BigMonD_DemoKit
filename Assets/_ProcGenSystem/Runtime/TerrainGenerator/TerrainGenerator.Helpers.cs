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
        /// <summary>
        /// Manages performance throttling, optinally specify a small step to bypass frame based 
        /// throttling until the next step, this is for cases where you want to slow down generation 
        /// but not every single step.
        /// </summary>
        /// <param name="smallStep">If true, bypasses frame based throttling until the next step.</param>
        /// <returns></returns>
        object GetThrottleYield(bool smallStep = false)
        {
            if (stepThroughGeneration) return WaitForDebugStep();

            if (slowGeneration)
            {
                if (smallStep) return null; 
                return ThrottleByFrames(); 
            }
            
            generationStepsThisFrame++;
            if (generationStepsThisFrame >= GenerationThrottleAmount)
            {
                generationStepsThisFrame = 0;
                return ThrottleBySteps();
            }

            // Defaults to null.
            return null;
        }
        /// <summary>
        /// Pasues generation until the user presses the space bar, 
        /// allowing step by step debugging of the generation process.
        /// </summary>
        /// <returns></returns>
        IEnumerator WaitForDebugStep()
        {
            while (!Input.GetKeyDown(KeyCode.Space))        // TODO need to look up if the new input system has a single line alternative.
                yield return null;

        }

        /// <summary>
        /// Throttles based on generation steps, allowing a certain number of steps per frame. 
        /// This is the default throttling method.
        /// </summary>
        /// <returns></returns>
        IEnumerator ThrottleBySteps()
        {
            yield return null; 
        }

        /// <summary>
        ///  Waits a certain number of frames before allowing the next generation step, 
        ///  effectively slowing down the generation process.
        /// </summary>
        /// <returns></returns>
        IEnumerator ThrottleByFrames()
        {
            for(int i = 0; i < GenerationThrottleAmount; i++)
            {
                yield return new WaitForFixedUpdate();
            }
        }
        
        #endregion

    }
}

