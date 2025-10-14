using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Weapon))]
[RequireComponent(typeof(Health))]
public class EnemyPatrol : MonoBehaviour
{
    public float moveSpeed = 2f;
    public Transform groundCheck;
    public float groundCheckDistance = 0.25f;
    public float wallCheckDistance = 0.3f;
    public LayerMask groundMask;

    public Transform player;
    public string playerTag = "Player";

    [Header("Detection Settings")]
    public float detectLeft = 4f;
    public float detectRight = 4f;
    public float detectUp = 2f;
    public float detectDown = 1f;

    private bool enableShooting = false;
    private float attackRange = 5f;
    private float attackInterval = 2f;
    public Transform graphics;

    [Header("Edge Behavior")]
    public bool stopAtEdge = true;

    private Rigidbody2D rb;
    private Weapon weapon;
    private Health health;
    private bool facingRight = true;
    private float attackTimer;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        weapon = GetComponent<Weapon>();
        health = GetComponent<Health>();
        health.OnDied += OnDeath;
    }

    void Start()
    {
        if (player == null && !string.IsNullOrEmpty(playerTag))
        {
            var go = GameObject.FindGameObjectWithTag(playerTag);
            if (go != null) player = go.transform;
        }
    }

    void Update()
    {
        if (health != null && health.IsFrozen)
        {
            //rb.linearVelocity = Vector2.zero;
            return;
        }

        if (player == null)
        {
            Patrol();
            return;
        }

        if (IsPlayerInDetectionZone())
        {
            MoveTowardsPlayer();
            attackTimer -= Time.deltaTime;
            if (enableShooting && IsPlayerInAttackRange() && attackTimer <= 0f)
            {
                Vector2 dir = (player.position - transform.position).normalized;
                if ((dir.x > 0 && !facingRight) || (dir.x < 0 && facingRight)) Flip();
                weapon.Fire(dir);
                attackTimer = attackInterval;
            }
        }
        else
        {
            Patrol();
        }
    }

    private void Patrol()
    {
        if (!CanMoveForward())
        {
            Flip();
        }

        rb.linearVelocity = new Vector2((facingRight ? 1f : -1f) * moveSpeed, rb.linearVelocity.y);
    }

    private void MoveTowardsPlayer()
    {
        float dir = player.position.x - transform.position.x;

        if (!CanMoveForward())
        {
            if (stopAtEdge)
            {
                rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
                return;
            }
        }

        rb.linearVelocity = new Vector2(Mathf.Sign(dir) * moveSpeed, rb.linearVelocity.y);

        if ((dir > 0f && !facingRight) || (dir < 0f && facingRight)) Flip();
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

    private bool IsPlayerInDetectionZone()
    {
        Vector2 localPos = transform.InverseTransformPoint(player.position);

        return localPos.x >= -detectLeft &&
               localPos.x <= detectRight &&
               localPos.y >= -detectDown &&
               localPos.y <= detectUp;
    }

    private bool IsPlayerInAttackRange()
    {
        return Vector2.Distance(transform.position, player.position) <= attackRange;
    }

    private void Flip()
    {
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

    void OnCollisionEnter2D(Collision2D collision)
    {
        for (int i = 0; i < collision.contactCount; i++)
        {
            var normal = collision.GetContact(i).normal;
            if (Mathf.Abs(normal.x) > 0.5f)
            {
                Flip();
                break;
            }
        }
    }

    private void OnDeath(GameObject killer)
    {
        Destroy(gameObject);
    }

    void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(groundCheck.position, groundCheck.position + Vector3.down * groundCheckDistance);
        }

        Gizmos.color = Color.cyan;
        Vector3 wallDir = (facingRight ? Vector3.right : Vector3.left) * wallCheckDistance;
        Gizmos.DrawLine(transform.position, transform.position + wallDir);

        Gizmos.color = Color.red;
        Vector3 localCenter = new Vector3((detectRight - detectLeft) * 0.5f, (detectUp - detectDown) * 0.5f, 0f);
        Vector3 localSize = new Vector3(detectLeft + detectRight, detectUp + detectDown, 0.1f);
        Matrix4x4 oldMatrix = Gizmos.matrix;
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawWireCube(localCenter, localSize);
        Gizmos.matrix = oldMatrix;
    }
}
