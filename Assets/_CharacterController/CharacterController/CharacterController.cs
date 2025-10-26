using System;
using System.Collections;
using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BMD
{
    [RequireComponent(typeof(UnityEngine.CharacterController))] // Ensure that a CharacterController component is attached
    public abstract class CharacterController : MonoBehaviour
    {
        private List<ICharacterModule> modules = new();

        #region Constants
        protected const float IDLE_VARIATION_INTERVAL = 2f; // Interval for idle animation variation
        protected const float IDLE_BLEND_SPEED = 0.5f; // Higher = faster blending
        #endregion

        #region Serialized fields
        [Header("Character Movement Settings")]
        [Tooltip("Speed settings for various character walking")]
        [SerializeField] protected float walkSpeed = 2f;        // Speed of the character movement
        [Tooltip("Speed settings for various character run")]
        [SerializeField] protected float runSpeed = 6f;        // Speed of the character when running
        [Tooltip("Speed settings for various character sprint")]
        [SerializeField] protected float sprintSpeed = 10f;      // Speed of the character when sprinting
        [SerializeField] protected bool rotationEnabled = true; // Whether character rotation is enabled
        [Tooltip("Speed settings for various character rotation")]
        [SerializeField] protected float rotationSpeed = 10f;   // Speed of character rotation in degrees per second
        [SerializeField] protected float rollSpeed = 15f;       // Speed of the character when rolling
        [SerializeField] protected float rollDuration = 0.6f;   // Duration of the roll animation
        [SerializeField] protected float crouchSpeed = 2.5f;    // Speed of the character when crouching
        [SerializeField] protected float crawlSpeed = 1f;       // Speed of the character when crawling
        [SerializeField] protected float pushSpeed = 3f;        // Speed of the character when pushing objects
        [SerializeField] protected float pullSpeed = 3f;        // Speed of the character when pulling objects
        [SerializeField] protected float climbSpeed = 3f;       // Speed of the character when climbing
        [SerializeField] protected float swimSpeed = 4f;        // Speed of the character when swimming
        [SerializeField] protected float swingSpeed = 8f;       // Speed of the character when swinging
        [SerializeField] protected float flySpeed = 12f;        // Speed of the character when flying

        [Header("Jump Settings")]
        [SerializeField] protected float jumpForce = 5f; // Force applied when jumping
        [SerializeField] protected int aerialJumps = 1; // Number of additional jumps allowed in the air
        [SerializeField] protected bool airControl = true; // Whether the character can control movement in the air
        [Range(0, 1)]
        [SerializeField] protected float airControlFactor = 0.5f; // Factor by which air control is applied to movement speed
        [SerializeField] protected float gravityScale = 1f; // Scale factor for gravity applied to the character

        #endregion

        #region Cached references
        protected Vector3 gravity = UnityEngine.Physics.gravity; // Gravity vector for the character
        protected UnityEngine.CharacterController unityController; // Reference to the CharacterController component    
        protected Animator animator;
        #endregion

        #region Runtime variables
        protected int currentAerialJumps = 0; // Counter for aerial jumps
        protected Vector3 moveDirection = Vector3.zero; // Current movement direction of the character
        protected float verticalVelocity = 0f; // Current vertical velocity of the character

        protected CharacterState currentState = CharacterState.Idle;
        private Coroutine idleLoopCoroutine; // Coroutine for handling idle loop animations
        private Coroutine rollCoroutine; // Coroutine for handling rolling movement

        private float currentIdleBlend = 0f;
        private float targetIdleBlend = 0f;

        #endregion


        protected virtual void Awake()
        {
            unityController = GetComponent<UnityEngine.CharacterController>();
            animator = GetComponent<Animator>();

            modules.AddRange(GetComponents<ICharacterModule>());
            foreach (var module in modules)
                module.Initialize(this);
        }
        protected virtual void Start()
        {
            if (unityController == null)
            {
                Debug.LogError("CharacterController component is missing on " + gameObject.name);
            }
        }
        protected virtual void Update()
        {
            foreach (var module in modules)
                module.Tick(Time.deltaTime);
        }
        protected virtual void FixedUpdate()
        {
            // Move direction should be set by the sub class
            Move(moveDirection);
            UpdateState();
        }

#if UNITY_EDITOR
        [ContextMenu("Add Default Modules")]
        private void AddDefaultModules()
        {
            if (!GetComponent<CharacterMovementModule>())
            {
                gameObject.AddComponent<CharacterMovementModule>();
                Debug.Log("Added default CharacterMovementModule.");
            }
            EditorUtility.SetDirty(this);
        }
#endif

        [ExecuteAlways]
        protected virtual void Reset()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                if (GetComponents<ICharacterModule>().Length == 0)
                {
                    gameObject.AddComponent<CharacterMovementModule>();
                    Debug.Log("Auto-added default CharacterMovementModule on new controller.");
                }
            }
#endif
        }

        protected virtual void Jump()
        {
            if (unityController.isGrounded)
            {
                verticalVelocity = jumpForce;
                currentAerialJumps = 0;
                animator.SetTrigger("JumpTrigger");
            }
            else if (currentAerialJumps < aerialJumps)
            {
                verticalVelocity = jumpForce;
                currentAerialJumps++;
                animator.SetTrigger("JumpTrigger");
            }
        }
        protected virtual void Move(Vector3 direction)
        {
            // Apply gravity
            if (unityController.isGrounded)
            {
                if (verticalVelocity < 0)
                    verticalVelocity = -2f; // Small downward force to keep grounded
            }
            else
            {
                verticalVelocity += gravity.y * gravityScale * Time.fixedDeltaTime;
            }

            // Horizontal movement
            Vector3 horizontalMove = Vector3.zero;

            if (unityController.isGrounded)
            {
                horizontalMove = direction * walkSpeed;
            }
            else if (airControl)
            {
                horizontalMove = direction * walkSpeed * airControlFactor;
            }

            // Combine horizontal and vertical movement
            Vector3 finalMove = horizontalMove;
            finalMove.y = verticalVelocity;

            // Apply movement
            unityController.Move(finalMove * Time.fixedDeltaTime);
        }
        protected virtual void UpdateState()
        {
            // Don't interrupt special states like rolling
            if (currentState == CharacterState.Rolling)
            {
                // Unless we roll off a cliff or something
                if (!unityController.isGrounded)
                {
                    currentState = verticalVelocity <= 0f ? CharacterState.Falling : CharacterState.Jumping;
                }
                UpdateAnimatorState();
                return;
            }

            if (unityController.isGrounded)
            {
                if (moveDirection.magnitude > 0.1f) currentState = CharacterState.Walking;
                else currentState = CharacterState.Idle;

            }
            else
            {
                if (verticalVelocity > 0f) currentState = CharacterState.Jumping;
                else currentState = CharacterState.Falling;
            }

            if (currentState != CharacterState.Idle && idleLoopCoroutine != null)
            {
                StopCoroutine(idleLoopCoroutine);
                idleLoopCoroutine = null;
            }

            UpdateAnimatorState();
        }

        protected virtual void UpdateAnimatorState()
        {
            // Optionally, use a state int or trigger
            animator.SetInteger("State", (int)currentState);

            animator.SetBool("IsGrounded", unityController.isGrounded);

            Vector2 moveDirection2D = new Vector2(moveDirection.x, moveDirection.z);
            float normalizedSpeed = moveDirection2D.magnitude;
            animator.SetFloat("Speed", normalizedSpeed, 0.4f, Time.deltaTime);
            animator.SetFloat("VerticalVelocity", verticalVelocity);

            // Smooth idle style blending
            if (currentState == CharacterState.Idle)
            {
                currentIdleBlend = Mathf.MoveTowards(currentIdleBlend, targetIdleBlend, IDLE_BLEND_SPEED * Time.deltaTime);
                animator.SetFloat("IdleStyle", currentIdleBlend);
            }

            //Apply turning
            Vector3 flatForward = transform.forward;
            Vector3 flatMove = new Vector3(moveDirection.x, 0f, moveDirection.z);

            float turnAngle = Vector3.SignedAngle(flatForward, flatMove, Vector3.up);
            animator.SetFloat("TurnAngle", turnAngle, 0.1f, Time.fixedDeltaTime);
        }

        public void OnIdleLoopComplete()
        {
            float chance = UnityEngine.Random.value; // 0.0 to 1.0
            if (chance < 0.3f) // 30% chance
            {
                animator.SetTrigger("SwitchIdle");

                if (idleLoopCoroutine == null)
                {
                    idleLoopCoroutine = StartCoroutine(IdleLoop());
                }



            }
        }

        protected virtual IEnumerator IdleLoop()
        {
            while (true)
            {
                yield return new WaitForSeconds(IDLE_VARIATION_INTERVAL);

                targetIdleBlend = UnityEngine.Random.value; // pick a new idle style
            }
        }

        protected virtual void ToggleCrouch()
        {
            Debug.Log("ToggleCrouch called, but not implemented in base class.");
        }

        protected virtual void PerformRoll()
        {
            // Early exit conditions
            if (!unityController.isGrounded ||
                currentState == CharacterState.Rolling ||
                currentState != CharacterState.Idle &&
                currentState != CharacterState.Walking &&
                currentState != CharacterState.Running)
            {
                return;
            }

            currentState = CharacterState.Rolling;
            animator.SetTrigger("RollTrigger");

            // If not moving roll forward
            Vector3 rollDirection = moveDirection.sqrMagnitude > 0.1f ? moveDirection : transform.forward;
            rollCoroutine = StartCoroutine(PerformRollMovement(rollDirection.normalized));
        }

        protected virtual IEnumerator PerformRollMovement(Vector3 direction)
        {
            float elapsed = 0f;

            while (elapsed < rollDuration)
            {
                unityController.Move(direction * rollSpeed * Time.deltaTime);
                elapsed += Time.deltaTime;
                yield return null;
            }

            currentState = CharacterState.Idle;
        }


    }
}
