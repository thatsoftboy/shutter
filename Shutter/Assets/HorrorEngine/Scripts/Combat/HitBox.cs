using System.Collections.Generic;
using UnityEngine;

namespace HorrorEngine
{
    public class HitBox : MonoBehaviour
    {
        [SerializeField] private Vector3 m_Center;
        [SerializeField] private Vector3 m_Size = Vector3.one;
        [SerializeField] private LayerMask m_LayerMask;
        [SerializeField] private bool m_ShowDebug;

        private Collider[] m_OverlapResults = new Collider[10];

        // --------------------------------------------------------------------

        public void GetOverlappingDamageables(List<Damageable> damageables)
        {
            damageables.Clear();

            var boxOrigin = transform.TransformPoint(m_Center);
            var scaledSize = new Vector3(m_Size.x * transform.lossyScale.x, m_Size.y * transform.lossyScale.y, m_Size.z * transform.lossyScale.z);

#if UNITY_EDITOR
            if (m_ShowDebug)
                DebugUtils.DrawBox(boxOrigin, transform.rotation, scaledSize, Color.red, 10);
#endif

            int count = Physics.OverlapBoxNonAlloc(boxOrigin, scaledSize * 0.5f, m_OverlapResults, transform.rotation, m_LayerMask, QueryTriggerInteraction.Collide);            
            for (int i = 0; i < count; ++i)
            {
                if (m_OverlapResults[i].TryGetComponent(out Damageable d))
                    damageables.Add(d);
            }
        }

        // --------------------------------------------------------------------

        private void OnDrawGizmosSelected()
        {
            if (m_ShowDebug)
            {
                Gizmos.color = Color.red;

                Matrix4x4 transformMatrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale);

                // Set Gizmos matrix
                Gizmos.matrix = transformMatrix;
                Gizmos.DrawWireCube(m_Center, m_Size);

                Gizmos.matrix = Matrix4x4.identity;

                Gizmos.color = Color.white;
            }
        }

    }
}