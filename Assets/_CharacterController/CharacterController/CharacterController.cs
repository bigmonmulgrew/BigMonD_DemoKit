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

        #region Actions
        public event Action<CharacterState> OnStateChanged;
        public event Action<Vector3> OnMoveDirectionChanged;
        public event Action OnJumpRequested;    // Event fdired attempting to jump
        public event Action OnJumpPerformed;    // Event fired when jump is performed
        public event Action OnLanded;           // Evenet fires when character lands

        public event Action OnSprintDown;
        public event Action OnSprintUp;

        public event Action OnRollRequested;    // Event fired attempting to roll
        public event Action OnRollPerformed;    // Event fired when roll is performed
        public event Action OnRollEnded;        // Event fired when roll ends

        public event Action OnDodgeRequested;    // Event fired attempting to dodge
        public event Action OnDogePerformed;    // Event fired when dodge is performed
        public event Action OnDogeEnded;        // Event fired when dodge ends

        #endregion

        #region Constants
        protected const float IDLE_VARIATION_INTERVAL = 2f; // Interval for idle animation variation
        protected const float IDLE_BLEND_SPEED = 0.5f; // Higher = faster blending
        #endregion

        #region Serialized fields
        
        [SerializeField] protected bool rotationEnabled = true; // Whether character rotation is enabled
        [Tooltip("Speed settings for various character rotation")]
        //[SerializeField] protected float rollSpeed = 15f;       // Speed of the character when rolling
        //[SerializeField] protected float rollDuration = 0.6f;   // Duration of the roll animation
        [SerializeField] protected float crouchSpeed = 2.5f;    // Speed of the character when crouching
        [SerializeField] protected float crawlSpeed = 1f;       // Speed of the character when crawling
        [SerializeField] protected float pushSpeed = 3f;        // Speed of the character when pushing objects
        [SerializeField] protected float pullSpeed = 3f;        // Speed of the character when pulling objects
        [SerializeField] protected float climbSpeed = 3f;       // Speed of the character when climbing
        [SerializeField] protected float swimSpeed = 4f;        // Speed of the character when swimming
        [SerializeField] protected float swingSpeed = 8f;       // Speed of the character when swinging
        [SerializeField] protected float flySpeed = 12f;        // Speed of the character when flying
        #endregion
    


        #region Cached references
        protected Vector3 gravity = UnityEngine.Physics.gravity; // Gravity vector for the character
        protected UnityEngine.CharacterController unityController; // Reference to the CharacterController component    
        protected Animator animator;
        #endregion

        #region Runtime variables
        protected Vector3 moveDirection = Vector3.zero; // Current movement direction of the character
        public Vector3 MoveDirection => moveDirection;

        protected CharacterState currentState = CharacterState.Idle;
        private Coroutine idleLoopCoroutine; // Coroutine for handling idle loop animations
        private Coroutine rollCoroutine; // Coroutine for handling rolling movement

        private float currentIdleBlend = 0f;
        private float targetIdleBlend = 0f;

        #endregion

        #region Properties
        public CharacterState CurrentState 
        {
            get { return currentState; }
            set { currentState = value; }
        }
        #endregion

        #region Signal Helpers
        // --- Signal helpers (so modules can’t fire events directly) ---
        public void NotifyStateChanged(CharacterState state) => OnStateChanged?.Invoke(state);

        // Jump signal helpers
        public void RequestJump() => OnJumpRequested?.Invoke();
        public void NotifyJumpPerformed() => OnJumpPerformed?.Invoke();
        public void NotifyJumpLanded() => OnLanded?.Invoke();

        // Roll signal helpers
        public void RequestRoll() => OnRollRequested?.Invoke();
        public void NotifyRollPerformed() => OnRollPerformed?.Invoke();
        public void NotifyRollEnded() => OnRollEnded?.Invoke();

        //Dodge signal helpers
        public void RequestDodge() => OnDodgeRequested?.Invoke();
        public void NotifyDodgePerformed() => OnDogePerformed?.Invoke();
        public void NotifyDodgeEnded() => OnDogeEnded?.Invoke();


        protected void NotifySprintTriggered(bool triggered) 
        {
            if (triggered)
            {
                OnSprintDown?.Invoke();
            }
            else
            {
                OnSprintUp?.Invoke();
            }
        }
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
            // PlayerController sets MoveDirection; movement happens inside modules.
            foreach (var module in modules)
                module.FixedTick(Time.fixedDeltaTime);

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
            //rollCoroutine = StartCoroutine(PerformRollMovement(rollDirection.normalized));
        }
        //protected virtual IEnumerator PerformRollMovement(Vector3 direction)
        //{
        //    float elapsed = 0f;

        //    while (elapsed < rollDuration)
        //    {
        //        unityController.Move(direction * rollSpeed * Time.deltaTime);
        //        elapsed += Time.deltaTime;
        //        yield return null;
        //    }

        //    currentState = CharacterState.Idle;
        //}


    }
}
