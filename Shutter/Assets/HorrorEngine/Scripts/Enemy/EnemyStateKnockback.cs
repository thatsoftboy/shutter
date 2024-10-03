using UnityEngine;

namespace HorrorEngine
{
    public class EnemyStateKnockback : ActorStateWithDuration
    {
        private static readonly float k_MinRigidbodySpeed = 0.1f;

        [SerializeField] private float m_Force;
        [SerializeField] private float m_Drag;
        
        protected Rigidbody m_Rigidbody;
        protected Vector3 m_Velocity;

        private bool HasStopped => m_Rigidbody.velocity.magnitude < k_MinRigidbodySpeed;

        // --------------------------------------------------------------------

        protected override void Awake()
        {
            base.Awake();

            m_Rigidbody = GetComponentInParent<Rigidbody>();
        }

        // --------------------------------------------------------------------

        public override void StateEnter(IActorState fromState)
        {
            base.StateEnter(fromState);

            m_Rigidbody.isKinematic = false;
            m_Velocity = -Actor.transform.forward * m_Force;
        }

        // --------------------------------------------------------------------

        public override void StateExit(IActorState intoState)
        {
            m_Rigidbody.isKinematic = true;

            base.StateExit(intoState);
        }

        // --------------------------------------------------------------------

        public override void StateFixedUpdate()
        {
            base.StateFixedUpdate();

            m_Velocity = m_Velocity * (1 - m_Drag);
            m_Rigidbody.velocity = m_Velocity;

            if (HasStopped && DurationElapsed)
                SetState(m_ExitState);
        }

        protected override void OnStateDurationEnd()
        {
            if (HasStopped && DurationElapsed)
                base.OnStateDurationEnd();
        }
    }
}