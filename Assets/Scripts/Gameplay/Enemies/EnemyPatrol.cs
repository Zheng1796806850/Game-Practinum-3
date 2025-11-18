using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Health))]
public class EnemyPatrol : MonoBehaviour
{
    public enum PatrolMode { AutoFlip, TurnPoints }

    public bool movementEnabled = true;

    public float moveSpeed = 2f;
    public Transform groundCheck;
    public float groundCheckDistance = 0.25f;
    public float wallCheckDistance = 0.3f;
    public LayerMask groundMask;

    public Transform graphics;

    public PatrolMode patrolMode = PatrolMode.AutoFlip;
    public Transform[] turnPoints;
    public float reachThreshold = 0.2f;
    public bool pingPong = true;

    public bool useAcceleration = true;
    public float maxAccel = 30f;
    public float maxDecel = 40f;
    public bool capHorizontalSpeed = false;
    public float maxHorizontalSpeed = 8f;

    [Header("Flip Control")]
    public bool restrictFlipToGrounded = true;
    public float flipCooldown = 0.1f;

    private Rigidbody2D rb;
    private Health health;
    private bool facingRight = true;

    private int turnIndex = 0;
    private int turnDir = 1;
    private float lastFlipTime = -999f;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        health = GetComponent<Health>();
    }

    void Update()
    {
        if (health != null && health.IsFrozen) return;
        if (!movementEnabled || rb == null) return;

        if (patrolMode == PatrolMode.AutoFlip)
        {
            if (!CanMoveForward()) Flip();
            float desiredSpeedX = (facingRight ? 1f : -1f) * moveSpeed;
            ApplyHorizontalControl(desiredSpeedX);
        }
        else
        {
            float desiredSpeedX = 0f;

            if (turnPoints == null || turnPoints.Length == 0)
            {
                desiredSpeedX = (facingRight ? 1f : -1f) * moveSpeed;
                ApplyHorizontalControl(desiredSpeedX);
                return;
            }

            Transform target = turnPoints[Mathf.Clamp(turnIndex, 0, turnPoints.Length - 1)];
            if (target == null)
            {
                ApplyHorizontalControl(0f);
                return;
            }

            Vector2 dir = ((Vector2)target.position - (Vector2)transform.position);
            if (dir.x > 0f && !facingRight) Flip();
            if (dir.x < 0f && facingRight) Flip();

            if (dir.magnitude <= reachThreshold)
            {
                if (pingPong)
                {
                    if (turnIndex == 0) turnDir = 1;
                    else if (turnIndex == turnPoints.Length - 1) turnDir = -1;
                    turnIndex = Mathf.Clamp(turnIndex + turnDir, 0, turnPoints.Length - 1);
                }
                else
                {
                    turnIndex = (turnIndex + 1) % turnPoints.Length;
                }
            }

            desiredSpeedX = Mathf.Sign(dir.x) * moveSpeed;
            ApplyHorizontalControl(desiredSpeedX);
        }
    }

    private void ApplyHorizontalControl(float desiredSpeedX)
    {
        if (!useAcceleration)
        {
            Vector2 v = rb.linearVelocity;
            v.x = desiredSpeedX;
            if (capHorizontalSpeed) v.x = Mathf.Clamp(v.x, -maxHorizontalSpeed, maxHorizontalSpeed);
            rb.linearVelocity = v;
            return;
        }

        float dt = Time.deltaTime;
        Vector2 vcur = rb.linearVelocity;
        float accel = Mathf.Abs(desiredSpeedX) > Mathf.Abs(vcur.x) ? maxAccel : maxDecel;
        float dv = desiredSpeedX - vcur.x;
        float step = Mathf.Clamp(dv, -accel * dt, accel * dt);
        float newVx = vcur.x + step;
        if (capHorizontalSpeed) newVx = Mathf.Clamp(newVx, -maxHorizontalSpeed, maxHorizontalSpeed);
        rb.linearVelocity = new Vector2(newVx, vcur.y);
    }

    private bool CanMoveForward()
    {
        bool hasGround = true;
        bool hasWall = false;

        if (groundCheck != null)
        {
            RaycastHit2D groundHit = Physics2D.Raycast(groundCheck.position, Vector2.down, groundCheckDistance, groundMask);
            hasGround = groundHit.collider != null;
        }

        Vector2 dir = facingRight ? Vector2.right : Vector2.left;
        RaycastHit2D wallHit = Physics2D.Raycast(transform.position, dir, wallCheckDistance, groundMask);
        hasWall = wallHit.collider != null;

        return hasGround && !hasWall;
    }

    private bool IsGrounded()
    {
        if (groundCheck == null) return true;
        RaycastHit2D hit = Physics2D.Raycast(groundCheck.position, Vector2.down, groundCheckDistance, groundMask);
        return hit.collider != null;
    }

    private void Flip()
    {
        if (restrictFlipToGrounded && !IsGrounded()) return;

        if (flipCooldown > 0f)
        {
            if (Time.time - lastFlipTime < flipCooldown) return;
        }

        lastFlipTime = Time.time;

        facingRight = !facingRight;
        if (graphics != null)
        {
            var s = graphics.localScale;
            s.x *= -1f;
            graphics.localScale = s;
        }
        else
        {
            var s = transform.localScale;
            s.x *= -1f;
            transform.localScale = s;
        }
    }

    void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(groundCheck.position, groundCheck.position + Vector3.down * groundCheckDistance);
        }

        Gizmos.color = Color.cyan;
        Vector3 wallDir = Vector3.right * wallCheckDistance;
        Gizmos.DrawLine(transform.position, transform.position + wallDir);
        Gizmos.DrawLine(transform.position, transform.position - wallDir);

        if (turnPoints != null && turnPoints.Length > 0)
        {
            Gizmos.color = Color.green;
            for (int i = 0; i < turnPoints.Length; i++)
            {
                var t = turnPoints[i];
                if (t == null) continue;
                Gizmos.DrawSphere(t.position, 0.08f);
                if (i + 1 < turnPoints.Length && turnPoints[i + 1] != null)
                {
                    Gizmos.DrawLine(t.position, turnPoints[i + 1].position);
                }
            }
        }
    }
}
