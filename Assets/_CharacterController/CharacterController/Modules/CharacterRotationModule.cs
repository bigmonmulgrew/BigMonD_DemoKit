using UnityEngine;

public class CharacterRotationModule : MonoBehaviour, ICharacterModule
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
        var c = new CharacterFlightModule();
        c.Initialize(controller);
    }
    public void FixedTick(float fixedDeltaTime)
    {

    }
}
