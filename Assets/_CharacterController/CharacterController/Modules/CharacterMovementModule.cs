using UnityEngine;

using Utils;
namespace BMD
{
    [RequireComponent(typeof(UnityEngine.CharacterController))]
    public class CharacterMovementModule : MonoBehaviour, ICharacterModule
    {
        #region Configuration
        [Header("Jump Settings")]
        [SerializeField] protected float jumpForce = 5f; // Force applied when jumping
        [SerializeField] protected int aerialJumps = 1; // Number of additional jumps allowed in the air
        [SerializeField] protected bool airControl = true; // Whether the character can control movement in the air
        [Range(0, 1)]
        [SerializeField] protected float airControlFactor = 0.5f; // Factor by which air control is applied to movement speed
        [SerializeField] protected float gravityScale = 1f; // Scale factor for gravity applied to the character
        #endregion

        #region Cached references
        private CharacterController controller;
        private UnityEngine.CharacterController unityController;
        #endregion

        #region Runtime Variables
        float verticalVelocity;
        private int currentAerialJumps = 0;
        private CharacterState CurrentState
        {
            get { return controller.CurrentState; }
            set { controller.CurrentState = value; }
        }
        #endregion



        public void Initialize(CharacterController controller)
        {
            this.controller = controller;
            unityController = controller.GetComponent<UnityEngine.CharacterController>();

            controller.OnJumpRequested += HandleJumpRequested;
        }

        public void Tick(float deltaTime)
        {
            // Optional: update animator params here later
        }

        public void FixedTick(float fixedDeltaTime)
        {
            ApplyMovement(fixedDeltaTime);
        }

        private void HandleJumpRequested()
        {
            if (unityController.isGrounded)
            {
                verticalVelocity = jumpForce;
                currentAerialJumps = 0;
                controller.NotifyJumpPerformed();
            }
            else if (currentAerialJumps < 1) // could use controller.GetAerialJumpCount()
            {
                verticalVelocity = jumpForce;
                currentAerialJumps++;
                controller.NotifyJumpPerformed();
            }
        }

        private void ApplyMovement(float dt)
        {
            // Gravity
            Vector3 gravity = Physics.gravity;
            float weightedGravityY = gravity.y * gravityScale;

            if (unityController.isGrounded && verticalVelocity < 0)
            {
                if (verticalVelocity < -2f)
                    controller.NotifyLanded();

                verticalVelocity = -2f;
                currentAerialJumps = 0;
            }
            else
            {
                verticalVelocity += weightedGravityY * dt;
            }

            // Horizontal movement
            Vector3 inputDir = controller.MoveDirection; // Set by PlayerController
            if (!unityController.isGrounded && !airControl)
                inputDir = Vector3.zero;
            else if (!unityController.isGrounded)
                inputDir *= airControlFactor;

            float moveSpeed = controller.GetWalkSpeed();
            Vector3 move = inputDir * moveSpeed;

            // Combine with vertical velocity
            move.y = verticalVelocity;

            // Apply movement
            unityController.Move(move * dt);

            UpdateState();
        }

        private void UpdateState()
        {
            CharacterState newState;

            if (unityController.isGrounded)
                newState = controller.MoveDirection.magnitude > 0.1f ? CharacterState.Walking : CharacterState.Idle;
            else
                newState = verticalVelocity > 0f ? CharacterState.Jumping : CharacterState.Falling;

            if (newState != CurrentState)
            {
                CurrentState = newState;
                controller.NotifyStateChanged(CurrentState);
            }
        }

        public void Dispose()
        {
            controller.OnJumpRequested -= HandleJumpRequested;
        }
    }
}
