using System.Collections.Generic;
using UnityEngine;

public class PathNode
{
    public int Id;
    public Vector2 Position;

    public PathNode Parent;                  // optional, useful for backtracking
    public PathNode GoldenChild;            // marks the child which represents the main path
    public List<PathNode> Children = new(); // branch exits from this node
}