using UnityEngine;

public class CharacterAnimationModule : MonoBehaviour, ICharacterModule
{
    private Animator animator;
    private BMD.CharacterController controller;

    public void Initialize(BMD.CharacterController controller)
    {
        this.controller = controller;
        animator = controller.GetComponent<Animator>();
    }

    public void Tick(float deltaTime)
    {
        //animator.SetFloat("Speed", controller.CurrentSpeed);
        //animator.SetBool("Grounded", controller.IsGrounded);
    }
    public void FixedTick(float fixedDeltaTime)
    {

    }
}