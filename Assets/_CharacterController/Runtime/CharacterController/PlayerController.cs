using PlasticPipe.PlasticProtocol.Messages;
using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BMD
{
    public class PlayerController : BMD.CharacterController
    {
        #region Cached references
        private PlayerControls playerControls;
        private InputAction move;
        private InputAction look;
        private InputAction zoom;
        private InputAction jump;
        private InputAction roll;
        private InputAction crouch;
        private InputAction sprint;
        private InputAction fire;
        private InputAction attack;
        private InputAction specialAttack;

        private new Camera camera;          // New keyword to hide inherited member, inherited member is depricated anyway.
        #endregion

        protected override void Awake()
        {
            base.Awake();
            SetupControls();

            camera = GetComponentInChildren<Camera>();
            if (camera == null)
            {
                Debug.LogWarning("No camera found on the player. Please attach a child camera.");
                return;
            }
        }
        
        private void SetupControls()
        {
            playerControls = new PlayerControls();
            move = playerControls.Player.Move;
            jump = playerControls.Player.Jump;
            look = playerControls.Player.Look;
            zoom = playerControls.Player.Zoom;
            crouch = playerControls.Player.Crouch;
            roll = playerControls.Player.Roll;
            sprint = playerControls.Player.Sprint;
            fire = playerControls.Player.Fire;
            attack = playerControls.Player.Attack;
            specialAttack = playerControls.Player.SpecialAttack;
        }
        private void OnEnable()
        {
            playerControls.Player.Enable();
            look.performed += ctx => HandleLookInput(ctx);
            look.canceled += ctx => HandleLookInput();
            zoom.performed += ctx => AdjustZoomLevel(-ctx.ReadValue<float>());
            zoom.canceled += ctx => AdjustZoomLevel(0f);
            crouch.performed += ctx => ToggleCrouch();
            roll.performed += ctx => PerformRoll();
            sprint.started += ctx => NotifySprintTriggered(true);
            sprint.canceled += ctx => NotifySprintTriggered(false);
        }
        private void OnDisable()
        {
            playerControls.Player.Disable();
        }
        protected override void Update()
        {
            HandleJumpInput();
            HandleAttackInput();
            base.Update();
        }

        private void HandleLookInput(InputAction.CallbackContext ctx) => lookInput = ctx.ReadValue<Vector2>();
        private void HandleLookInput() => lookInput = Vector2.zero;
        private void AdjustZoomLevel(float zd) => NotifyZoomChanged(zd);

        private void HandleJumpInput()
        {
            if (jump.WasPressedThisFrame())
            {
                RequestJump();
            }
        }
        private void HandleAttackInput() 
        {
            if (attack.WasPressedThisFrame())        RequestAttack();
            if (specialAttack.WasPressedThisFrame()) RequestSpecialAttack();
            if (fire.WasPressedThisFrame())          RequestFireWeapon();
        }
        protected override void FixedUpdate()
        {
            SetMoveDirection();

            base.FixedUpdate(); // controller.Tick() and FixedTick() will trigger module updates

        }
        private void SetMoveDirection()
        {
            Vector2 moveInput = move.ReadValue<Vector2>();
            float inputMagnitude = moveInput.magnitude;
            inputMagnitude = Mathf.Pow(inputMagnitude, 1.5f); // smoother start

            // TODO swapped this from camera root while look is frozen for demo.
            //Vector3 moveDir = (cameraRoot.forward * moveInput.y + cameraRoot.right * moveInput.x);
            Vector3 moveDir = (camera.transform.forward * moveInput.y + camera.transform.right * moveInput.x);
            moveDir.y = 0f;
            moveDirection = moveDir.normalized * inputMagnitude;
        }
        protected override void ToggleCrouch()
        {
            if (crouch.WasPressedThisFrame())
            {
                base.ToggleCrouch();
            }
        }
        private void PerformRoll()
        {
            if (roll.WasPressedThisFrame())
            {
                RequestRoll();
            }
        }

    }
}
