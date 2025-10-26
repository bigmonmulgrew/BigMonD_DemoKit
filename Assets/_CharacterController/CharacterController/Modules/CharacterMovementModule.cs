using UnityEngine;

public class CharacterMovementModule : MonoBehaviour, ICharacterModule
{
    //#region Serialized fields
    //[Header("Character Movement Settings")]
    //[Tooltip("Speed settings for various character walking")]
    //[SerializeField] protected float walkSpeed = 2f;        // Speed of the character movement
    //[Tooltip("Speed settings for various character run")]
    //[SerializeField] protected float runSpeed = 6f;        // Speed of the character when running
    //[Tooltip("Speed settings for various character sprint")]
    //[SerializeField] protected float sprintSpeed = 10f;      // Speed of the character when sprinting
    //[Tooltip("Speed settings for various character rotation")]
    //[SerializeField] protected float rollSpeed = 15f;       // Speed of the character when rolling
    //[SerializeField] protected float rollDuration = 0.6f;   // Duration of the roll animation
    //[SerializeField] protected float crouchSpeed = 2.5f;    // Speed of the character when crouching
    //[SerializeField] protected float crawlSpeed = 1f;       // Speed of the character when crawling

    //[Header("Jump Settings")]
    //[SerializeField] protected float jumpForce = 5f; // Force applied when jumping
    //[SerializeField] protected int aerialJumps = 1; // Number of additional jumps allowed in the air
    //[SerializeField] protected bool airControl = true; // Whether the character can control movement in the air
    //[Range(0, 1)]
    //[SerializeField] protected float airControlFactor = 0.5f; // Factor by which air control is applied to movement speed
    //[SerializeField] protected float gravityScale = 1f; // Scale factor for gravity applied to the character
    //#endregion

    private BMD.CharacterController controller;

    public void Initialize(BMD.CharacterController controller)
    {
        this.controller = controller;
    }

    public void Tick(float deltaTime)
    {

    }
    public void FixedTick(float fixedDeltaTime)
    {

    }
}
