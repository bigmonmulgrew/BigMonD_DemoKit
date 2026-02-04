using System;
using UnityEngine;

namespace BMD
{

    public class CharacterCameraModule : CharacterModule
    {
        enum CamFollowStyle
        {
            UseRigSettings,
            KeepChildOrSelfTransform
        }
        #region Configuration
        [Header("Camera reference (optional)")]
        [Tooltip("Optionally assign a camera.\n" +
            "If one is not specified it will be searched for in child objects.\n" +
            "If a child Camera does not exist, it will be created.")]
        [SerializeField] new Camera camera;         // New keyword to hide inherited member, inherited member is depricated anyway.

        [Header("Camera Features")]
        [SerializeField] bool enableLook = true;
        [SerializeField] bool enableTilt = true;
        [SerializeField] bool enablePan = true;
        [SerializeField] bool enableZoom = true;
        [SerializeField] bool isThirdPerson = true; // toggle first/third person


        [Header("Camera Input")]
        [SerializeField] bool invertVerticalLook = false;
        [SerializeField] bool invertHorizontalLook = false;
        [SerializeField] bool invertZoomInput = false;
        [Range(1f, 500f)]
        [SerializeField] float lookSensitivity = 100f;  // Speed of the camera rotation
        [Range(0.1f, 50f)]
        [SerializeField] float zoomSpeed = 0.5f;

        [Header("Camera Follow")]
        [Range(1f, 50f)]
        [SerializeField] float defaultFollowDistance = 5f;
        [Range(1f, 20f)]
        [SerializeField] float minFollowDistance = 2f;
        [Range(1f, 50f)]
        [SerializeField] float maxFollowDistance = 10f;
        [Range(0.01f, 1.0f)]
        [Tooltip("Higher values slow camera zoom change.")]
        [SerializeField] float camZoomDampingRate = 0.1f;
        [SerializeField] float cameraFollowDamping = 0.05f;

        [Header("Camera Rig Settings")]
        [SerializeField] CamFollowStyle camFollowStyle = CamFollowStyle.UseRigSettings;
        [SerializeField] float followHeight = 2f;
        [Range(0, 85.0f)]
        [SerializeField] float verticalClamp = 80f; // Maximum vertical angle for camera rotation
        [Range(-5.0f, 5.0f)]
        [Tooltip("Use offset to align camera left/rigth of character")]
        [SerializeField] float horizontalOffset = 0f; // Horizontal offset for the camera in third person mode
        [Range(0.001f,2)]
        [SerializeField] float followSmoothRate = 0.5f;       // When camera is moved  how quickly do we follow the player
        #endregion

        #region Cached References
        BMD.CharacterController controller;
        private UnityEngine.CharacterController unityController;
        private Camera _camera;                                  

        private Transform cameraPivot;
        private Transform cameraRoot;
        private Vector3 cameraVelocity;
        #endregion

        #region Runtime Variables
        Vector3 cameraOffset = new();
        Vector3 targetCamaraRigPosition = new();
        private float cameraPitch = 0f;

        // Zoom variables
        float targetFollowDistance;
        float currentFollowDistance;
        float _zoomVelocity = 0;
        #endregion
        public Camera Camera => _camera;
        #region Properties
        int InvertZoom => invertZoomInput ? -1 : 1;
        int InvertLookY => invertVerticalLook ? -1 : 1;
        int InvertLookX => invertHorizontalLook ? -1 : 1;
        #endregion
        public override void PreInitialize(BMD.CharacterController controller)
        {           
            CacheReferences(controller);
        }

        public override void Initialize(BMD.CharacterController controller)
        {
            currentFollowDistance = defaultFollowDistance;
            targetFollowDistance = currentFollowDistance;
            SetupCamera();
            InitializeSignals(controller);

        }

        public override void Tick(float deltaTime)
        {
            HandleLook(deltaTime);
            SmoothZoom(deltaTime);
        }
        public override void FixedTick(float fixedDeltaTime)
        {
            MoveCameraRigWitGameObject();
        }
        public override void Dispose()
        {
        }

        private void CacheReferences(CharacterController controller)
        {
            this.controller = controller;
            unityController = controller.GetComponent<UnityEngine.CharacterController>();
        }

        private void InitializeSignals(CharacterController controller)
        {
            controller.OnZoomChanged += HandleZoom;
        }

        void HandleZoom(float zoomDelta)
        {
            if (!enableZoom) return;

            // Clamp between -1 and 1, this allows joystick sensitivity but caps incorrect applied scaled from other input sources.
            zoomDelta = Mathf.Clamp(zoomDelta, -1, 1);

            // Apply speed and invert settings
            zoomDelta = zoomDelta * zoomSpeed * InvertZoom;

            // Update target follow distance
            targetFollowDistance = Mathf.Clamp(
                targetFollowDistance + zoomDelta,
                minFollowDistance,
                maxFollowDistance
            );
        }

        private void SmoothZoom(float deltaTime)
        {
            if (!enableZoom) return;

            currentFollowDistance = Mathf.SmoothDamp(
                currentFollowDistance,
                targetFollowDistance,
                ref _zoomVelocity,
                camZoomDampingRate,
                Mathf.Infinity,
                deltaTime
            );

            if (camFollowStyle == CamFollowStyle.UseRigSettings)
            {
                // Update existing variable rather than creating new. Faster, less allocations.
                cameraOffset.x = horizontalOffset;
                cameraOffset.y = 0f;
                
            }

            cameraOffset.z = -currentFollowDistance;

            _camera.transform.localPosition = cameraOffset;
        }

        private void SetupCamera()
        {
            // First check if a camera was manually assigned
            // Copy serialized camera to internal camera
            if (camera != null) _camera = camera;

            // Second, try searching for camera in children
            if (_camera == null) _camera = GetComponentInChildren<Camera>();

            // Finally, if no camera found create one
            if (_camera == null) _camera = new Camera();

            // Create CameraPivot (yaw control)
            cameraPivot = new GameObject("CameraPivot").transform;
            
            // Create and position CameraRoot (pitch control)
            cameraRoot = new GameObject("CameraRoot").transform;

            controller.RegisterCamera(_camera);
            SetupCameraTransforms(ref cameraPivot, ref cameraRoot);
        }

        void SetupCameraTransforms(ref Transform cameraPivot, ref Transform cameraRoot)
        {
            if (_camera == null)
            {
                Debug.LogError($"{this.name}: has no camera.");
                return;
            }

            Vector3 camOriginalPosition = _camera.transform.position;
            Quaternion camOriginalRotation = _camera.transform.rotation;

            // 1. Setup camera rig Pivot, Always use character transform as pivot 

            cameraPivot.position = transform.position;
            cameraPivot.rotation = camOriginalRotation;
            targetCamaraRigPosition = cameraPivot.position;

            // 2. Setup camera root, always to camera pivot
            cameraRoot.SetParent(cameraPivot, false);

            switch (camFollowStyle)
            {
                case CamFollowStyle.KeepChildOrSelfTransform:
                    // Nothing set for root, keep original transform
                    break;
                case CamFollowStyle.UseRigSettings:
                default:
                    cameraRoot.localPosition = new Vector3(0f, followHeight, 0f);
                    cameraRoot.localRotation = Quaternion.identity;

                    break;
            }

            // 3. Reparent and reposition the actual camera
            _camera.transform.SetParent(cameraRoot, false);
            switch (camFollowStyle)
            {
                case CamFollowStyle.KeepChildOrSelfTransform:
                    cameraOffset = camOriginalPosition;
                    _camera.transform.localPosition = cameraOffset;
                    //_camera.transform.localRotation = camOriginalRotation;
                    break;
                case CamFollowStyle.UseRigSettings:
                default:
                    
                    cameraOffset.x = horizontalOffset;
                    cameraOffset.y = 0f;
                    cameraOffset.z = -currentFollowDistance;
                    _camera.transform.localPosition = cameraOffset;
                    _camera.transform.localRotation = Quaternion.identity;
                    break;
            }

        }

        private void HandleLook(float deltaTime)
        {
            if (!enableLook) return;

            Vector2 delta = new();

            if (enablePan) delta.x = controller.LookInput.x * InvertLookX;
            if (enableTilt) delta.y = controller.LookInput.y * InvertLookY;

            delta *= lookSensitivity * deltaTime;

            // Pitch (up/down)
            cameraPitch -= delta.y;
            cameraPitch = Mathf.Clamp(cameraPitch, -verticalClamp, verticalClamp);
            cameraRoot.localEulerAngles = new Vector3(cameraPitch, 0f, 0f);

            // Yaw (left/right)
            cameraPivot.Rotate(Vector3.up * delta.x);
        }

        private void MoveCameraRigWitGameObject()
        {
            if (cameraPivot == null) return;

            Vector3 targetPos = transform.position;
            cameraPivot.position = Vector3.SmoothDamp(
                 cameraPivot.position,
                 transform.position,
                 ref cameraVelocity,
                 followSmoothRate
             );
        }
    }
}