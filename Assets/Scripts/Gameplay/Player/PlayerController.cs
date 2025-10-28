using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Refs")]
    public Rigidbody2D rb;
    public CapsuleCollider2D capsule;

    [Header("Move")]
    public float moveSpeed = 6f;
    public float acceleration = 60f;
    public float deceleration = 80f;
    public float airControl = 0.6f;

    [Header("Jump")]
    public LayerMask groundMask;
    public float jumpForce = 12f;
    public float coyoteTime = 0.1f;
    public float jumpBuffer = 0.1f;

    [Header("Recoil")]
    public float recoilDamping = 8f;

    private float inputX;
    private bool grounded;
    private float lastGroundedTime;
    private float lastJumpPressTime;
    private bool wantJump;
    private Vector2 externalVelocity;

    void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (capsule == null) capsule = GetComponent<CapsuleCollider2D>();
        lastJumpPressTime = -999f;
        lastGroundedTime = -999f;
        externalVelocity = Vector2.zero;
    }

    void Update()
    {
        inputX = Input.GetAxisRaw("Horizontal");
        if (Input.GetButtonDown("Jump")) lastJumpPressTime = Time.time;

        if (IsGrounded())
        {
            grounded = true;
            lastGroundedTime = Time.time;
        }
        else
        {
            grounded = false;
        }

        bool pressedRecently = Time.time - lastJumpPressTime <= jumpBuffer && lastJumpPressTime > -900f;
        bool canCoyote = Time.time - lastGroundedTime <= coyoteTime;
        wantJump = pressedRecently && canCoyote;
    }

    void FixedUpdate()
    {
        float target = inputX * moveSpeed;
        float accel = Mathf.Abs(target) > 0.01f ? acceleration : deceleration;
        float ctrl = grounded ? 1f : airControl;
        float vx = Mathf.MoveTowards(rb.linearVelocity.x, target, accel * ctrl * Time.fixedDeltaTime);
        float vy = rb.linearVelocity.y;

        if (wantJump)
        {
            vy = jumpForce;
            wantJump = false;
            lastJumpPressTime = -999f;
            lastGroundedTime = -999f;
        }

        rb.linearVelocity = new Vector2(vx, vy);

        if (externalVelocity.sqrMagnitude > 0.000001f)
        {
            rb.linearVelocity += externalVelocity;
            float k = 1f - Mathf.Exp(-recoilDamping * Time.fixedDeltaTime);
            externalVelocity = Vector2.Lerp(externalVelocity, Vector2.zero, k);
            if (externalVelocity.sqrMagnitude < 0.00001f) externalVelocity = Vector2.zero;
        }
    }

    public void AddRecoil(Vector2 velocityDelta)
    {
        externalVelocity += velocityDelta;
    }

    private bool IsGrounded()
    {
        if (capsule == null) return false;
        var bounds = capsule.bounds;
        float extra = 0.05f;
        Vector2 center = new Vector2(bounds.center.x, bounds.min.y - extra * 0.5f);
        Vector2 size = new Vector2(bounds.size.x * 0.9f, extra);
        Collider2D hit = Physics2D.OverlapBox(center, size, 0f, groundMask);
        return hit != null;
    }
}
