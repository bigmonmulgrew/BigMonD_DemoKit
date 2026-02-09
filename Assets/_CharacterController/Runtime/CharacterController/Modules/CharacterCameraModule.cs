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

        enum CameraTiltMode { Unrestricted, Restricted, Disabled }
        enum CameraPanMode { Unrestricted, Restricted, Disabled }
        enum CameraZoomMode { Unrestricted, Restricted, Disabled }
        #region Configuration
        [Header("Camera reference (optional)")]
        [Tooltip("Optionally assign a camera.\n" +
            "If one is not specified it will be searched for in child objects.\n" +
            "If a child Camera does not exist, it will be created.")]
        [SerializeField] new Camera camera;         // New keyword to hide inherited member, inherited member is depricated anyway.

        [Header("Camera Features")]
        [SerializeField] bool enableLook = true;
        [SerializeField] CameraTiltMode tiltMode;
        [SerializeField] CameraPanMode panMode;
        [SerializeField] CameraZoomMode zoomMode;
        [SerializeField] bool isThirdPerson = true; // toggle first/third person
        [Tooltip("When using Restricted pan mode, this is the maximum angle the camera can rotate in degrees.")]
        [SerializeField] float maxLookdelta = 30f;


        [Header("Camera Input")]
        [SerializeField] bool invertVerticalLook = false;
        [SerializeField] bool invertHorizontalLook = false;
        [SerializeField] bool invertZoomInput = false;
        [Range(1f, 500f)]
        [SerializeField] float lookSensitivity = 100f;  // Speed of the camera rotation
        [Range(0.1f, 50f)]
        [SerializeField] float zoomSpeed = 0.5f;

        // Applies to Camera zoom by moving camera position
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
        [Tooltip("Adjust the height of the camera.\n" +
            "This will be set based on the prefab position if cam follow style is set to keep child or self transform.")]
        [SerializeField] float followHeight = 2f;
        [Tooltip("Offset the height of the pivot point used for camera tilt.\n" +
            "This is relative to the follow height.")]
        [SerializeField] float tiltOffset = 0.0f;
        [Range(0, 85.0f)]
        [SerializeField] float verticalClamp = 80f; // Maximum vertical angle for camera rotation
        [Range(-5.0f, 5.0f)]
        [Tooltip("Use offset to align camera left/rigth of character")]
        [SerializeField] float horizontalOffset = 0f; // Horizontal offset for the camera in third person mode
        [Range(0.001f, 2)]
        [SerializeField] float followSmoothRate = 0.5f;       // When camera is moved  how quickly do we follow the player
        #endregion

        [SerializeField] Vector3 DEBUG_CAMERA_ROTATION;
        [SerializeField] Vector3 DEBUG_CAMERA_LOCAL_ROTATION;

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

        float cameraPitch = 0f;
        float startingRotation;

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
        bool TiltDisabled => tiltMode == CameraTiltMode.Disabled;
        bool PanDisabled => panMode == CameraPanMode.Disabled;
        bool ZoomDisabled => zoomMode == CameraZoomMode.Disabled;
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

            DEBUG_CAMERA_ROTATION = _camera.transform.rotation.eulerAngles;
            DEBUG_CAMERA_LOCAL_ROTATION = _camera.transform.localRotation.eulerAngles;
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
            if (ZoomDisabled) return;

            // Clamp between -1 and 1, this allows joystick sensitivity but caps incorrect applied scaled from other input sources.
            zoomDelta = Mathf.Clamp(zoomDelta, -1, 1);

            // Apply speed and invert settings
            zoomDelta = zoomDelta * zoomSpeed * InvertZoom;

            // Update target follow distance
            if(zoomMode == CameraZoomMode.Restricted)
            {
                targetFollowDistance = Mathf.Clamp(targetFollowDistance + zoomDelta, minFollowDistance, maxFollowDistance);
            }
            else
            {
                targetFollowDistance += zoomDelta;
            }
        }
        private void SmoothZoom(float deltaTime)
        {
            if (ZoomDisabled) return;

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
            // add primitive sphere to pivot to make it easier to see in editor, can be removed later
            GameObject debugSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            debugSphere.transform.SetParent(cameraPivot, false);
            //disable collider on debug sphere so it doesn't interfere with character controller, can be removed later
            Destroy(debugSphere.GetComponent<Collider>());
            // halve the size
            debugSphere.transform.localScale = Vector3.one * 0.5f;

            // Create and position CameraRoot (pitch control)
            cameraRoot = new GameObject("CameraRoot").transform;
            // add primitive sphere to root to make it easier to see in editor, can be removed later
            GameObject debugSphere2 = GameObject.CreatePrimitive(PrimitiveType.Sphere);

            debugSphere2.transform.SetParent(cameraRoot, false);
            // set colour to red to differentiate from pivot sphere
            debugSphere2.GetComponent<Renderer>().material.color = Color.red;
            //disable collider on debug sphere so it doesn't interfere with character controller, can be removed later
            Destroy(debugSphere2.GetComponent<Collider>());
            // halve the size
            debugSphere2.transform.localScale = Vector3.one * 0.5f;

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

            Vector3 camOriginalPosition = _camera.transform.localPosition;
            Quaternion camOriginalRotation = _camera.transform.localRotation;

            // 1. Setup camera rig Pivot, Always use character transform as pivot 
            cameraPivot.position = transform.position;                                          // Camera pivot should be aligned with character origin
            cameraPivot.eulerAngles = new Vector3(0, camOriginalRotation.eulerAngles.y, 0);     // Set only the y axis rotation for pivot
            targetCamaraRigPosition = cameraPivot.position;

            // 2. Setup camera root, always to camera pivot
            cameraRoot.SetParent(cameraPivot, false);

            switch (camFollowStyle)
            {
                case CamFollowStyle.KeepChildOrSelfTransform:
                    // Nothing set for root, keep original transform
                    cameraRoot.localPosition = new Vector3(0f, camOriginalPosition.y + tiltOffset, 0f);
                    break;
                case CamFollowStyle.UseRigSettings:
                default:
                    cameraRoot.localPosition = new Vector3(0f, followHeight + tiltOffset, 0f);
                    cameraRoot.localRotation = Quaternion.identity;

                    break;
            }

            // 3. Reparent and reposition the actual camera
            _camera.transform.SetParent(cameraRoot, false);
            switch (camFollowStyle)
            {
                case CamFollowStyle.KeepChildOrSelfTransform:
                    cameraOffset = camOriginalPosition;
                    cameraOffset.y -= cameraRoot.localPosition.y;
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

            // Recrod startinng transforms for later use if needed
            startingRotation = cameraPivot.rotation.eulerAngles.y;

        }
        private void HandleLook(float deltaTime)
        {
            if (!enableLook) return;

            Vector2 delta = new();

            if (!PanDisabled) delta.x = controller.LookInput.x * InvertLookX;
            if (!TiltDisabled) delta.y = controller.LookInput.y * InvertLookY;

            delta *= lookSensitivity * deltaTime;

            // Pitch (up/down)
            cameraPitch -= delta.y;
            if (tiltMode == CameraTiltMode.Restricted) cameraPitch = Mathf.Clamp(cameraPitch, -verticalClamp, verticalClamp);
            cameraRoot.localEulerAngles = new Vector3(cameraPitch, 0f, 0f);

            // Yaw (left/right)
            if (panMode == CameraPanMode.Restricted)
            {
                float currentYRotation = cameraPivot.rotation.eulerAngles.y;
                float desiredYRotation = currentYRotation + delta.x;
                // Handle angle wrapping
                if (desiredYRotation > 180f) desiredYRotation -= 360f;
                if (desiredYRotation < -180f) desiredYRotation += 360f;
                float clampedYRotation = Mathf.Clamp(desiredYRotation, startingRotation - maxLookdelta, startingRotation + maxLookdelta);
                cameraPivot.rotation = Quaternion.Euler(0f, clampedYRotation, 0f);
            }
            else
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