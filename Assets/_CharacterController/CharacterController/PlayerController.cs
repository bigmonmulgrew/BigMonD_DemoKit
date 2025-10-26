using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BMD
{
    public class PlayerController : BMD.CharacterController
    {
        #region Serialized fields
        [Header("Camera Settings")]
        [Range(0.01f, 2f)]
        [SerializeField] float lookSensitivity = 1f;  // Speed of the camera rotation
        [Range(0, 85.0f)]
        [SerializeField] float verticalClamp = 80f; // Maximum vertical angle for camera rotation

        [Header("Camera Follow Settings")]
        [SerializeField] float followDistance = 5f;
        [SerializeField] float followHeight = 2f;
        [Range(-5.0f, 5.0f)]
        [SerializeField] float horizontalOffset = 0f; // Horizontal offset for the camera in third person mode
        [SerializeField] bool isThirdPerson = true; // toggle first/third person
        [SerializeField] float smoothSpeed = 10f;
        [SerializeField] float cameraFollowDamping = 0.05f;

        #endregion

        #region Cached references
        private PlayerControls playerControls;
        private InputAction move;
        private InputAction look;
        private InputAction jump;
        private InputAction roll;
        private InputAction crouch;
        #endregion

        #region Runtime variables
        private Vector2 lookInput;
        private float cameraPitch = 0f;

        // Camera
        private new Camera camera;          // New keyword to hide inherited member, inherited member is depricated anyway.
        private Transform cameraPivot;
        private Transform cameraRoot;
        private Vector3 cameraVelocity;

        #endregion
        protected override void Awake()
        {
            base.Awake();
            SetupControls();
            SetupCamera();

        }

        private void SetupCamera()
        {
            camera = GetComponentInChildren<Camera>();
            if (camera == null)
            {
                Debug.LogWarning("No camera found on the player. Please attach a child camera.");
                return;
            }

            // 1. Create and position CameraPivot (yaw control)
            cameraPivot = new GameObject("CameraPivot").transform;
            cameraPivot.position = transform.position;
            cameraPivot.rotation = Quaternion.identity;

            // 2. Create and position CameraRoot (pitch control)
            cameraRoot = new GameObject("CameraRoot").transform;
            cameraRoot.SetParent(cameraPivot, false);
            cameraRoot.localPosition = new Vector3(0f, followHeight, 0f);
            cameraRoot.localRotation = Quaternion.identity;

            // 3. Reparent and reposition the actual camera
            camera.transform.SetParent(cameraRoot, false);
            camera.transform.localPosition = new Vector3(horizontalOffset, 0f, -followDistance);
            camera.transform.localRotation = Quaternion.identity;
        }


        private void SetupControls()
        {
            playerControls = new PlayerControls();
            move = playerControls.Player.Move;
            jump = playerControls.Player.Jump;
            look = playerControls.Player.Look;
            crouch = playerControls.Player.Crouch;
            roll = playerControls.Player.Roll;
        }

        private void OnEnable()
        {
            playerControls.Player.Enable();
            look.performed += ctx => lookInput = ctx.ReadValue<Vector2>();
            look.canceled += ctx => lookInput = Vector2.zero;
            crouch.performed += ctx => ToggleCrouch();
            roll.performed += ctx => PerformRoll();
        }

        private void OnDisable()
        {
            playerControls.Player.Disable();
        }

        protected override void Update()
        {
            HandleLook();

            Jump();
            base.Update();
        }


        private void UpdateCameraRigFollow()
        {
            if (cameraPivot == null) return;

            Vector3 targetPos = transform.position;
            cameraPivot.position = Vector3.SmoothDamp(
                 cameraPivot.position,
                 transform.position,
                 ref cameraVelocity,
                 cameraFollowDamping
             );
        }
        private void HandleLook()
        {
            Vector2 delta = lookInput * lookSensitivity;

            // Pitch (up/down)
            cameraPitch -= delta.y;
            cameraPitch = Mathf.Clamp(cameraPitch, -verticalClamp, verticalClamp);
            cameraRoot.localEulerAngles = new Vector3(cameraPitch, 0f, 0f);

            // Yaw (left/right)
            cameraPivot.Rotate(Vector3.up * delta.x);
        }
        protected override void Jump()
        {
            if (jump.WasPressedThisFrame())
            {
                base.Jump();
            }
        }
        protected override void FixedUpdate()
        {
            SetMoveDirection();
            RotateTowardsMoveDirection();

            UpdateCameraRigFollow();
            base.FixedUpdate();

        }
        private void RotateTowardsMoveDirection()
        {
            Vector3 moveDirectionFlat = new Vector3(moveDirection.x, 0f, moveDirection.z);

            if (moveDirectionFlat.sqrMagnitude < 0.001f)
                return; // Don’t rotate if not moving

            Quaternion targetRotation = Quaternion.LookRotation(moveDirectionFlat);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime);
        }
        private void SetMoveDirection()
        {
            Vector2 moveInput = move.ReadValue<Vector2>();
            if (moveInput.sqrMagnitude > 1)
                moveInput.Normalize();

            // Get the camera's forward and right vectors
            Vector3 camForward = cameraRoot.forward;
            Vector3 camRight = cameraRoot.right;

            // Flatten both vectors to prevent vertical movement
            camForward.y = 0f;
            camRight.y = 0f;
            camForward.Normalize();
            camRight.Normalize();

            // Combine input with camera directions
            moveDirection = camForward * moveInput.y + camRight * moveInput.x;
        }
        protected override void ToggleCrouch()
        {
            if (crouch.WasPressedThisFrame())
            {
                base.ToggleCrouch();
            }
        }
        protected override void PerformRoll()
        {
            if (roll.WasPressedThisFrame())
            {
                base.PerformRoll();
            }
        }

    }
}
