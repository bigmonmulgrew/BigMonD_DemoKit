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
    }
}

