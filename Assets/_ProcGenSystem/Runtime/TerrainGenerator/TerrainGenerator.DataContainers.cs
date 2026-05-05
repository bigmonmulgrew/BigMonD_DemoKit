using System.Collections.Generic;
using UnityEngine;

namespace BMD.ProcGen
{
    public partial class TerrainGenerator : MonoBehaviour
    {
        enum RoomType
        {
            Standard,
            Boss,
            BranchEnd
        }
        class GrowthParameters
        {
            public int sourceNodeID;
            public int branchID = 0;
            public RoomType roomType = RoomType.Standard;
            public int growth = 0;

            public GrowthParameters(int sourceNodeID) : this(sourceNodeID, 0, RoomType.Standard) { }

            public GrowthParameters(int sourceNodeID, int branchID, RoomType roomType)
            {
                this.sourceNodeID = sourceNodeID;
                this.branchID = branchID;
                this.roomType = roomType;
            }
        }

        class GrowthAttempt
        {
            public GrowthParameters Parameters;
            public PathMapNode SourceNode;
            public PathMapNode NewBud;
            public List<PathMapNode> Segments = new();
            public int TargetLength;
            public int BranchID { 
                get { return Parameters.branchID; } 
                set { Parameters.branchID = value; } 
            }
            public int SourceNodeID { 
                get { return Parameters.sourceNodeID; } 
                set { Parameters.sourceNodeID = value; } 
            }
            public int PathDepthIndex;
            public int NextNodeIndex;
            public string GenerationLog;    // This is so we dont need to add a Debug.Log constantly, we can print once
        }
        class GrowthResult
        {
            public bool Success;
            public int Growth;
        }
        readonly struct NodeAddress
        {
            public readonly int Branch;
            public readonly int Depth;

            public NodeAddress(int branch, int depth)
            {
                Branch = branch;
                Depth = depth;
            }
        }
    }
}

