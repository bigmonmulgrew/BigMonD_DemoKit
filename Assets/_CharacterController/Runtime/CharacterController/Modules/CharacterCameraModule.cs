using System;
using UnityEngine;

namespace BMD
{

    public class CharacterCameraModule : CharacterModule
    {
        #region Configuration
        [Header("Camera Movement Settings")]
        [SerializeField] bool enableCameraControl = true;
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

        #region Cached References
        BMD.CharacterController controller;
        private UnityEngine.CharacterController unityController;
        private new Camera camera;                                  // New keyword to hide inherited member, inherited member is depricated anyway.

        private Transform cameraPivot;
        private Transform cameraRoot;
        private Vector3 cameraVelocity;
        #endregion

        #region Runtime Variables
        //private Vector2 lookInput;
        private float cameraPitch = 0f;
        #endregion

        public override void PreInitialize(BMD.CharacterController controller)
        {
            CacheReferences(controller);
        }

        public override void Initialize(BMD.CharacterController controller)
        {
            SetupCamera();
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
            Debug.Log("CharacterTemplateModule Dispose triggered");
        }

        private void CacheReferences(CharacterController controller)
        {
            this.controller = controller;
            unityController = controller.GetComponent<UnityEngine.CharacterController>();
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


        private void HandleLook()
        {
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