using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Refs")]
    public Rigidbody2D rb;
    public CapsuleCollider2D capsule;

    [Header("Data")]
    public PlayerData data;

    [Header("Ground")]
    public LayerMask groundMask;

    [Header("Input")]
    public string horizontalAxis = "Horizontal";
    public string verticalAxis = "Vertical";
    public string jumpButton = "Jump";

    [Header("Recoil")]
    public float recoilDamping = 8f;

    private float inputX;
    private float inputY;

    private bool isJumping;
    private bool isJumpFalling;
    private bool isJumpCut;

    private float lastGroundedTime = -999f;
    private float lastJumpPressedTime = -999f;

    private Vector2 externalVelocity;

    void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (capsule == null) capsule = GetComponent<CapsuleCollider2D>();
        externalVelocity = Vector2.zero;
    }

    void Start()
    {
        if (data != null) SetGravityScale(data.gravityScale);
    }

    void Update()
    {
        inputX = Input.GetAxisRaw(horizontalAxis);
        inputY = Input.GetAxisRaw(verticalAxis);

        if (Input.GetButtonDown(jumpButton))
            lastJumpPressedTime = Time.time;

        if (Input.GetButtonUp(jumpButton))
        {
            if (CanJumpCut()) isJumpCut = true;
        }

        if (IsGrounded())
        {
            lastGroundedTime = Time.time;
            if (!isJumping) isJumpFalling = false;
        }

        if (isJumping && rb.linearVelocity.y < 0f)
        {
            isJumping = false;
            isJumpFalling = true;
        }

        bool buffered = Time.time - lastJumpPressedTime <= data.jumpInputBufferTime;
        bool canCoyote = Time.time - lastGroundedTime <= data.coyoteTime;

        if (buffered && canCoyote && CanJump())
        {
            DoJump();
        }

        ApplySmartGravity();
    }

    void FixedUpdate()
    {
        Run();

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

    private void Run()
    {
        float targetSpeed = inputX * data.runMaxSpeed;
        float accelRate;

        bool onGround = Time.time - lastGroundedTime <= data.coyoteTime;

        if (onGround)
            accelRate = Mathf.Abs(targetSpeed) > 0.01f ? data.runAccelAmount : data.runDeccelAmount;
        else
            accelRate = (Mathf.Abs(targetSpeed) > 0.01f ? data.runAccelAmount * data.accelInAir : data.runDeccelAmount * data.deccelInAir);

        if ((isJumping || isJumpFalling) && Mathf.Abs(rb.linearVelocity.y) < data.jumpHangTimeThreshold)
        {
            accelRate *= data.jumpHangAccelerationMult;
            targetSpeed *= data.jumpHangMaxSpeedMult;
        }

        if (data.doConserveMomentum &&
            Mathf.Abs(rb.linearVelocity.x) > Mathf.Abs(targetSpeed) &&
            Mathf.Sign(rb.linearVelocity.x) == Mathf.Sign(targetSpeed) &&
            Mathf.Abs(targetSpeed) > 0.01f &&
            !onGround)
        {
            accelRate = 0f;
        }

        float speedDiff = targetSpeed - rb.linearVelocity.x;
        float movement = speedDiff * accelRate;
        rb.AddForce(movement * Vector2.right, ForceMode2D.Force);
    }

    private void DoJump()
    {
        lastJumpPressedTime = -999f;
        lastGroundedTime = -999f;

        float force = data.jumpForce;
        if (rb.linearVelocity.y < 0f) force -= rb.linearVelocity.y;

        rb.AddForce(Vector2.up * force, ForceMode2D.Impulse);

        isJumping = true;
        isJumpCut = false;
        isJumpFalling = false;
    }

    private void ApplySmartGravity()
    {
        if (IsGrounded() && rb.linearVelocity.y <= 0f)
        {
            SetGravityScale(data.gravityScale);
            isJumpCut = false;
            return;
        }

        if (rb.linearVelocity.y < 0f && inputY < 0f)
        {
            SetGravityScale(data.gravityScale * data.fastFallGravityMult);
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, Mathf.Max(rb.linearVelocity.y, -data.maxFastFallSpeed));
            return;
        }

        if (isJumpCut)
        {
            SetGravityScale(data.gravityScale * data.jumpCutGravityMult);
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, Mathf.Max(rb.linearVelocity.y, -data.maxFallSpeed));
            return;
        }

        if ((isJumping || isJumpFalling) && Mathf.Abs(rb.linearVelocity.y) < data.jumpHangTimeThreshold)
        {
            SetGravityScale(data.gravityScale * data.jumpHangGravityMult);
            return;
        }

        if (rb.linearVelocity.y < 0f)
        {
            SetGravityScale(data.gravityScale * data.fallGravityMult);
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, Mathf.Max(rb.linearVelocity.y, -data.maxFallSpeed));
            return;
        }

        SetGravityScale(data.gravityScale);
    }

    private bool CanJump()
    {
        return !isJumping;
    }

    private bool CanJumpCut()
    {
        return (isJumping || isJumpFalling) && rb.linearVelocity.y > 0f;
    }

    private void SetGravityScale(float scale)
    {
        rb.gravityScale = scale;
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

    void OnDrawGizmosSelected()
    {
        if (capsule == null) return;
        var bounds = capsule.bounds;
        float extra = 0.05f;
        Vector3 center = new Vector3(bounds.center.x, bounds.min.y - extra * 0.5f, 0f);
        Vector3 size = new Vector3(bounds.size.x * 0.9f, extra, 0.1f);
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(center, size);
    }

    public float InputX => inputX;
    public bool IsGroundedNow => IsGrounded();
    public bool IsJumping => isJumping;
    public bool IsJumpFalling => isJumpFalling;
    public float VerticalVelocity => rb != null ? rb.linearVelocity.y : 0f;
}
