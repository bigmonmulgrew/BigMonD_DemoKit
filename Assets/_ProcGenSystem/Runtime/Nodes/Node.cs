using System.Collections.Generic;
using UnityEngine;

namespace BMD.ProcGen
{
    public class Node : MonoBehaviour
    {
        List<Connection> connections = new();

        public List<Connection> Connections => connections;
        public void Awake()
        {
            connections.AddRange(GetComponentsInChildren<Connection>());
            foreach (var connection in connections)
            {
                connection.Initialise(this);
            }
        }
        
        public void Clear()
        {
            // Clear any state or references here if needed
            throw new System.NotImplementedException("Clear method is not implemented yet.");
        }
    }

}
