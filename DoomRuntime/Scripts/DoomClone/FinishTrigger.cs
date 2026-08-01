using UnityEngine;

namespace DoomArcade.Scripts.DoomClone
{
    public class FinishTrigger : MonoBehaviour
    {
        public Color gizmoColor = Color.green;

        void OnDrawGizmos()
        {
            Gizmos.color = gizmoColor;

            Collider col = GetComponent<Collider>();
            if (col != null)
            {
                Gizmos.matrix = col.transform.localToWorldMatrix;
                Gizmos.DrawWireCube(col.bounds.center - col.transform.position, col.bounds.size);
            }
            else
            {
                Gizmos.DrawWireCube(transform.position, Vector3.one);
            }
        }
    }
}