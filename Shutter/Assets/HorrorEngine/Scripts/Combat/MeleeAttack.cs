using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Serialization;

namespace HorrorEngine
{
    public class MeleeAttack : WeaponAttack, IVerticalAttack
    {
        [Header("Melee Specs")]
        [SerializeField] private HitBox m_Hitbox;
        [FormerlySerializedAs("m_Duration")]
        [SerializeField] private float m_HitDuration;
        [Tooltip("This determines the maximum number of damageables that will be hit. Leave at 0 to hit all damageables")]
        [SerializeField] private int m_PenetrationHits = 0;

        private List<Damageable> m_Damageables = new List<Damageable>();
        private DamageableSorting m_DamageableSort = new DamageableSorting();
        private float m_Verticality;
        private int m_Hits;
        private float m_Time;

        // --------------------------------------------------------------------

        private void Start()
        {
            enabled = false;
        }

        // --------------------------------------------------------------------

        public override void StartAttack()
        {
            base.StartAttack();

            m_DamageableSort.Clear();

            m_Hits = 0;
            m_Time = 0f;
            enabled = true;
        }

        // --------------------------------------------------------------------

        public void FixedUpdate()
        {
            m_Hitbox.GetOverlappingDamageables(m_Damageables);

            m_DamageableSort.SortAndGetImpacted(ref m_Damageables, m_Attack);

            foreach (Damageable damageable in m_Damageables)
            {
                Vector3 fakeHitPoint = (damageable.transform.position + m_Hitbox.transform.position) * 0.5f;
                Vector3 impactDir = (damageable.transform.position - m_Hitbox.transform.position).normalized;
                Process(new AttackInfo()
                {
                    Attack = this,
                    Damageable = damageable,
                    ImpactDir = impactDir,
                    ImpactPoint = fakeHitPoint
                });
                ++m_Hits;
                if (m_PenetrationHits > 0 && m_Hits >= m_PenetrationHits)
                    break;
            }

            m_Time += Time.deltaTime;
            if (m_Time > m_HitDuration)
            {
                enabled = false;
            }
        }

       

        // --------------------------------------------------------------------

        public void SetVerticality(float verticality)
        {
            m_Verticality = verticality;
        }

    }
}
