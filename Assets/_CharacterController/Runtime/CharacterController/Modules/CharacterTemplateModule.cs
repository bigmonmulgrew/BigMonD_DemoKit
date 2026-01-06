using UnityEngine;

namespace BMD
{

    public class CharacterTemplateModule : CharacterModule
    {
        #region Configuration
        [Header("Sample Settings")]
        [SerializeField] float myValue = 10.0f;
        #endregion

        #region Cached References
        BMD.CharacterController controller;
        private UnityEngine.CharacterController unityController;
        #endregion

        public override void Initialize(BMD.CharacterController controller)
        {
            Debug.Log("CharacterTemplateModule Initialize triggered");
            this.controller = controller;
            unityController = controller.GetComponent<UnityEngine.CharacterController>();
        }
        
        public override void Tick(float deltaTime)
        {
            Debug.Log("CharacterTemplateModule Tick triggered");
        }
        public override void FixedTick(float fixedDeltaTime)
        {
            Debug.Log("CharacterTemplateModule FixedTick triggered");
        }
        public override void Dispose()
        {
            Debug.Log("CharacterTemplateModule Dispose triggered");
        }

        
    }
}