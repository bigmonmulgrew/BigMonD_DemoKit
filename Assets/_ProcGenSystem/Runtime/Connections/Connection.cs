using UnityEngine;
namespace BMD.ProcGen
{

    public class Connection : MonoBehaviour
    {
        [SerializeField] ConnectionDirection direction = ConnectionDirection.Auto;

        Connection linked;
        Node parent;
        Vector3 parentOffset;


        private void Awake()
        {
            if (parent == null)
            {
                Debug.LogError($"{name}: Connection has not been initialised with a parent node.");
                return;
            }

            GetParentOffset();

            SetDirection();
        }

        private void GetParentOffset()
        {
            parentOffset = transform.position - parent.transform.position;
            Debug.Log($"{name}: Connection initialised with parent {parent.name} and offset {parentOffset}");
        }

        private void SetDirection()
        {
            if (direction != ConnectionDirection.Auto) return;

            Vector3 dir = parentOffset.normalized;
            if (Mathf.Abs(dir.x) > Mathf.Abs(dir.z))
            {
                direction = dir.x > 0 ? ConnectionDirection.East : ConnectionDirection.West;
            }
            else
            {
                direction = dir.z > 0 ? ConnectionDirection.North : ConnectionDirection.South;
            }
            Debug.Log($"{name}: Auto-set connection direction to {direction}");
        }

        public void Initialise(Node parent)
        {
            this.parent = parent;
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
            Debug.Log($"Linked {conA.name} ({conA.direction}) to {conB.name} ({conB.direction})");

            Vector3 parentBNewPos = conA.parentOffset + conB.parentOffset + conA.transform.position;

            conB.parent.transform.position = parentBNewPos;
        }
        
    }
}