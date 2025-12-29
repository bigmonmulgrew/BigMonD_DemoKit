using BMD;
using UnityEngine;

public class CharacterFlightModule : CharacterModule
{
    private Animator animator;
    private BMD.CharacterController controller;

    public override void Initialize(BMD.CharacterController controller)
    {
        this.controller = controller;
        animator = controller.GetComponent<Animator>();
    }

    public override void Tick(float deltaTime)
    {

    }
    public override void FixedTick(float fixedDeltaTime)
    {

    }
    public override void Dispose()
    {

    }
}
