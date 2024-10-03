using UnityEngine;

namespace HorrorEngine
{
    public class PlayerStateMotion : ActorState
    {
        [SerializeField] ActorState m_InteractionState;
        [SerializeField] PlayerStateAiming m_AimingState;
        [SerializeField] ActorState m_Turn180State;
        [SerializeField] ActorState m_PushingState;
        [SerializeField] PlayerStateClimbing m_ClimbingState;
        [SerializeField] PushDetector m_PushDetector;
        [SerializeField] ClimbDetector m_ClimbDetector;
        [SerializeField] bool m_WaitTransitionToEnableMovement = true;

        private IPlayerInput m_Input;
        private PlayerMovement m_Movement;
        private PlayerInteractor m_Interaction;

        // --------------------------------------------------------------------

        protected override void Awake()
        {
            base.Awake();

            m_Input = GetComponentInParent<IPlayerInput>();
            m_Movement = GetComponentInParent<PlayerMovement>();
            m_Interaction = GetComponentInParent<PlayerInteractor>();
        }

        // --------------------------------------------------------------------

        public override void StateEnter(IActorState fromState)
        {
            base.StateEnter(fromState);

            m_Interaction.enabled = true;

            if (!m_WaitTransitionToEnableMovement || TransitionFinished)
                m_Movement.enabled = true;
        }

        // --------------------------------------------------------------------

        public override void StateUpdate()
        {
            base.StateUpdate();

            if (m_Interaction.IsInteracting)
            {
                SetState(m_InteractionState);
            }

            if (m_AimingState.IsAiming && GameManager.Instance.Inventory.GetEquippedWeapon() != null)
            {
                SetState(m_AimingState);
            }

            if (m_Input.IsTurn180Down())
            {
                SetState(m_Turn180State);
            }

            if (m_PushDetector.IsPushing)
            {
                SetState(m_PushingState);
            }

            if (m_ClimbDetector.Climbable && m_Input.IsInteractingDown())
            {
                SetState(m_ClimbingState);
            }
        }

        // --------------------------------------------------------------------

        public override void StateExit(IActorState intoState)
        {
            m_Interaction.enabled = false;
            m_Movement.enabled = false;
            base.StateExit(intoState);
        }


        // --------------------------------------------------------------------

        public override void OnEnterTransitionEnd()
        {
            base.OnEnterTransitionEnd();
            m_Movement.enabled = true;
        }
    }
}