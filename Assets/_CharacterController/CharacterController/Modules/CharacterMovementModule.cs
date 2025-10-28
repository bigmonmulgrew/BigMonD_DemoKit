using UnityEngine;

using Utils;
namespace BMD
{
    [RequireComponent(typeof(UnityEngine.CharacterController))]
    public class CharacterMovementModule : MonoBehaviour, ICharacterModule
    {
        #region Configuration
        [Header("Character Movement Settings")]
        [Tooltip("Speed settings for character walking.")]
        [SerializeField] protected float walkSpeed = 2f;            // Speed of the character movement - Speed at which animaiton will beging to transition from walk to run
        [Tooltip("Speed settings for character run. eg Full positive movement input")]
        [SerializeField] protected float runSpeed = 6f;             // Speed of the character when running
        [Tooltip("Speed settings for various character sprint")]
        [SerializeField] protected float sprintSpeed = 10f;         // Speed of the character when sprinting
        [Tooltip("Acceleration and deceleration")]
        [SerializeField] float SpeedChangeRate = 10.0f;


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
        int currentAerialJumps = 0;
        bool isSprintHeld = false;
        bool isSprinting = false;
        private CharacterState CurrentState
        {
            get { return controller.CurrentState; }
            set { controller.CurrentState = value; }
        }
        #endregion

        // property to return isSprintHeld || isSprinting && unityController.velocity.magnitude > walkSpeed
        bool IsSprinting { get { return isSprintHeld || isSprinting && unityController.velocity.magnitude > walkSpeed; } }

        public void Initialize(CharacterController controller)
        {
            this.controller = controller;
            unityController = controller.GetComponent<UnityEngine.CharacterController>();

            controller.OnJumpRequested += HandleJumpRequested;
            // On sprint down set isSprinting to true
            controller.OnSprintDown += HandleSprintDown;
            controller.OnSprintUp += HandleSprintUp;
        }
        public void Tick(float deltaTime)
        {
            isSprinting = IsSprinting;
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
        private void HandleSprintDown()
        {
            isSprintHeld = true;
        }
        private void HandleSprintUp()
        {
            isSprintHeld = false;
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
            Vector3 inputDir = controller.MoveDirection;            // Set by PlayerController
            Debugger.Log("[MovementModule] Input Magnitude: " + inputDir.magnitude);
            if (!unityController.isGrounded && !airControl)
                inputDir = Vector3.zero;
            else if (!unityController.isGrounded)
                inputDir *= airControlFactor;

            //ternary operator to choose between walkSpeed and sprintSpeed
            float moveSpeed = isSprinting ? sprintSpeed : walkSpeed;
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
