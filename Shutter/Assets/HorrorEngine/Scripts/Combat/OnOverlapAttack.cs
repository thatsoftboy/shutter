using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace HorrorEngine
{
    public class OnOverlapAttack : AttackBase
    {
        [Tooltip("Delay applied recursively to wait before applying the attack impact. If Duration is 0 this is only waited once")]
        public float DamageRate = 1f;
        [Tooltip("Duration passed before the attack deactivates. Set to -1 for infinite duration")]
        public float Duration = 0f;
        [Tooltip("The HitBox used for this attack. If null it will be try to be obtained from this object")]
        [SerializeField] private HitBox m_HitBox;
        [Tooltip("This determines the maximum number of damageables that will be hit. Leave at 0 to hit all damageables")]
        [SerializeField] private int m_PenetrationHits = 0;

        public UnityEvent OnAttackStart;
        public UnityEvent OnAttackEnd;

        private DamageableSorting m_DamageableSort = new DamageableSorting();
        private float m_CurrentDuration;
        private float m_Time;
        private int m_Hits;
        private List<Damageable> m_Damageables = new List<Damageable>();

        // --------------------------------------------------------------------

        protected override void Awake()
        {
            base.Awake();

            if (!m_HitBox)
                m_HitBox = GetComponent<HitBox>();
        }

        // --------------------------------------------------------------------

        protected override void OnEnable()
        {
            base.OnEnable();

            m_Hits = 0;
            m_DamageableSort.Clear();
            m_CurrentDuration = 0f;
            m_Time = 0;

            OnAttackStart?.Invoke();
            Hit();
        }

        // --------------------------------------------------------------------

        private void OnDisable()
        {
            OnAttackEnd?.Invoke();
        }

        // --------------------------------------------------------------------

        public void FixedUpdate()
        {
            m_CurrentDuration += Time.deltaTime;
            m_Time += Time.deltaTime;
            if (m_Time >= DamageRate)
            {
                Hit();
            }

            if (Duration > 0 && m_CurrentDuration >= Duration)
            {
                enabled = false;
                return;
            }
        }

        // --------------------------------------------------------------------

        private void Hit()
        {
            m_HitBox.GetOverlappingDamageables(m_Damageables);
            m_DamageableSort.SortAndGetImpacted(ref m_Damageables, m_Attack);

            foreach (Damageable damageable in m_Damageables)
            {
                Vector3 fakeHitPoint = (damageable.transform.position + m_HitBox.transform.position) * 0.5f;
                Vector3 impactDir = (damageable.transform.position - m_HitBox.transform.position).normalized;
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

            m_Time = 0f;

            if (Duration == 0)
            {
                enabled = false;
            }
        }

        // --------------------------------------------------------------------

        public override void StartAttack()
        {
            base.StartAttack();

            enabled = true;
        }

    }
}