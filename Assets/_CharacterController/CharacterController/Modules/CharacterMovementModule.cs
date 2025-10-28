using UnityEngine;
using Utils;
namespace BMD
{
    [RequireComponent(typeof(UnityEngine.CharacterController))]
    public class CharacterMovementModule : MonoBehaviour, ICharacterModule
    {
        const float MIN_WALK_SPEED = 0.1f;
        const float SPEED_OFFSET = 0.1f;

        #region Configuration
        [Header("Character Movement Settings")]
        [Tooltip("Speed settings for character walking.")]
        [SerializeField] float walkSpeed = 2f;            // Speed of the character movement - Speed at which animaiton will beging to transition from walk to run
        [Tooltip("Speed settings for character run. eg Full positive movement input")]
        [SerializeField] float runSpeed = 6f;             // Speed of the character when running
        [Tooltip("Speed settings for various character sprint")]
        [SerializeField] float sprintSpeed = 10f;         // Speed of the character when sprinting
        [Tooltip("Acceleration and deceleration")]
        [SerializeField] float movementAcceleration = 10.0f;
        [SerializeField] float minMoveInputMagnitude = 0.05f; // Minimum input magnitude to consider movement

        [Header("Rotation Settings")]
        [Tooltip("Toggle rotation")]
        [SerializeField] bool rotationEnabled = true; 
        [Tooltip("Rotation speed in degrees per second")]
        [SerializeField] float rotationSpeed = 10f;

        [SerializeField, Tooltip("When moving slower than this speed, rotation snaps instantly")]
        private float instantTurnThreshold = 0.05f;

        [Header("Jump and fall Settings")]
        [SerializeField] bool canJump = true; // Whether the character can jump
        [SerializeField] float jumpForce = 5f; // Force applied when jumping
        [SerializeField] int aerialJumps = 1; // Number of additional jumps allowed in the air
        [SerializeField] bool airControl = true; // Whether the character can control movement in the air
        [Range(0, 1)]
        [SerializeField] float airControlFactor = 0.5f; // Factor by which air control is applied to movement speed
        [SerializeField] float gravityScale = 1f; // Scale factor for gravity applied to the character
        [SerializeField] float terminalVelocity = 53f; // Maximum downward velocity due to gravity
        [Tooltip("Coyote Time Settings, applies if falling off object even if jump is disabled")]
        [SerializeField] float coyoteTime = 0.1f;   // Time window after leaving ground during which a jump can still be performed
        #endregion

        #region Cached references
        private CharacterController controller;
        private UnityEngine.CharacterController unityController;
        #endregion

        #region Runtime Variables
        float verticalVelocity;
        float moveSpeed = 0.0f;
        Vector3 currentHorrizontalVelocity = Vector3.zero;  // Declared here to avoid creating new vectors each frame, garbage collection optimisation.
        int currentAerialJumps = 0;
        float lastGroundedTime = 0f;
        bool isSprintHeld = false;
        bool isSprinting = false;
        private CharacterState CurrentState
        {
            get { return controller.CurrentState; }
            set { controller.CurrentState = value; }
        }
        #endregion

        // Enable sprinting and do not disable until speed drops below walk speed
        bool IsSprinting { get { return isSprintHeld || isSprinting && unityController.velocity.magnitude > walkSpeed; } }
        bool IsConsideredGrounded { get { return unityController.isGrounded || Time.time < lastGroundedTime + coyoteTime; } }   // Reusable property for coyote time check
        public void Initialize(CharacterController controller)
        {
            InitializeReferences(controller);
            InitializeSignals(controller);
            InitializeSanityChecks();
            
        }

        private void InitializeSanityChecks()
        {
            runSpeed = Mathf.Max(runSpeed, walkSpeed);
            sprintSpeed = Mathf.Max(sprintSpeed, runSpeed);
            walkSpeed = Mathf.Max(walkSpeed, MIN_WALK_SPEED);
        }

        private void InitializeSignals(CharacterController controller)
        {
            controller.OnJumpRequested += HandleJumpRequested;
            // On sprint down set isSprinting to true
            controller.OnSprintDown += HandleSprintDown;
            controller.OnSprintUp += HandleSprintUp;
        }

        private void InitializeReferences(CharacterController controller)
        {
            this.controller = controller;
            unityController = controller.GetComponent<UnityEngine.CharacterController>();
        }

        public void Tick(float deltaTime)
        {
            isSprinting = IsSprinting;
            if (unityController.isGrounded)
                lastGroundedTime = Time.time;
        }
        public void FixedTick(float fixedDeltaTime)
        {
            ApplyMovement(fixedDeltaTime);
            
            // Handle rotation
            RotateCharacterTowardsMovement(fixedDeltaTime);
        }
        private void HandleJumpRequested()
        {
            if (!canJump) return;

            if (IsConsideredGrounded)
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

            if (IsConsideredGrounded && verticalVelocity < 0)
            {
                if (verticalVelocity < -2f)
                    controller.NotifyLanded();

                verticalVelocity = -2f;
                currentAerialJumps = 0;
            }
            else
            {
                verticalVelocity += weightedGravityY * dt;
                // Clamp to terminal velocity
                if (verticalVelocity < -terminalVelocity)
                    verticalVelocity = -terminalVelocity;
            }

            // Horizontal movement
            Vector3 inputDir = controller.MoveDirection;            // Set by PlayerController
            float inputMagnitude = inputDir.magnitude;

            if (inputMagnitude < minMoveInputMagnitude)
                inputDir = Vector3.zero;

            if (!IsConsideredGrounded && !airControl)
                inputDir = Vector3.zero;
            else if (!IsConsideredGrounded)
                inputDir *= airControlFactor;

            //ternary operator to choose between runSpeed and sprintSpeed, walk speed is not used here and is only for threshholds within this module
            float targetSpeed = isSprinting ? sprintSpeed : runSpeed;

            currentHorrizontalVelocity.x = unityController.velocity.x;
            currentHorrizontalVelocity.y = 0f;  // Safety check
            currentHorrizontalVelocity.z = unityController.velocity.z;
            float currentHorizontalSpeed = currentHorrizontalVelocity.magnitude;

            if (currentHorizontalSpeed < targetSpeed - SPEED_OFFSET ||
            currentHorizontalSpeed > targetSpeed + SPEED_OFFSET)
            {
                // creates curved result rather than a linear one giving a more organic speed change
                // note T in Lerp is clamped, so we don't need to clamp our speed
                moveSpeed = Mathf.Lerp(currentHorizontalSpeed, targetSpeed * inputMagnitude,
                    dt * movementAcceleration);

                // round speed to 3 decimal places
                moveSpeed = Mathf.Round(moveSpeed * 1000f) / 1000f;
            }
            else
            {
                moveSpeed = targetSpeed;
            }

            moveSpeed = Mathf.Max(0f, moveSpeed);   // Preventions friction bugs from making speed negative

            Vector3 move = inputDir * moveSpeed;

            // Combine with vertical velocity
            move.y = verticalVelocity;

            // Apply movement
            unityController.Move(move * dt);

            UpdateState();
        }
        private void RotateCharacterTowardsMovement(float dt)
        {
            if (!rotationEnabled) return;

            // Vector 3 comparison uses approximation to account for floating point errors
            if (currentHorrizontalVelocity == Vector3.zero) return; // Nothing to rotate towards

            // Compute target rotation
            Quaternion targetRotation = Quaternion.LookRotation(currentHorrizontalVelocity.normalized);

            // Snap instantly if barely moving (prevents jitter)
            if (currentHorrizontalVelocity.magnitude < instantTurnThreshold)
            {
                unityController.transform.rotation = targetRotation;
                return;
            }

            // Smooth rotation
            unityController.transform.rotation = Quaternion.Slerp(
                unityController.transform.rotation,
                targetRotation,
                rotationSpeed * dt
            );
        }

        private void UpdateState()
        {
            CharacterState newState;
                     
            if (IsConsideredGrounded)
            {
                if (moveSpeed < MIN_WALK_SPEED)
                    newState = CharacterState.Idle;
                else if (moveSpeed <= walkSpeed)
                    newState = CharacterState.Walking;
                else if (moveSpeed <= runSpeed)
                    newState = CharacterState.Running;
                else
                    newState = CharacterState.Sprinting;

            }
            else
                newState = verticalVelocity > 0f ? CharacterState.Jumping : CharacterState.Falling;

            // Do nothing if state unchanged
            if (newState != CurrentState)
            {
                CurrentState = newState;
                controller.NotifyStateChanged(CurrentState);
            }
        }
        public void Dispose()
        {
            controller.OnJumpRequested -= HandleJumpRequested;
            controller.OnSprintDown -= HandleSprintDown;
            controller.OnSprintUp -= HandleSprintUp;
        }
    }
}
