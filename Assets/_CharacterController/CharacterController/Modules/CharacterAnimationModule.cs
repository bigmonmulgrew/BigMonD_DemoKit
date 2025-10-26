using UnityEngine;

namespace BMD
{
    [RequireComponent(typeof(Animator))]
    public class CharacterAnimatorModule : MonoBehaviour, ICharacterModule
    {
        private Animator animator;
        private CharacterController controller;

        // Cached state
        private CharacterState currentState;
        private bool isGrounded;

        // Animator parameter names (could be made configurable later)
        private static readonly int SpeedHash = Animator.StringToHash("Speed");
        private static readonly int VerticalVelocityHash = Animator.StringToHash("VerticalVelocity");
        private static readonly int StateHash = Animator.StringToHash("State");
        private static readonly int IsGroundedHash = Animator.StringToHash("IsGrounded");
        private static readonly int IsCrouchingHash = Animator.StringToHash("IsCrouching");
        private static readonly int JumpTriggerHash = Animator.StringToHash("JumpTrigger");
        private static readonly int LandTriggerHash = Animator.StringToHash("LandTrigger");
        private static readonly int RollTriggerHash = Animator.StringToHash("RollTrigger");
        private static readonly int StandTriggerHash = Animator.StringToHash("StandTrigger");


        public void Initialize(CharacterController controller)
        {
            this.controller = controller;
            animator = controller.GetComponent<Animator>();

            // Subscribe to controller events
            controller.OnJumpPerformed += HandleJumpPerformed;
            controller.OnLanded += HandleLanded;
            controller.OnStateChanged += HandleStateChanged;

            Debug.Log("[AnimatorModule] Subscribed to controller events.");
        }

        public void Tick(float deltaTime)
        {
            // Update movement blend parameters per frame
            float moveSpeed = controller.MoveDirection.magnitude;
            animator.SetFloat(SpeedHash, moveSpeed, 0.1f, deltaTime);
            animator.SetBool(IsGroundedHash, isGrounded);
            animator.SetInteger(StateHash, (int)currentState);
        }

        public void FixedTick(float fixedDeltaTime)
        {
            // Animator does not need fixed-timestep updates
        }

        private void HandleJumpPerformed()
        {
            animator.SetTrigger(JumpTriggerHash);
            isGrounded = false;
        }

        private void HandleLanded()
        {
            //animator.SetTrigger(LandTriggerHash); // TOIDO : enable when landing animation is added. modify to include landing animation.
            animator.SetBool(IsGroundedHash, true);
            isGrounded = true;
        }

        private void HandleStateChanged(CharacterState state)
        {
            currentState = state;
        }

        private void OnDestroy()
        {
            if (controller != null)
            {
                controller.OnJumpPerformed -= HandleJumpPerformed;
                controller.OnLanded -= HandleLanded;
                controller.OnStateChanged -= HandleStateChanged;
            }
        }
    }
}