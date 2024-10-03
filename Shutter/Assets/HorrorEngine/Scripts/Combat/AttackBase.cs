using UnityEngine;

namespace HorrorEngine
{
    public interface IAttack
    {
        void StartAttack();

        void OnAttackNotStarted();
    }

    public interface IVerticalAttack : IAttack
    {
        void SetVerticality(float verticality);
    }
    public struct AttackInfo
    {
        public AttackBase Attack;
        public Damageable Damageable;
        public Vector3 ImpactDir;
        public Vector3 ImpactPoint;
    }

    public abstract class AttackBase : MonoBehaviour, IAttack
    {
        public Combatant Owner { get; private set; }
        [SerializeField] protected AttackType m_Attack;

        // --------------------------------------------------------------------
        protected virtual void Awake() { }

        // --------------------------------------------------------------------
        protected virtual void OnEnable()
        {
            // Attacks are usually attached dynamically (as weapons). So refresh the owner every time the attack enables
            Owner = GetComponentInParent<Combatant>();
        }

        // --------------------------------------------------------------------

        public virtual void StartAttack()
        {
            
        }

        // --------------------------------------------------------------------

        public void Process(AttackInfo info)
        {
            AttackImpact impact = m_Attack.GetImpact(info.Damageable.Type);
            if (impact != null)
            {
                if (impact.Filters != null)
                {
                    foreach (var filter in impact.Filters)
                    {
                        if (!filter.Passes(info))
                            return;
                    }
                }

                if (impact.PreDamageEffects != null)
                {
                    foreach (var effect in impact.PreDamageEffects)
                    {
                        effect.Apply(info);
                    }
                }

                info.Damageable.Damage(impact.Damage, info);

                if (impact.PostDamageEffects != null)
                {
                    foreach (var effect in impact.PostDamageEffects)
                    {
                        effect.Apply(info);
                    }
                }
            }
        }

        // --------------------------------------------------------------------

        public virtual void OnAttackNotStarted() { }
    }
}