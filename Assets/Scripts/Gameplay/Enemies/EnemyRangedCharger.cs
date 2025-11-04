using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Weapon))]
[RequireComponent(typeof(Health))]
public class EnemyRangedCharger : MonoBehaviour
{
    public enum PatrolMode { AutoFlip, TurnPoints }

    [Header("Target")]
    public Transform player;
    public string playerTag = "Player";

    [Header("Patrol")]
    public PatrolMode patrolMode = PatrolMode.AutoFlip;
    public Transform[] turnPoints;
    public float reachThreshold = 0.2f;
    public bool pingPong = true;

    [Header("Movement")]
    public float moveSpeed = 2f;
    public bool useAcceleration = true;
    public float maxAccel = 30f;
    public float maxDecel = 45f;
    public bool capHorizontalSpeed = false;
    public float maxHorizontalSpeed = 8f;

    [Header("Facing & Obstacles")]
    public Transform graphics;
    public bool autoFlipOnObstacle = true;
    public Transform groundCheck;
    public float groundCheckDistance = 0.25f;
    public float wallCheckDistance = 0.3f;
    public LayerMask groundMask;
    public bool stopAtEdgeDuringChase = true;

    [Header("Detection")]
    public bool useRectDetection = true;
    public float detectLeft = 5f;
    public float detectRight = 5f;
    public float detectUp = 2.5f;
    public float detectDown = 1.2f;
    public float detectionRange = 7f;
    public float attackRange = 6f;
    public float chargeDuration = 2.5f;
    public bool faceTarget = true;

    [Header("Freeze Behavior")]
    public bool ignoreFreezeForShooting = false;
    public bool resetChargeOnFreeze = true;

    [Header("Projectile & Damage")]
    public bool overrideBulletSpeed = false;
    public float bulletSpeed = 12f;
    public bool overrideBulletDamage = true;
    public float bulletDamage = 1f;
    public string bulletTagForSwitch = "FireBullet";
    public float generatorChargePercent = 100f;

    [Header("Particles")]
    public ParticleSystem chargeParticlesPrefab;
    public ParticleSystem shotParticlesPrefab;

    [Header("Runtime (Private)")]
    private Rigidbody2D rb;
    private Weapon weapon;
    private Health health;
    private bool charging = false;
    private float chargeTimer = 0f;
    private bool facingRight = true;
    private ParticleSystem chargeParticlesInstance;
    private int turnIndex = 0;
    private int turnDir = 1;
    private bool blockedThisFrame = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        weapon = GetComponent<Weapon>();
        health = GetComponent<Health>();
        health.OnFreezeStateChanged += OnFreezeChanged;
    }

    void OnDestroy()
    {
        if (health != null) health.OnFreezeStateChanged -= OnFreezeChanged;
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
        blockedThisFrame = false;

        if (health != null && health.IsFrozen && !ignoreFreezeForShooting)
        {
            ApplyHorizontalControl(0f);
            StopChargingParticles();
            charging = false;
            return;
        }

        if (player == null)
        {
            Patrol();
            return;
        }

        bool detected = useRectDetection ? IsPlayerInRectZone() : IsPlayerInCircleZone();

        if (!detected)
        {
            Patrol();
            charging = false;
            StopChargingParticles();
            return;
        }

        float dist = Vector2.Distance(transform.position, player.position);

        if (dist > attackRange)
        {
            MoveTowardsPlayer();
            StopChargingParticles();
            charging = false;
            return;
        }

        ApplyHorizontalControl(0f);

        if (health != null && health.IsFrozen && !ignoreFreezeForShooting)
        {
            StopChargingParticles();
            charging = false;
            return;
        }

        if (!charging)
        {
            charging = true;
            chargeTimer = chargeDuration;
            StartChargingParticles();
        }
        else
        {
            chargeTimer -= Time.deltaTime;
            if (chargeTimer <= 0f)
            {
                FireFullCharge();
                charging = false;
                chargeTimer = 0f;
                StopChargingParticles();
            }
            else
            {
                UpdateChargingParticles();
            }
        }

        if (faceTarget && !blockedThisFrame)
        {
            Vector2 dir = player.position - transform.position;
            if ((dir.x > 0 && !facingRight) || (dir.x < 0 && facingRight)) Flip();
        }
    }

    private void Patrol()
    {
        if (patrolMode == PatrolMode.AutoFlip)
        {
            if (autoFlipOnObstacle && !CanMoveForward()) Flip();
            float desiredX = (facingRight ? 1f : -1f) * moveSpeed;
            ApplyHorizontalControl(desiredX);
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

    private void MoveTowardsPlayer()
    {
        float dir = player.position.x - transform.position.x;

        if (autoFlipOnObstacle)
        {
            if (!CanMoveForward())
            {
                blockedThisFrame = true;
                if (stopAtEdgeDuringChase)
                {
                    ApplyHorizontalControl(0f);
                    return;
                }
                else
                {
                    Flip();
                }
            }
        }

        float desiredX = Mathf.Sign(dir) * moveSpeed;
        ApplyHorizontalControl(desiredX);
        if ((dir > 0f && !facingRight) || (dir < 0f && facingRight)) Flip();
    }

    private void ApplyHorizontalControl(float desiredSpeedX)
    {
        if (rb == null) return;

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

    private void FireFullCharge()
    {
        if (weapon == null || weapon.bulletPrefab == null) return;

        Vector2 dir;
        Vector3 fp = weapon.firePoint != null ? weapon.firePoint.position : transform.position;
        if (player != null) dir = ((Vector2)player.position - (Vector2)fp).normalized;
        else dir = facingRight ? Vector2.right : Vector2.left;

        GameObject bullet = Object.Instantiate(weapon.bulletPrefab, fp, Quaternion.identity);
        if (!string.IsNullOrEmpty(bulletTagForSwitch)) bullet.tag = bulletTagForSwitch;

        float spd = overrideBulletSpeed ? bulletSpeed : weapon.bulletSpeed;
        float dmg = overrideBulletDamage ? bulletDamage : weapon.damage;

        var rb2 = bullet.GetComponent<Rigidbody2D>();
        if (rb2 != null) rb2.linearVelocity = dir * spd;

        var proj = bullet.GetComponent<Projectile>();
        if (proj != null)
        {
            proj.projectileType = Projectile.ProjectileType.Ice;
            proj.Init(weapon.owner != null ? weapon.owner : gameObject, dmg, dir * spd);
        }

        var payload = bullet.GetComponent<GeneratorChargePayload>();
        if (payload == null) payload = bullet.AddComponent<GeneratorChargePayload>();
        payload.chargePercent = generatorChargePercent;

        if (shotParticlesPrefab != null)
        {
            var shot = Object.Instantiate(shotParticlesPrefab, fp, Quaternion.identity);
            var main = shot.main;
            if (!main.loop) Object.Destroy(shot.gameObject, main.startLifetime.constantMax + 0.2f);
        }
    }

    private bool IsPlayerInRectZone()
    {
        if (player == null) return false;
        Vector2 localPos = transform.InverseTransformPoint(player.position);
        return localPos.x >= -detectLeft &&
               localPos.x <= detectRight &&
               localPos.y >= -detectDown &&
               localPos.y <= detectUp;
    }

    private bool IsPlayerInCircleZone()
    {
        if (player == null) return false;
        float dist = Vector2.Distance(transform.position, player.position);
        return dist <= detectionRange;
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

    private void OnFreezeChanged(bool frozen)
    {
        if (frozen)
        {
            if (resetChargeOnFreeze)
            {
                charging = false;
                chargeTimer = chargeDuration;
            }
            StopChargingParticles();
        }
    }

    private void StartChargingParticles()
    {
        if (chargeParticlesPrefab == null) return;
        if (chargeParticlesInstance != null) return;
        Vector3 pos = weapon != null && weapon.firePoint != null ? weapon.firePoint.position : transform.position;
        chargeParticlesInstance = Object.Instantiate(chargeParticlesPrefab, pos, Quaternion.identity);
        chargeParticlesInstance.Play();
    }

    private void UpdateChargingParticles()
    {
        if (chargeParticlesInstance == null) return;
        Vector3 pos = weapon != null && weapon.firePoint != null ? weapon.firePoint.position : transform.position;
        chargeParticlesInstance.transform.position = pos;
    }

    private void StopChargingParticles()
    {
        if (chargeParticlesInstance == null) return;
        var inst = chargeParticlesInstance;
        chargeParticlesInstance = null;
        var main = inst.main;
        if (!main.loop) Object.Destroy(inst.gameObject, main.startLifetime.constantMax + 0.2f);
        else Object.Destroy(inst.gameObject);
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

        if (useRectDetection)
        {
            Gizmos.color = Color.red;
            Matrix4x4 old = Gizmos.matrix;
            Gizmos.matrix = transform.localToWorldMatrix;
            Vector3 center = new Vector3((detectRight - detectLeft) * 0.5f, (detectUp - detectDown) * 0.5f, 0);
            Vector3 size = new Vector3(detectLeft + detectRight, detectUp + detectDown, 0.1f);
            Gizmos.DrawWireCube(center, size);
            Gizmos.matrix = old;
        }
        else
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, detectionRange);
        }

        if (patrolMode == PatrolMode.TurnPoints && turnPoints != null && turnPoints.Length > 0)
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
