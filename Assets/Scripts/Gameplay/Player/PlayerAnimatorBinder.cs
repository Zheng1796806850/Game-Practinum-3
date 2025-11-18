using UnityEngine;

public class PlayerAnimatorBinder : MonoBehaviour
{
    public Animator animator;
    public PlayerController player;
    public Rigidbody2D rb;
    public SpriteRenderer facingSprite;
    public bool invertFacingFromFlipX = false;

    public string groundedParam = "IsGrounded";
    public string moveForwardParam = "IsMovingForward";
    public string moveBackwardParam = "IsMovingBackward";
    public string jumpUpParam = "IsJumpUp";
    public string jumpDownParam = "IsJumpDown";

    public float moveInputThreshold = 0.1f;
    public float verticalSpeedThreshold = 0.05f;

    void Reset()
    {
        animator = GetComponentInChildren<Animator>() ?? GetComponent<Animator>();
        player = GetComponentInParent<PlayerController>() ?? GetComponent<PlayerController>();
        rb = GetComponentInParent<Rigidbody2D>() ?? GetComponent<Rigidbody2D>();
        if (animator != null)
        {
            var sr = animator.GetComponent<SpriteRenderer>();
            if (sr != null) facingSprite = sr;
        }
        if (facingSprite == null)
        {
            facingSprite = GetComponentInChildren<SpriteRenderer>();
        }
    }

    void Update()
    {
        if (animator == null) return;
        if (player == null) return;
        if (rb == null) return;

        bool grounded = player.IsGroundedNow;
        float vy = player.VerticalVelocity;
        float vx = rb.linearVelocity.x;

        bool movingRight = vx > moveInputThreshold;
        bool movingLeft = vx < -moveInputThreshold;

        bool facingRight = true;
        if (facingSprite != null)
        {
            bool flip = facingSprite.flipX;
            facingRight = invertFacingFromFlipX ? flip : !flip;
        }

        bool moveForward = false;
        bool moveBackward = false;

        if (movingRight || movingLeft)
        {
            if (movingRight && facingRight) moveForward = true;
            else if (movingLeft && !facingRight) moveForward = true;
            else moveBackward = true;
        }

        bool jumpUp = false;
        bool jumpDown = false;

        if (!grounded)
        {
            if (vy > verticalSpeedThreshold) jumpUp = true;
            else if (vy < -verticalSpeedThreshold) jumpDown = true;
        }

        animator.SetBool(groundedParam, grounded);
        animator.SetBool(moveForwardParam, moveForward);
        animator.SetBool(moveBackwardParam, moveBackward);
        animator.SetBool(jumpUpParam, jumpUp);
        animator.SetBool(jumpDownParam, jumpDown);
    }
}
