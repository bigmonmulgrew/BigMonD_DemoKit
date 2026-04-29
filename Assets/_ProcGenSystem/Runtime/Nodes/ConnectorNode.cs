using UnityEngine;

namespace BMD.ProcGen
{
    /// <summary>
    /// Used to bridge the gap between other node types.
    /// </summary>
    public class ConnectorNode : Node
    {
        [Range(0, 10)]
        [SerializeField] int length = 1;

        public int Length => length;
    }
}