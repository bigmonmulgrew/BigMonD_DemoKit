using System.Collections.Generic;
using UnityEngine;
namespace BMD.ProcGen
{

    public class Connection : MonoBehaviour
    {
        #region Configuration
        [SerializeField] ConnectionDirection direction = ConnectionDirection.Auto;
        [SerializeField] GameObject editorVisualisation;
        [SerializeField] bool keepVisualisationOnPlay = false;
        #endregion

        #region Cached References
        Connection linked;
        Breadcrumbs breadcrumbs;
        Node parent;
        #endregion

        #region Runtime Variables
        Vector3 parentOffset;
        string originalName;
        ConnectionDirection defaultDirection;
        (Connection, Connection) lastTestedConnections;
        #endregion

        #region Properties
        public ConnectionDirection Direction => direction;
        public ConnectionDirection DefaultDirection => defaultDirection;
        #endregion

        private void Awake()
        {
            if (parent == null)
            {
                Debug.LogError($"{name}: Connection has not been initialised with a parent node.");
                return;
            }

            RemoveEditorVisualisation();
            FindBreadcrumbs();
            GetParentOffset();
            SetDirection();
            defaultDirection = direction;
            originalName = name;
            SetName();
        }
        void FindBreadcrumbs()
        {
            breadcrumbs = GetComponent<Breadcrumbs>();
        }
        public void RemoveBreadcrumbs()
        {
            if (!breadcrumbs) return;

            Destroy(breadcrumbs.gameObject);
        }
        public void KeepBreadcrumbs()
        {
            if (!breadcrumbs) return;

            // Attach to the connectors parent
            breadcrumbs.transform.parent = transform.parent;

            if (transform.parent.TryGetComponent<Node>(out Node node))
            {
                node.AddBreadcrumbs(breadcrumbs);
            }
        }
        void RemoveEditorVisualisation()
        {
            if (keepVisualisationOnPlay) return;

            if (!editorVisualisation) return;

            Destroy(editorVisualisation.gameObject);
        }
        void SetName()
        {
            name = $"{direction.ToString()}_{originalName}";
        }
        private void GetParentOffset()
        {
            parentOffset = transform.position - parent.transform.position;
            //Debug.Log($"{name}: Connection initialised with parent {parent.name} and offset {parentOffset}");
        }
        public void RotateConnection(bool reverse)
        {
            GetParentOffset();
            direction = (direction, reverse) switch
            {
                (ConnectionDirection.North, false) => ConnectionDirection.East,
                (ConnectionDirection.East,  false) => ConnectionDirection.South,
                (ConnectionDirection.South, false) => ConnectionDirection.West,
                (ConnectionDirection.West,  false) => ConnectionDirection.North,
                (ConnectionDirection.North, true)  => ConnectionDirection.East,
                (ConnectionDirection.East,  true)  => ConnectionDirection.South,
                (ConnectionDirection.South, true)  => ConnectionDirection.West,
                (ConnectionDirection.West,  true)  => ConnectionDirection.North,
                _ => direction, // Fallback to no change
            };
            SetName();
        }
        public void ResetConnectionRotation()
        {
            direction = defaultDirection;
        }
        private void SetDirection()
        {
            if (direction != ConnectionDirection.Auto) return;

            Vector3 dir = parentOffset.normalized;
            if (Mathf.Abs(dir.x) > Mathf.Abs(dir.z))
            {
                direction = dir.x > 0 ? ConnectionDirection.North : ConnectionDirection.South;
            }
            else
            {
                direction = dir.z > 0 ? ConnectionDirection.West : ConnectionDirection.East;
            }
            //Debug.Log($"{name}: Auto-set connection direction to {direction}");
        }
        public void Initialise(Node parent)
        {
            this.parent = parent;
        }
        /// <summary>
        /// Moves objects without creating links. Should usually be followed up with Link or CompleteTestLinks
        /// </summary>
        /// <param name="conA"></param>
        /// <param name="conB"></param>
        public static void TestLink(Connection conA, Connection conB)
        {
            Vector3 parentBNewPos = conA.transform.position - conB.parentOffset;

            conB.parent.transform.position = parentBNewPos;
            
            conA.lastTestedConnections = (conA,  conB);            
        }
        public static void CompleteTestLinks(List<Connection> list)
        {
            foreach (Connection con in list)
            {
                if (con.lastTestedConnections.Item1 == null) continue;
                Link(con.lastTestedConnections.Item1, con.lastTestedConnections.Item2);
            }
        }
        public static void Link(Connection conA, Connection conB)
        {
            if (conA.linked != null || conB.linked != null)
            {
                Debug.LogError($"Cannot link {conA.name} and {conB.name} because one of them is already linked.");
                return;
            }
            conA.linked = conB;
            conB.linked = conA;

            conA.KeepBreadcrumbs();
            conB.KeepBreadcrumbs();

            Debug.Log($"Linked {conA.name} ({conA.direction}) to {conB.name} ({conB.direction})");

            Vector3 parentBNewPos = conA.transform.position - conB.parentOffset;

            conB.parent.transform.position = parentBNewPos;
        }
    }
}