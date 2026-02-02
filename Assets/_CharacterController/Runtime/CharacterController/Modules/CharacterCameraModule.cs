using System;
using UnityEngine;

namespace BMD
{

    public class CharacterCameraModule : CharacterModule
    {
        #region Configuration
        [Header("Camera (optional)")]
        [Tooltip("Optionally assign a camera.\n" +
            "If one is not specified it will be searched for in child objects.\n" +
            "If a child Camera does not exist, it will be created.")]
        [SerializeField] new Camera camera;         // New keyword to hide inherited member, inherited member is depricated anyway.

        [Header("Camera Movement Settings")]
        [SerializeField] bool enableLook = true;
        [SerializeField] bool enableTilt = true;
        [SerializeField] bool enablePan = true;
        [SerializeField] bool enableZoom = true;
        [Range(0.01f, 2f)]
        [SerializeField] float lookSensitivity = 1f;  // Speed of the camera rotation
        [Range(0, 85.0f)]
        [SerializeField] float verticalClamp = 80f; // Maximum vertical angle for camera rotation

        [Header("Camera Follow Settings")]
        [Range(1f, 50f)]
        [SerializeField] float defaultFollowDistance = 5f;
        [Range(1f, 20f)]
        [SerializeField] float minFollowDistance = 2f;
        [Range(1f, 50f)]
        [SerializeField] float maxFollowDistance = 10f;
        [Range(0.1f, 50f)]
        [SerializeField] float zoomSpeed = 20f;
        [SerializeField] float followHeight = 2f;
        [Range(-5.0f, 5.0f)]
        [SerializeField] float horizontalOffset = 0f; // Horizontal offset for the camera in third person mode
        [SerializeField] bool isThirdPerson = true; // toggle first/third person
        [SerializeField] float smoothSpeed = 10f;
        [SerializeField] float cameraFollowDamping = 0.05f;
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
        //private Vector2 lookInput;
        private float cameraPitch = 0f;
        private float currentFollowDistance;
        #endregion
        public Camera Camera => _camera;
        #region Properties
        #endregion
        public override void PreInitialize(BMD.CharacterController controller)
        {           
            CacheReferences(controller);
        }

        public override void Initialize(BMD.CharacterController controller)
        {
            currentFollowDistance = defaultFollowDistance;
            SetupCamera();
            InitializeSignals(controller);

        }

        public override void Tick(float deltaTime)
        {
            HandleLook();
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
            controller.OnZoomChanged += HandleZoomChanged;
        }

        void HandleZoomChanged(float zd)
        {
            zd = zd > 0 ? 1 : zd < 0 ? -1 : 0;

            currentFollowDistance += zoomSpeed * zd * Time.deltaTime;
            currentFollowDistance = Mathf.Clamp(currentFollowDistance, minFollowDistance, maxFollowDistance);

            _camera.transform.localPosition = new Vector3(horizontalOffset, 0f, -currentFollowDistance);
        }

        private void SetupCamera()
        {
            // First check if a camera was manually assigned
            // Copy serialized camera to internal camera
            if (camera != null) _camera = camera;

            // Second, try searching for camera in children
            if (_camera != null) _camera = GetComponentInChildren<Camera>();

            // Finally, if no camera found create one
            if (_camera != null) _camera = new Camera();

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
            _camera.transform.SetParent(cameraRoot, false);
            _camera.transform.localPosition = new Vector3(horizontalOffset, 0f, -currentFollowDistance);
            _camera.transform.localRotation = Quaternion.identity;

            controller.RegisterCamera(_camera);
        }


        private void HandleLook()
        {
            if (!enableLook) return;

            Vector2 delta = controller.LookInput * lookSensitivity;

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
                 cameraFollowDamping
             );
        }
    }
}